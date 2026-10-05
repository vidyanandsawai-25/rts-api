using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.AapleSarkar;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Aaple Sarkar (MahaIT) Integration Service for Right to Services (RTS) Module.
/// Follows NtisPlatform Clean Architecture with dedicated RTS schema tables.
/// Fully compliant with Aaple Sarkar Portal Technical Integration Document v3.3.
/// </summary>
public class AapleSarkarIntegrationService : IAapleSarkarIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AapleSarkarIntegrationService> _logger;

    public AapleSarkarIntegrationService(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<AapleSarkarIntegrationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string DecodeTrackId(string? tdToken)
    {
        if (string.IsNullOrWhiteSpace(tdToken))
            return string.Empty;

        var token = tdToken.Trim();

        // 1. Direct track ID check (digits with length >= 10)
        if (token.Length >= 10 && token.All(char.IsDigit))
            return token;

        // 2. Base64 reversed format (# replaces =, characters reversed)
        try
        {
            var replaced = token.Replace("#", "=");
            var reversed = new string(replaced.Reverse().ToArray());
            var bytes = Convert.FromBase64String(reversed);
            var decoded = Encoding.UTF8.GetString(bytes).Trim();
            if (!string.IsNullOrWhiteSpace(decoded) && decoded.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                return decoded;
        }
        catch
        {
            // Ignore decoding failure and fall back
        }

        // 3. Fallback: direct base64 decode without reversal
        try
        {
            var bytes = Convert.FromBase64String(token);
            var decoded = Encoding.UTF8.GetString(bytes).Trim();
            if (!string.IsNullOrWhiteSpace(decoded) && decoded.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                return decoded;
        }
        catch
        {
            // Ignore
        }

        return token;
    }

    public async Task<bool> NotifyDocumentPendingAsync(string tdToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tdToken))
            return false;

        var actualTrackId = DecodeTrackId(tdToken);
        if (string.IsNullOrWhiteSpace(actualTrackId))
            return false;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var req = await db.RTSAapleSarkarRequests
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(r => r.AapleSarkarTrackId == actualTrackId || r.CitizenUserId == actualTrackId, ct);

            // Canonical trackId for MahaIT
            if (req != null && !string.IsNullOrWhiteSpace(req.AapleSarkarTrackId))
            {
                actualTrackId = req.AapleSarkarTrackId;
            }

            // Only transition to DocumentPending if application has not been submitted yet
            if (req != null)
            {
                if (!string.IsNullOrWhiteSpace(req.ApplicationNo) || 
                    (!string.Equals(req.Status, "Received", StringComparison.OrdinalIgnoreCase) && 
                     !string.Equals(req.Status, "DocumentPending", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("TrackId {TrackId} is already past DocumentPending stage (Status={Status}, AppNo={AppNo}). Skipping.",
                        actualTrackId, req.Status, req.ApplicationNo);
                    return false;
                }

                req.Status = "DocumentPending";
                req.UpdatedDate = DateTime.Now;
            }
            else
            {
                if (actualTrackId.Contains("-") || actualTrackId.Length > 20)
                {
                    _logger.LogWarning("Cannot create new AapleSarkarRequest with non-numeric TrackId: {TrackId}", actualTrackId);
                    return false;
                }

                var cred = await db.RTSAapleSarkarCredentials
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync(c => c.IsActive, ct);

                req = new RTSAapleSarkarRequestEntity
                {
                    AapleSarkarTrackId = actualTrackId,
                    UlbId = cred?.UlbId,
                    UlbDistrict = cred?.UlbDistrict,
                    Status = "DocumentPending",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };
                await db.RTSAapleSarkarRequests.AddAsync(req, ct);
            }

            // Audit status log
            bool logExists = await db.RTSAapleSarkarStatusLogs
                .AnyAsync(l => l.AapleSarkarTrackId == actualTrackId && l.Status == "DocumentPending", ct);

            if (!logExists)
            {
                await db.RTSAapleSarkarStatusLogs.AddAsync(new RTSAapleSarkarStatusLogEntity
                {
                    AapleSarkarTrackId = actualTrackId,
                    Status = "DocumentPending",
                    Remark = "Initial form landing recorded as Document Pending",
                    CreatedDate = DateTime.Now
                }, ct);
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Marked DocumentPending in RTS DB for TrackId={TrackId}", actualTrackId);

            // Push SOAP to MahaIT
            _ = Task.Run(() => PushStatusToMahaITSoapAsync(actualTrackId, "DocumentPending", ct: CancellationToken.None));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark DocumentPending for TrackId={TrackId}", actualTrackId);
            return false;
        }
    }

    public async Task<bool> NotifyPaymentPendingAsync(string tdTokenOrAppNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tdTokenOrAppNo))
            return false;

        var actualTrackId = DecodeTrackId(tdTokenOrAppNo);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var req = await db.RTSAapleSarkarRequests
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(r => r.AapleSarkarTrackId == actualTrackId 
                                       || r.CitizenUserId == actualTrackId 
                                       || r.ApplicationNo == tdTokenOrAppNo, ct);

            if (req != null)
            {
                req.Status = "PaymentPending";
                req.UpdatedDate = DateTime.Now;

                bool logExists = await db.RTSAapleSarkarStatusLogs
                    .AnyAsync(l => l.AapleSarkarTrackId == req.AapleSarkarTrackId && l.Status == "PaymentPending", ct);

                if (!logExists)
                {
                    await db.RTSAapleSarkarStatusLogs.AddAsync(new RTSAapleSarkarStatusLogEntity
                    {
                        AapleSarkarTrackId = req.AapleSarkarTrackId,
                        ApplicationNo = req.ApplicationNo,
                        Status = "PaymentPending",
                        Remark = "Statutory fee payment pending",
                        CreatedDate = DateTime.Now
                    }, ct);
                }

                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Marked PaymentPending in RTS DB for TrackId={TrackId}, AppNo={AppNo}", req.AapleSarkarTrackId, req.ApplicationNo);

                // Push SOAP to MahaIT
                _ = Task.Run(() => PushStatusToMahaITSoapAsync(req.AapleSarkarTrackId, "PaymentPending", ct: CancellationToken.None));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark PaymentPending for identifier={Id}", tdTokenOrAppNo);
            return false;
        }
    }

    public async Task<bool> MapApplicationAsync(
        string tdTokenOrTrackId,
        string applicationNo,
        string? status = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tdTokenOrTrackId) || string.IsNullOrWhiteSpace(applicationNo))
            return false;

        var actualTrackId = DecodeTrackId(tdTokenOrTrackId);
        if (string.IsNullOrWhiteSpace(actualTrackId))
            return false;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Fetch Application details from RTS.ApplicationDetails
            var app = await db.Set<RTSApplicationDetailsEntity>()
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.ApplicationNo == applicationNo.Trim(), ct);

            // 2. Resolve target status if not explicitly provided
            string targetStatus = status?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(targetStatus))
            {
                bool isFeeRequired = app?.Service != null && app.Service.FeesRequired && (app.Service.Fees ?? 0) > 0;
                bool isPaymentPending = string.Equals(app?.ApplicationStatus, "Payment Pending", StringComparison.OrdinalIgnoreCase);

                targetStatus = (isFeeRequired || isPaymentPending) ? "PaymentPending" : "UnderScrutiny";
            }

            // 3. Lookup credential and service mapping dynamically
            var cred = await db.RTSAapleSarkarCredentials
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(c => c.IsActive, ct);

            RTSAapleSarkarServiceMappingEntity? sMap = null;
            if (app?.ServiceId > 0)
            {
                sMap = await db.RTSAapleSarkarServiceMappings
                    .FirstOrDefaultAsync(m => m.RtsServiceId == app.ServiceId && m.IsActive, ct);
            }

            // 4. Find RTS.AapleSarkarRequest record by TrackId, CitizenUserId, or ApplicationNo
            var req = await db.RTSAapleSarkarRequests
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(r => r.AapleSarkarTrackId == actualTrackId 
                                       || r.CitizenUserId == actualTrackId
                                       || (!string.IsNullOrWhiteSpace(r.ApplicationNo) && r.ApplicationNo == applicationNo.Trim()), ct);

            // If not found yet and actualTrackId looks like a GUID, search by CitizenUserId
            if (req == null && (actualTrackId.Contains("-") || actualTrackId.Length > 20))
            {
                req = await db.RTSAapleSarkarRequests
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync(r => r.CitizenUserId == actualTrackId, ct);
            }

            // Resolve canonical numeric TrackId for MahaIT
            if (req != null && !string.IsNullOrWhiteSpace(req.AapleSarkarTrackId) && req.AapleSarkarTrackId.All(char.IsDigit))
            {
                actualTrackId = req.AapleSarkarTrackId;
            }

            if (req != null)
            {
                req.ApplicationNo = applicationNo.Trim();
                req.RtsServiceId = app?.ServiceId ?? req.RtsServiceId;
                if (!string.IsNullOrWhiteSpace(sMap?.MahaITServiceId.ToString()))
                {
                    req.ServiceId = sMap.MahaITServiceId.ToString();
                }
                if (!req.UlbId.HasValue && cred != null) req.UlbId = cred.UlbId;
                if (!req.UlbDistrict.HasValue && cred != null) req.UlbDistrict = cred.UlbDistrict;
                req.Status = targetStatus;
                req.UpdatedDate = DateTime.Now;
            }
            else
            {
                req = new RTSAapleSarkarRequestEntity
                {
                    AapleSarkarTrackId = actualTrackId,
                    ApplicationNo = applicationNo.Trim(),
                    RtsServiceId = app?.ServiceId,
                    ServiceId = sMap?.MahaITServiceId.ToString(),
                    UlbId = cred?.UlbId,
                    UlbDistrict = cred?.UlbDistrict,
                    CitizenName = app?.ApplicantName,
                    MobileNo = app?.ApplicantMobileNo,
                    Status = targetStatus,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };
                await db.RTSAapleSarkarRequests.AddAsync(req, ct);
            }

            // 5. Record status log
            bool logExists = await db.RTSAapleSarkarStatusLogs
                .AnyAsync(l => l.AapleSarkarTrackId == actualTrackId && l.Status == targetStatus, ct);

            if (!logExists)
            {
                await db.RTSAapleSarkarStatusLogs.AddAsync(new RTSAapleSarkarStatusLogEntity
                {
                    AapleSarkarTrackId = actualTrackId,
                    ApplicationNo = applicationNo.Trim(),
                    Status = targetStatus,
                    Remark = $"Application {applicationNo} mapped with status {targetStatus}",
                    CreatedDate = DateTime.Now
                }, ct);
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Mapped TrackId={TrackId} to ApplicationNo={AppNo} with Status={Status}", actualTrackId, applicationNo, targetStatus);

            // 6. Push SOAP to MahaIT
            _ = Task.Run(() => PushStatusToMahaITSoapAsync(actualTrackId, targetStatus, ct: CancellationToken.None));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map ApplicationNo={AppNo} to TrackId={TrackId}", applicationNo, actualTrackId);
            return false;
        }
    }

    public async Task<bool> UpdateStatusAsync(
        string applicationNo,
        string status,
        string? remark = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(applicationNo) || string.IsNullOrWhiteSpace(status))
            return false;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var req = await db.RTSAapleSarkarRequests
                .FirstOrDefaultAsync(r => r.ApplicationNo == applicationNo.Trim(), ct);

            if (req != null)
            {
                req.Status = status.Trim();
                req.UpdatedDate = DateTime.Now;

                bool logExists = await db.RTSAapleSarkarStatusLogs
                    .AnyAsync(l => l.AapleSarkarTrackId == req.AapleSarkarTrackId && l.Status == status.Trim(), ct);

                if (!logExists)
                {
                    await db.RTSAapleSarkarStatusLogs.AddAsync(new RTSAapleSarkarStatusLogEntity
                    {
                        AapleSarkarTrackId = req.AapleSarkarTrackId,
                        ApplicationNo = applicationNo.Trim(),
                        Status = status.Trim(),
                        Remark = remark ?? $"Status updated to {status}",
                        CreatedDate = DateTime.Now
                    }, ct);
                }

                await db.SaveChangesAsync(ct);
            }

            // Push SOAP to MahaIT
            _ = Task.Run(() => PushStatusToMahaITSoapAsync(applicationNo.Trim(), status.Trim(), remark: remark, ct: CancellationToken.None));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Aaple Sarkar status for AppNo={AppNo} to {Status}", applicationNo, status);
            return false;
        }
    }

    // ================= SOAP PUSH TO MAHAIT =================

    public async Task<bool> PushStatusToMahaITSoapAsync(
        string trackIdOrAppNo,
        string statusName,
        string? remark = null,
        string? transactionId = null,
        CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Fetch Request details from RTS.AapleSarkarRequest
            var req = await db.RTSAapleSarkarRequests
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(r => r.AapleSarkarTrackId == trackIdOrAppNo 
                                       || r.CitizenUserId == trackIdOrAppNo 
                                       || r.ApplicationNo == trackIdOrAppNo, ct);

            string? trackId = (req != null && !string.IsNullOrWhiteSpace(req.AapleSarkarTrackId) && req.AapleSarkarTrackId.All(char.IsDigit))
                ? req.AapleSarkarTrackId
                : (trackIdOrAppNo.All(char.IsDigit) ? trackIdOrAppNo : null);

            string? appNo = req?.ApplicationNo ?? (!trackIdOrAppNo.All(char.IsDigit) ? trackIdOrAppNo : null);

            if (string.IsNullOrWhiteSpace(trackId))
            {
                _logger.LogWarning("No valid numeric trackId resolved for {Id} to push SOAP", trackIdOrAppNo);
                return false;
            }

            // Fetch Application Details by ApplicationNo if available
            RTSApplicationDetailsEntity? appDetails = null;
            if (!string.IsNullOrWhiteSpace(appNo))
            {
                appDetails = await db.Set<RTSApplicationDetailsEntity>()
                    .Include(a => a.Service)
                    .FirstOrDefaultAsync(a => a.ApplicationNo == appNo, ct);
            }

            // 2. Fetch active credentials dynamically from RTS.AapleSarkarCredential
            var credQuery = db.RTSAapleSarkarCredentials.Where(c => c.IsActive);
            if (req?.UlbId.HasValue == true && req.UlbId.Value > 0)
            {
                credQuery = credQuery.Where(c => c.UlbId == req.UlbId.Value);
            }

            var cred = await credQuery.OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct)
                       ?? await db.RTSAapleSarkarCredentials.OrderByDescending(c => c.Id).FirstOrDefaultAsync(c => c.IsActive, ct);

            if (cred == null)
            {
                _logger.LogError("No active AapleSarkarCredential configured in RTS database. Cannot push SOAP for TrackId={TrackId}", trackId);
                return false;
            }

            string clientCode = cred.ClientCode;
            string checksumKey = cred.ChecksumKey;
            string encKey = cred.EncryptionKey;
            string encIv = cred.EncryptionIV;
            string serviceUrl = !string.IsNullOrWhiteSpace(cred.ServiceUrl)
                ? cred.ServiceUrl.Trim()
                : "http://testcitizenservices.mahaitgov.in/Dept_Authentication.asmx";
            string ulbIdStr = (req?.UlbId ?? cred.UlbId).ToString();
            string ulbDistrictStr = (req?.UlbDistrict ?? cred.UlbDistrict).ToString();

            // 3. Resolve service mapping dynamically from RTS.AapleSarkarServiceMapping
            int? rtsServiceId = req?.RtsServiceId ?? appDetails?.ServiceId;
            RTSAapleSarkarServiceMappingEntity? mapping = null;

            if (int.TryParse(req?.ServiceId, out var reqMahaItId) && reqMahaItId > 0)
            {
                mapping = await db.RTSAapleSarkarServiceMappings
                    .FirstOrDefaultAsync(m => m.MahaITServiceId == reqMahaItId && m.IsActive, ct);
            }
            else if (rtsServiceId.HasValue && rtsServiceId.Value > 0)
            {
                mapping = await db.RTSAapleSarkarServiceMappings
                    .FirstOrDefaultAsync(m => m.RtsServiceId == rtsServiceId.Value && m.IsActive, ct);
            }

            string serviceIdStr = req?.ServiceId ?? mapping?.MahaITServiceId.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(serviceIdStr))
            {
                _logger.LogError("No ServiceId resolved from database for TrackId={TrackId}, AppNo={AppNo}, ServiceId={SId}", trackId, appNo, rtsServiceId);
                return false;
            }

            string serviceName = mapping?.GovtServiceName
                                 ?? appDetails?.Service?.ServiceName
                                 ?? "RTS Service";
            int maxDays = mapping?.MaxProcessingDays ?? 7;
            if (mapping == null && !string.IsNullOrWhiteSpace(appDetails?.Service?.Sla))
            {
                var digits = new string(appDetails.Service.Sla.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var parsedDays) && parsedDays > 0)
                {
                    maxDays = parsedDays;
                }
            }

            // 4. Resolve citizen user ID and fee amount dynamically
            string userId = !string.IsNullOrWhiteSpace(req?.CitizenUserId) ? req.CitizenUserId.Trim() : "NA";
            decimal totalFee = appDetails?.Service?.FeesRequired == true ? (appDetails.Service.Fees ?? 0m) : 0m;
            string amountStr = totalFee.ToString("0.0#");

            // 5. Map MahaIT Status Payload (per MahaIT Doc v3.3 Page 25-27)
            var today = DateTime.Now;
            var estDate = today.AddDays(maxDays).ToString("yyyy-MM-dd");

            string appStatus = "1";
            string reqFlag = "0";
            string payStatus = "N";
            string payDate = "NA";
            string digSignStatus = "N";
            string digSignDate = "NA";
            string safeRemark = remark ?? $"{serviceName} Pending";

            var normalizedStatus = statusName.Trim().ToLowerInvariant();
            switch (normalizedStatus)
            {
                case "documentpending":
                case "document pending":
                    reqFlag = "0";
                    appStatus = "1";
                    payStatus = "N";
                    safeRemark = $"Your request for {serviceName} has been successfully submitted and is currently Document Pending.";
                    break;

                case "paymentpending":
                case "payment pending":
                    reqFlag = "0";
                    appStatus = "2";
                    payStatus = "N";
                    safeRemark = $"{serviceName} Pending";
                    break;

                case "paymentdone":
                case "payment done":
                    reqFlag = "1";
                    appStatus = "3";
                    payStatus = "Y";
                    payDate = today.ToString("yyyy-MM-dd");
                    safeRemark = $"{serviceName} completed successfully. Transaction ID: {transactionId ?? "NA"}.";
                    break;

                case "underscrutiny":
                case "under scrutiny":
                case "pending":
                    bool hadPayment = await db.RTSAapleSarkarStatusLogs
                        .AnyAsync(l => l.AapleSarkarTrackId == trackId && l.Status == "PaymentDone", ct);

                    reqFlag = "1";
                    appStatus = "3";
                    payStatus = hadPayment ? "Y" : "N";
                    payDate = hadPayment ? today.ToString("yyyy-MM-dd") : "NA";
                    safeRemark = $"Your application for {serviceName} is currently under scrutiny. Application No: {appNo ?? trackId}.";
                    break;

                case "approved":
                case "applicationapproved":
                    reqFlag = "0";
                    appStatus = "4";
                    payStatus = "Y";
                    digSignStatus = "Y";
                    digSignDate = today.ToString("yyyy-MM-dd");
                    safeRemark = $"Your application for {serviceName} has been approved successfully. Application No: {appNo ?? trackId}.";
                    break;

                case "rejected":
                case "applicationrejected":
                    reqFlag = "0";
                    appStatus = "5";
                    payStatus = "Y";
                    safeRemark = !string.IsNullOrWhiteSpace(remark) ? remark : $"Your application for {serviceName} has been rejected";
                    break;

                default:
                    reqFlag = "1";
                    appStatus = "3";
                    payStatus = "Y";
                    safeRemark = $"Your application for {serviceName} is in progress.";
                    break;
            }

            var effectiveAppId = !string.IsNullOrWhiteSpace(appNo) ? appNo.Trim() : trackId;
            var ud1 = ulbIdStr;
            var ud2 = ulbDistrictStr;

            // MahaIT Technical Integration v3.3 (Page 26-27):
            // TrackID|ClientCode|UserID|ServiceID|ApplicationID|PaymentStatus|PaymentDate|DigitalSignStatus|DigitalSignDate|EstimatedDays|EstimatedDate|Amount|RequestFlag|ApplicationStatus|Remark|UD1|UD2|UD3|UD4|UD5|
            string dataPayload = $"{trackId}|{clientCode}|{userId}|{serviceIdStr}|{effectiveAppId}|{payStatus}|{payDate}|{digSignStatus}|{digSignDate}|{maxDays}|{estDate}|{amountStr}|{reqFlag}|{appStatus}|{safeRemark}|{ud1}|{ud2}|NA|NA|NA|";

            string rawWithChecksum = dataPayload + checksumKey;
            uint crc = Crc32Helper.Compute(Encoding.UTF8.GetBytes(rawWithChecksum));
            string finalPayload = dataPayload + crc.ToString();

            // 6. Encrypt TripleDES CBC ZeroPadding
            string encryptedHex = TripleDesEncrypt(finalPayload, encKey, encIv);

            // 7. Post SOAP to MahaIT
            string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <SetAppStatus xmlns=""http://tempuri.org/"">
      <EncyKey>{encryptedHex}</EncyKey>
      <DeptCode>{clientCode}</DeptCode>
    </SetAppStatus>
  </soap:Body>
</soap:Envelope>";

            var client = _httpClientFactory.CreateClient();
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
            httpReq.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            httpReq.Headers.Add("SOAPAction", "\"http://tempuri.org/SetAppStatus\"");

            var response = await client.SendAsync(httpReq, ct);
            var responseXml = await response.Content.ReadAsStringAsync(ct);

            // 7. Decrypt result
            bool isSuccess = false;
            string decrypted = "";
            int start = responseXml.IndexOf("<SetAppStatusResult>") + 20;
            int end = responseXml.IndexOf("</SetAppStatusResult>");
            if (start > 19 && end > start)
            {
                var encResult = responseXml.Substring(start, end - start).Trim();
                decrypted = TripleDesDecrypt(encResult, encKey, encIv);
                isSuccess = decrypted.Contains("Success") || decrypted.Contains("<status>Success</status>");
            }

            _logger.LogInformation("MahaIT SOAP push result for TrackId={TrackId}, Status={Status}, Success={Ok}, Decrypted={Dec}",
                trackId, statusName, isSuccess, decrypted);

            // 8. Log to RTS.AapleSarkarWebhookLog
            var webhookLog = new RTSAapleSarkarWebhookLogEntity
            {
                AapleSarkarTrackId = trackId,
                ApplicationNo = effectiveAppId,
                ServiceId = int.TryParse(serviceIdStr, out var sId) ? sId : 0,
                Status = statusName,
                MahaItStatusCode = appStatus,
                RequestPayload = dataPayload,
                ResponsePayload = string.IsNullOrWhiteSpace(decrypted) ? responseXml : decrypted,
                IsSuccess = isSuccess,
                CreatedDate = DateTime.Now
            };
            await db.RTSAapleSarkarWebhookLogs.AddAsync(webhookLog, ct);
            await db.SaveChangesAsync(ct);

            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception pushing SOAP to MahaIT for {Id}, Status={Status}", trackIdOrAppNo, statusName);
            return false;
        }
    }

    // ================= CALLBACK / LANDING PROCESSING =================

    public async Task<(bool success, string redirectUrl, string? errorMessage)> ProcessCallbackAsync(
        string str,
        string ns,
        int? ulbId,
        int? ulbDistrict,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(str))
            return (false, string.Empty, "Encrypted data (str) is missing.");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Fetch active credentials dynamically
            var credQuery = db.RTSAapleSarkarCredentials.Where(c => c.IsActive);
            if (ulbId.HasValue && ulbId.Value > 0)
                credQuery = credQuery.Where(c => c.UlbId == ulbId.Value);

            var cred = await credQuery.OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct)
                       ?? await db.RTSAapleSarkarCredentials.OrderByDescending(c => c.Id).FirstOrDefaultAsync(c => c.IsActive, ct);

            if (cred == null)
                return (false, string.Empty, "No active AapleSarkarCredential found in database.");

            // 2. Decrypt str
            string decrypted = TripleDesDecrypt(str, cred.EncryptionKey, cred.EncryptionIV);
            if (string.IsNullOrWhiteSpace(decrypted))
                return (false, string.Empty, "Failed to decrypt incoming str payload.");

            // 3. Validate Checksum (if pipe delimited)
            var parts = decrypted.Split('|');
            if (parts.Length >= 5)
            {
                string rawChecksum = $"{parts[0]}|{parts[1]}|{parts[2]}|{cred.ChecksumKey}|{parts[4]}";
                uint expectedCrc = Crc32Helper.Compute(Encoding.UTF8.GetBytes(rawChecksum));
                if (expectedCrc.ToString() != parts[3])
                {
                    _logger.LogWarning("Checksum verification failed for incoming callback str. Expected={Exp}, Found={Act}", expectedCrc, parts[3]);
                }
            }

            // 4. Fetch Citizen Profile from MahaIT SOAP
            string trackId = parts.Length > 0 ? parts[0].Trim() : "";
            string userId = parts.Length > 1 ? parts[1].Trim() : "";
            string fullName = "";
            string mobileNo = "";
            int? districtId = null;
            int? talukaId = null;
            int? villageId = null;
            int? divisionId = null;

            try
            {
                var citizenData = await FetchCitizenDataFromMahaITAsync(str, cred, ct);
                if (citizenData != null)
                {
                    if (!string.IsNullOrWhiteSpace(citizenData.TrackId)) trackId = citizenData.TrackId;
                    if (!string.IsNullOrWhiteSpace(citizenData.UserId)) userId = citizenData.UserId;
                    fullName = citizenData.FullName ?? "";
                    mobileNo = citizenData.MobileNo ?? "";
                    districtId = citizenData.DistrictId;
                    talukaId = citizenData.TalukaId;
                    villageId = citizenData.VillageId;
                    divisionId = citizenData.DivisionId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch citizen profile from MahaIT SOAP. Falling back to decrypted tokens.");
            }

            // If trackId looks like a GUID/session token, attempt to recover numeric trackId from existing request in DB
            if (trackId.Contains("-") || trackId.Length > 20)
            {
                var existing = await db.RTSAapleSarkarRequests
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync(r => r.CitizenUserId == trackId || r.CitizenUserId == parts[0], ct);

                if (existing != null && !string.IsNullOrWhiteSpace(existing.AapleSarkarTrackId) && !existing.AapleSarkarTrackId.Contains("-"))
                {
                    trackId = existing.AapleSarkarTrackId;
                    userId = existing.CitizenUserId ?? userId;
                    fullName = !string.IsNullOrWhiteSpace(existing.CitizenName) ? existing.CitizenName : fullName;
                    mobileNo = !string.IsNullOrWhiteSpace(existing.MobileNo) ? existing.MobileNo : mobileNo;
                }
            }

            if (string.IsNullOrWhiteSpace(trackId))
                return (false, string.Empty, "Could not resolve TrackId from incoming payload.");

            // 5. Lookup Service Mapping
            int? mahaItServiceId = int.TryParse(ns, out var sId) ? sId : null;
            RTSAapleSarkarServiceMappingEntity? mapping = null;
            if (mahaItServiceId.HasValue)
            {
                mapping = await db.RTSAapleSarkarServiceMappings
                    .FirstOrDefaultAsync(m => m.MahaITServiceId == mahaItServiceId.Value && m.IsActive, ct);
            }

            int rtsServiceId = mapping?.RtsServiceId ?? 55;

            // 6. Save or update [RTS].[AapleSarkarRequest]
            var req = await db.RTSAapleSarkarRequests
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(r => r.AapleSarkarTrackId == trackId 
                                       || (!string.IsNullOrWhiteSpace(userId) && r.CitizenUserId == userId && r.ServiceId == ns), ct);

            if (req != null)
            {
                req.CitizenUserId = !string.IsNullOrWhiteSpace(userId) ? userId : req.CitizenUserId;
                req.CitizenName = !string.IsNullOrWhiteSpace(fullName) ? fullName : req.CitizenName;
                req.MobileNo = !string.IsNullOrWhiteSpace(mobileNo) ? mobileNo : req.MobileNo;
                req.DistrictId = districtId ?? req.DistrictId;
                req.TalukaId = talukaId ?? req.TalukaId;
                req.VillageId = villageId ?? req.VillageId;
                req.DivisionId = divisionId ?? req.DivisionId;
                req.RtsServiceId = rtsServiceId;
                req.ServiceId = ns;
                req.UlbId = ulbId ?? cred.UlbId;
                req.UlbDistrict = ulbDistrict ?? cred.UlbDistrict;
                req.RawPayload = decrypted;
                req.UpdatedDate = DateTime.Now;
            }
            else
            {
                req = new RTSAapleSarkarRequestEntity
                {
                    AapleSarkarTrackId = trackId,
                    CitizenUserId = userId,
                    CitizenName = fullName,
                    MobileNo = mobileNo,
                    DistrictId = districtId,
                    TalukaId = talukaId,
                    VillageId = villageId,
                    DivisionId = divisionId,
                    RtsServiceId = rtsServiceId,
                    ServiceId = ns,
                    UlbId = ulbId ?? cred.UlbId,
                    UlbDistrict = ulbDistrict ?? cred.UlbDistrict,
                    Status = "Received",
                    RawPayload = decrypted,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };
                await db.RTSAapleSarkarRequests.AddAsync(req, ct);
            }

            await db.SaveChangesAsync(ct);

            // 7. Generate TD Token (Base64 reversed with # replacing =)
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(trackId));
            string tdToken = new string(b64.Replace("=", "#").Reverse().ToArray());

            // 8. Build Redirect URL to RTS UI form strictly from DB [RTS].[AapleSarkarCredential].PortalBaseUrl
            if (string.IsNullOrWhiteSpace(cred.PortalBaseUrl))
            {
                _logger.LogError("PortalBaseUrl is not configured in database [RTS].[AapleSarkarCredential] for ClientCode={Code}, UlbId={UlbId}.", cred.ClientCode, cred.UlbId);
                return (false, string.Empty, $"PortalBaseUrl is not configured in database table [RTS].[AapleSarkarCredential] for ULB {cred.UlbId}. Please configure PortalBaseUrl in database.");
            }

            string baseUi = cred.PortalBaseUrl.TrimEnd('/');
            string finalRedirectUrl = $"{baseUi}/mr/service/{rtsServiceId}?TD={tdToken}";

            return (true, finalRedirectUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Aaple Sarkar callback with str");
            return (false, string.Empty, ex.Message);
        }
    }

    private async Task<CitizenProfileResult?> FetchCitizenDataFromMahaITAsync(
        string encKey,
        RTSAapleSarkarCredentialEntity cred,
        CancellationToken ct)
    {
        string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetParameterNew xmlns=""http://tempuri.org/"">
      <EncyKey>{encKey}</EncyKey>
      <DeptCode>{cred.ClientCode}</DeptCode>
    </GetParameterNew>
  </soap:Body>
</soap:Envelope>";

        var client = _httpClientFactory.CreateClient();
        string serviceUrl = !string.IsNullOrWhiteSpace(cred.ServiceUrl) 
            ? cred.ServiceUrl 
            : "http://testcitizenservices.mahaitgov.in/Dept_Authentication.asmx";

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
        httpReq.Content = new StringContent(soap, Encoding.UTF8, "text/xml");
        httpReq.Headers.Add("SOAPAction", "\"http://tempuri.org/GetParameterNew\"");

        var response = await client.SendAsync(httpReq, ct);
        var responseXml = await response.Content.ReadAsStringAsync(ct);

        int start = responseXml.IndexOf("<GetParameterNewResult>") + 23;
        int end = responseXml.IndexOf("</GetParameterNewResult>");
        if (start < 23 || end <= start) return null;

        var encryptedXml = System.Net.WebUtility.HtmlDecode(responseXml.Substring(start, end - start).Trim());
        var decryptedXml = TripleDesDecrypt(encryptedXml, cred.EncryptionKey, cred.EncryptionIV);

        if (string.IsNullOrWhiteSpace(decryptedXml) || !decryptedXml.Contains("<")) return null;

        var xdoc = new System.Xml.XmlDocument();
        xdoc.LoadXml(decryptedXml);

        var res = new CitizenProfileResult();
        res.TrackId = xdoc.SelectSingleNode("//TrackId")?.InnerText?.Trim();
        res.UserId = xdoc.SelectSingleNode("//UserID")?.InnerText?.Trim();
        res.FullName = xdoc.SelectSingleNode("//FullName")?.InnerText?.Trim();
        res.MobileNo = xdoc.SelectSingleNode("//MobileNo")?.InnerText?.Trim();
        if (int.TryParse(xdoc.SelectSingleNode("//DistrictID")?.InnerText, out var dId)) res.DistrictId = dId;
        if (int.TryParse(xdoc.SelectSingleNode("//TalukaID")?.InnerText, out var tId)) res.TalukaId = tId;
        if (int.TryParse(xdoc.SelectSingleNode("//VillageID")?.InnerText, out var vId)) res.VillageId = vId;
        if (int.TryParse(xdoc.SelectSingleNode("//DivisionID")?.InnerText, out var divId)) res.DivisionId = divId;

        return res;
    }

    private class CitizenProfileResult
    {
        public string? TrackId { get; set; }
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? MobileNo { get; set; }
        public int? DistrictId { get; set; }
        public int? TalukaId { get; set; }
        public int? VillageId { get; set; }
        public int? DivisionId { get; set; }
    }

    // ================= CRYPTO HELPERS =================

    private static string TripleDesEncrypt(string plainText, string keyStr, string ivStr)
    {
        using var des = TripleDES.Create();
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.Zeros;

        var keyBytes = Encoding.UTF8.GetBytes(keyStr);
        var ivBytes = Encoding.UTF8.GetBytes(ivStr);
        using var encryptor = des.CreateEncryptor(keyBytes, ivBytes);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return BitConverter.ToString(encBytes).Replace("-", "");
    }

    private static string TripleDesDecrypt(string hexString, string keyStr, string ivStr)
    {
        if (string.IsNullOrWhiteSpace(hexString)) return string.Empty;

        using var des = TripleDES.Create();
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.Zeros;

        var keyBytes = Encoding.UTF8.GetBytes(keyStr);
        var ivBytes = Encoding.UTF8.GetBytes(ivStr);
        using var decryptor = des.CreateDecryptor(keyBytes, ivBytes);

        int numberChars = hexString.Length;
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);

        var decBytes = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(decBytes).Trim('\0');
    }

    private static class Crc32Helper
    {
        private static readonly uint[] Table;

        static Crc32Helper()
        {
            Table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ 0xEDB88320;
                    else
                        entry >>= 1;
                }
                Table[i] = entry;
            }
        }

        public static uint Compute(byte[] bytes)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < bytes.Length; i++)
            {
                byte index = (byte)((crc & 0xFF) ^ bytes[i]);
                crc = (crc >> 8) ^ Table[index];
            }
            return ~crc;
        }
    }

    public async Task<(bool success, string redirectUrl, string? errorMessage, string? citizenUserId)> ProcessDashboardRedirectAsync(
        string appId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return (false, string.Empty, "Appid is required.", null);

        var cleanAppId = appId.Trim();
        var actualTrackId = DecodeTrackId(cleanAppId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Look up AapleSarkarRequest by TrackId or ApplicationNo
            var req = await db.RTSAapleSarkarRequests
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(r => r.AapleSarkarTrackId == cleanAppId || r.AapleSarkarTrackId == actualTrackId || r.ApplicationNo == cleanAppId, ct);

            string citizenUserId = req?.CitizenUserId ?? actualTrackId;

            // 2. Fetch portal base URL from credentials
            var cred = await db.RTSAapleSarkarCredentials
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(c => c.IsActive, ct);

            string portalBase = !string.IsNullOrWhiteSpace(cred?.PortalBaseUrl)
                ? cred.PortalBaseUrl.TrimEnd('/')
                : "http://localhost:3000";

            // 3. Build Redirect URL to citizen dashboard
            string redirectUrl = $"{portalBase}/mr/service/dashboard?CUID={Uri.EscapeDataString(citizenUserId)}";

            _logger.LogInformation("Dashboard redirect prepared for Appid={Appid}, CitizenUserId={CUID} -> {Url}",
                appId, citizenUserId, redirectUrl);

            return (true, redirectUrl, null, citizenUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dashboard redirect for Appid={Appid}", appId);
            return (false, string.Empty, ex.Message, null);
        }
    }

    public async Task<AapleSarkarCitizenApplicationsResponseDto> GetAapleSarkarApplicationsAsync(
        AapleSarkarCitizenApplicationsRequestDto request,
        CancellationToken ct = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CitizenUserId))
        {
            return new AapleSarkarCitizenApplicationsResponseDto
            {
                Status = false,
                Message = "CitizenUserId is required."
            };
        }

        var cuid = request.CitizenUserId.Trim();
        int pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 10;
        int offset = (pageNumber - 1) * pageSize;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Query base: all requests belonging to this citizen (by CitizenUserId, TrackId, or Mobile)
            var baseQuery = from r in db.RTSAapleSarkarRequests
                            where r.CitizenUserId == cuid || r.AapleSarkarTrackId == cuid
                            join sm in db.RTSServices on r.RtsServiceId equals sm.Id into smJoin
                            from service in smJoin.DefaultIfEmpty()
                            join ad in db.RTSApplicationDetails on r.ApplicationNo equals ad.ApplicationNo into adJoin
                            from appDetail in adJoin.DefaultIfEmpty()
                            join asm in db.RTSAapleSarkarServiceMappings on service.Id equals asm.RtsServiceId into asmJoin
                            from mapping in asmJoin.DefaultIfEmpty()
                            select new
                            {
                                r.ApplicationNo,
                                r.AapleSarkarTrackId,
                                RtsServiceId = service != null ? service.Id : r.RtsServiceId,
                                MahaITServiceId = mapping != null ? mapping.MahaITServiceId : 0,
                                ServiceName = service != null ? service.ServiceName : "Service",
                                ServiceNameMr = service != null ? (service.ServiceNameLocal ?? service.ServiceName) : "सेवा",
                                ApplicationStatus = appDetail != null ? appDetail.ApplicationStatus : (r.Status ?? "Pending"),
                                CreatedDate = appDetail != null ? appDetail.CreatedDate : r.CreatedDate,
                                IssuedCertificateGuid = appDetail != null ? appDetail.IssuedCertificateGuid : null
                            };

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var st = request.SearchText.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.ApplicationNo.Contains(st) ||
                    x.AapleSarkarTrackId.Contains(st) ||
                    x.ServiceName.Contains(st) ||
                    x.ServiceNameMr.Contains(st));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) && !request.StatusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var sf = request.StatusFilter.Trim();
                if (sf.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    baseQuery = baseQuery.Where(x => x.ApplicationStatus == "Approved" || x.ApplicationStatus.Contains("Approved"));
                }
                else if (sf.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    baseQuery = baseQuery.Where(x => x.ApplicationStatus == "Rejected" || x.ApplicationStatus.Contains("DisApproved") || x.ApplicationStatus.Contains("Reject"));
                }
                else
                {
                    baseQuery = baseQuery.Where(x => x.ApplicationStatus == sf);
                }
            }

            var totalCount = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(x => x.CreatedDate)
                .Skip(offset)
                .Take(pageSize)
                .ToListAsync(ct);

            var resultList = items.Select(x =>
            {
                string status = "Pending";
                if (x.ApplicationStatus != null && (x.ApplicationStatus.Contains("Approved", StringComparison.OrdinalIgnoreCase) || x.ApplicationStatus.Contains("Certificate Issued", StringComparison.OrdinalIgnoreCase)))
                    status = "Approved";
                else if (x.ApplicationStatus != null && (x.ApplicationStatus.Contains("DisApproved", StringComparison.OrdinalIgnoreCase) || x.ApplicationStatus.Contains("Reject", StringComparison.OrdinalIgnoreCase)))
                    status = "Rejected";

                return new AapleSarkarCitizenApplicationItemDto
                {
                    ApplicationNo = x.ApplicationNo ?? string.Empty,
                    AapleSarkarTrackId = x.AapleSarkarTrackId ?? string.Empty,
                    RtsServiceId = x.RtsServiceId ?? 0,
                    MahaITServiceId = x.MahaITServiceId,
                    ServiceName = x.ServiceName,
                    ServiceNameMr = x.ServiceNameMr,
                    ApplicationStatus = x.ApplicationStatus ?? "Pending",
                    Status = status,
                    CreatedDate = x.CreatedDate,
                    IssuedCertificateGuid = x.IssuedCertificateGuid,
                    CertificateUrl = x.IssuedCertificateGuid.HasValue
                        ? $"/api/RTSApplication/download-certificate?guid={x.IssuedCertificateGuid.Value}"
                        : null,
                    TrackingUrl = $"/mr/right-to-service/track-application?appNo={x.ApplicationNo}"
                };
            }).ToList();

            return new AapleSarkarCitizenApplicationsResponseDto
            {
                Status = true,
                Message = "Applications retrieved successfully.",
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = resultList
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Aaple Sarkar applications for CitizenUserId={CUID}", cuid);
            return new AapleSarkarCitizenApplicationsResponseDto
            {
                Status = false,
                Message = "Error retrieving applications: " + ex.Message
            };
        }
    }
}

