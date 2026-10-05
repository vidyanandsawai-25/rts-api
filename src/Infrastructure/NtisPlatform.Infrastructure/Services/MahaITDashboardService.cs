using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.AapleSarkar;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for integrating with Government MahaIT RTS Central Dashboard:
/// - Token Generation: GET /api/Token/GetToken
/// - Application Summary Push: POST /api/Dashboard/PushDepartmentDetails
/// Document Reference: DashboardAPI_clientDoc_New.pdf
/// </summary>
public class MahaITDashboardService : IMahaITDashboardService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MahaITDashboardService> _logger;

    public MahaITDashboardService(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<MahaITDashboardService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<MahaITDashboardRowDto>> GenerateMonthlySummaryAsync(int year, int month, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        string deptCode = _configuration["MahaIT:DepartmentName"] ?? "AKMC";
        int defaultDivision = int.TryParse(_configuration["MahaIT:DefaultDivision"], out var div) ? div : 6;
        int defaultDistrict = int.TryParse(_configuration["MahaIT:DefaultDistrict"], out var dist) ? dist : 520;
        int defaultTaluka = int.TryParse(_configuration["MahaIT:DefaultTaluka"], out var tal) ? tal : 4173;

        // Fetch active mappings
        var mappings = await db.RTSAapleSarkarServiceMappings
            .Include(m => m.RtsService)
            .Where(m => m.IsActive && m.MahaITServiceId > 0)
            .ToListAsync(ct);

        // Fetch all applications in range
        var appList = await db.RTSApplicationDetails
            .Where(a => a.CreatedDate >= startDate && a.CreatedDate < endDate && a.IsActive != false)
            .Select(a => new
            {
                a.ServiceId,
                a.ApplicationStatus,
                a.CreatedDate,
                a.UpdatedDate
            })
            .ToListAsync(ct);

        // Fetch all AapleSarkarRequests in range
        var asrList = await db.RTSAapleSarkarRequests
            .Where(r => r.CreatedDate >= startDate && r.CreatedDate < endDate)
            .Select(r => new
            {
                r.RtsServiceId,
                r.Status,
                r.CreatedDate,
                r.UpdatedDate
            })
            .ToListAsync(ct);

        var resultRows = new List<MahaITDashboardRowDto>();

        foreach (var mapping in mappings)
        {
            int rtsServiceId = mapping.RtsServiceId;
            int mahaItServiceId = mapping.MahaITServiceId;
            string serviceName = mapping.RtsService?.ServiceName ?? mapping.GovtServiceName ?? $"Service-{rtsServiceId}";
            int maxDays = mapping.MaxProcessingDays > 0 ? mapping.MaxProcessingDays : 15;

            var serviceApps = appList.Where(a => a.ServiceId == rtsServiceId).ToList();
            var serviceAsr = asrList.Where(r => r.RtsServiceId == rtsServiceId).ToList();

            // Counts calculation
            int approved = 0;
            int rejected = 0;
            int pendingUser = 0;
            int pendingDept = 0;
            int notOnTime = 0;

            foreach (var a in serviceApps)
            {
                var status = a.ApplicationStatus?.Trim() ?? string.Empty;
                DateTime created = a.CreatedDate ?? DateTime.UtcNow;
                DateTime updated = a.UpdatedDate ?? created;
                int elapsedDays = (updated - created).Days;

                if (status.Contains("Approved", StringComparison.OrdinalIgnoreCase) || status.Contains("Certificate Issued", StringComparison.OrdinalIgnoreCase))
                {
                    approved++;
                    if (elapsedDays > maxDays)
                        notOnTime++;
                }
                else if (status.Contains("Reject", StringComparison.OrdinalIgnoreCase) || status.Contains("DisApproved", StringComparison.OrdinalIgnoreCase))
                {
                    rejected++;
                    if (elapsedDays > maxDays)
                        notOnTime++;
                }
                else if (status.Contains("PaymentPending", StringComparison.OrdinalIgnoreCase) || status.Contains("DocumentPending", StringComparison.OrdinalIgnoreCase))
                {
                    pendingUser++;
                }
                else
                {
                    pendingDept++;
                }
            }

            // Include any AapleSarkarRequests not tracked in RTSApplicationDetails
            if (!serviceApps.Any() && serviceAsr.Any())
            {
                foreach (var r in serviceAsr)
                {
                    var status = r.Status?.Trim() ?? string.Empty;
                    DateTime created = r.CreatedDate;
                    DateTime updated = r.UpdatedDate ?? created;
                    int elapsedDays = (updated - created).Days;

                    if (status.Contains("Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        approved++;
                        if (elapsedDays > maxDays) notOnTime++;
                    }
                    else if (status.Contains("Reject", StringComparison.OrdinalIgnoreCase) || status.Contains("DisApproved", StringComparison.OrdinalIgnoreCase))
                    {
                        rejected++;
                        if (elapsedDays > maxDays) notOnTime++;
                    }
                    else if (status.Contains("Payment", StringComparison.OrdinalIgnoreCase) || status.Contains("Document", StringComparison.OrdinalIgnoreCase))
                    {
                        pendingUser++;
                    }
                    else
                    {
                        pendingDept++;
                    }
                }
            }

            // Validation formulas from MahaIT Document:
            // Total Disposed = Approved + Rejected
            // OnTimeDelivery = Total Disposed - NotOnTimeDelivery
            int totalDisposed = approved + rejected;
            if (notOnTime > totalDisposed) notOnTime = totalDisposed;
            int onTime = totalDisposed - notOnTime;

            var row = new MahaITDashboardRowDto
            {
                Department = deptCode,
                Service = mahaItServiceId,
                Division = defaultDivision,
                District = defaultDistrict,
                Taluka = defaultTaluka,
                Approved = approved,
                Rejected = rejected,
                OnTimeDelivery = onTime,
                PendingatUser = pendingUser,
                PendingatDepartment = pendingDept,
                NotOnTimeDelivery = notOnTime,
                PaymentMode = "PG",
                ApplicationSource = "U",
                Year = year,
                Month = month
            };
            resultRows.Add(row);

            // Upsert in RTS.MahaITDashboardReport
            var existingReport = await db.RTSMahaITDashboardReports
                .FirstOrDefaultAsync(r => r.ReportYear == year && r.ReportMonth == month && r.RtsServiceId == rtsServiceId, ct);

            if (existingReport == null)
            {
                existingReport = new RTSMahaITDashboardReportEntity
                {
                    ReportYear = year,
                    ReportMonth = month,
                    RtsServiceId = rtsServiceId,
                    MahaITServiceId = mahaItServiceId,
                    ServiceName = serviceName,
                    DepartmentCode = deptCode,
                    Division = defaultDivision,
                    District = defaultDistrict,
                    Taluka = defaultTaluka,
                    Approved = approved,
                    Rejected = rejected,
                    PendingatUser = pendingUser,
                    PendingatDepartment = pendingDept,
                    OnTimeDelivery = onTime,
                    NotOnTimeDelivery = notOnTime,
                    ApplicationSource = "U",
                    PaymentMode = "PG",
                    GeneratedOn = DateTime.UtcNow
                };
                db.RTSMahaITDashboardReports.Add(existingReport);
            }
            else
            {
                existingReport.MahaITServiceId = mahaItServiceId;
                existingReport.ServiceName = serviceName;
                existingReport.Approved = approved;
                existingReport.Rejected = rejected;
                existingReport.PendingatUser = pendingUser;
                existingReport.PendingatDepartment = pendingDept;
                existingReport.OnTimeDelivery = onTime;
                existingReport.NotOnTimeDelivery = notOnTime;
                existingReport.GeneratedOn = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Generated MahaIT dashboard summary for Year={Year}, Month={Month}: {Count} services",
            year, month, resultRows.Count);

        return resultRows;
    }

    public async Task<MahaITDashboardPushResultDto> PushToMahaITDashboardAsync(int year, int month, CancellationToken ct = default)
    {
        var summaryRows = await GenerateMonthlySummaryAsync(year, month, ct);

        string clientSecretKey = _configuration["MahaIT:ClientSecretKey"] ?? "4332093E-07D9-4765-AA1E-4C6A640ACF94";
        string departmentCode = _configuration["MahaIT:DepartmentCode"] ?? "AKMCDEPT";
        string tokenUrl = _configuration["MahaIT:TokenUrl"] ?? "https://rtsdashboarddeptapi.mahaitgov.in/api/Token/GetToken";
        string pushUrl = _configuration["MahaIT:PushURL"] ?? "https://rtsdashboarddeptapi.mahaitgov.in/api/Dashboard/PushDepartmentDetails";

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        string? bearerToken = null;
        var resultDto = new MahaITDashboardPushResultDto
        {
            ReportYear = year,
            ReportMonth = month,
            TotalServices = summaryRows.Count,
            PushedData = summaryRows
        };

        // 1. Get Token
        try
        {
            using var tokenReq = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
            tokenReq.Headers.Add("ClientSecretKey", clientSecretKey);
            tokenReq.Headers.Add("DepartmentCode", departmentCode);

            var tokenResp = await client.SendAsync(tokenReq, ct);
            var tokenJson = await tokenResp.Content.ReadAsStringAsync(ct);

            if (tokenResp.IsSuccessStatusCode)
            {
                var tokenObj = JsonSerializer.Deserialize<MahaITTokenResponse>(tokenJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                bearerToken = tokenObj?.data;
            }

            if (string.IsNullOrWhiteSpace(bearerToken))
            {
                resultDto.IsSuccess = false;
                resultDto.Message = $"Failed to obtain MahaIT token. HTTP {(int)tokenResp.StatusCode}: {tokenJson}";
                await LogPushAttemptAsync(year, month, summaryRows.Count, 0, summaryRows.Count, false, false, null, tokenJson, resultDto.Message);
                return resultDto;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling MahaIT GetToken at {Url}", tokenUrl);
            resultDto.IsSuccess = false;
            resultDto.Message = $"Exception obtaining MahaIT token: {ex.Message}";
            await LogPushAttemptAsync(year, month, summaryRows.Count, 0, summaryRows.Count, false, false, null, null, ex.Message);
            return resultDto;
        }

        // 2. Push Department Details
        int successCount = 0;
        int failedCount = 0;
        var responses = new List<string>();

        // Try batch push first if multiple, or iterate per service as documented
        foreach (var row in summaryRows)
        {
            try
            {
                using var pushReq = new HttpRequestMessage(HttpMethod.Post, pushUrl);
                pushReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                string rowJson = JsonSerializer.Serialize(row);
                pushReq.Content = new StringContent(rowJson, Encoding.UTF8, "application/json");

                var pushResp = await client.SendAsync(pushReq, ct);
                string pushContent = await pushResp.Content.ReadAsStringAsync(ct);
                responses.Add($"Service {row.Service}: HTTP {(int)pushResp.StatusCode} -> {pushContent}");

                if (pushResp.IsSuccessStatusCode)
                {
                    successCount++;
                    await UpdateServicePushStatus(year, month, row.Service, true, pushContent);
                }
                else
                {
                    failedCount++;
                    await UpdateServicePushStatus(year, month, row.Service, false, pushContent);
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                responses.Add($"Service {row.Service}: Exception -> {ex.Message}");
                await UpdateServicePushStatus(year, month, row.Service, false, ex.Message);
            }
        }

        bool overallSuccess = failedCount == 0 && successCount > 0;
        resultDto.IsSuccess = overallSuccess;
        resultDto.SuccessServices = successCount;
        resultDto.FailedServices = failedCount;
        resultDto.RawResponse = string.Join("; ", responses);
        resultDto.Message = overallSuccess
            ? $"Successfully pushed all {successCount} services to MahaIT RTS Dashboard."
            : $"Pushed with warnings: {successCount} succeeded, {failedCount} failed.";

        await LogPushAttemptAsync(year, month, summaryRows.Count, successCount, failedCount, overallSuccess, true,
            JsonSerializer.Serialize(summaryRows), resultDto.RawResponse, overallSuccess ? null : resultDto.Message);

        return resultDto;
    }

    public async Task<List<RTSMahaITDashboardReportEntity>> GetDashboardSummaryAsync(int year, int month, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.RTSMahaITDashboardReports
            .Where(r => r.ReportYear == year && r.ReportMonth == month)
            .OrderBy(r => r.RtsServiceId)
            .ToListAsync(ct);
    }

    public async Task<List<RTSMahaITDashboardPushLogEntity>> GetPushLogsAsync(int limit = 50, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.RTSMahaITDashboardPushLogs
            .OrderByDescending(l => l.PushedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    private async Task UpdateServicePushStatus(int year, int month, int mahaItServiceId, bool success, string response)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var report = await db.RTSMahaITDashboardReports
                .FirstOrDefaultAsync(r => r.ReportYear == year && r.ReportMonth == month && r.MahaITServiceId == mahaItServiceId);

            if (report != null)
            {
                report.LastPushedOn = DateTime.UtcNow;
                report.PushStatus = success ? "SUCCESS" : "FAILED";
                report.PushResponse = response.Length > 2000 ? response.Substring(0, 2000) : response;
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating push status for service {Service}", mahaItServiceId);
        }
    }

    private async Task LogPushAttemptAsync(
        int year,
        int month,
        int total,
        int success,
        int failed,
        bool isSuccess,
        bool tokenObtained,
        string? requestBody,
        string? responseBody,
        string? errorMessage)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var log = new RTSMahaITDashboardPushLogEntity
            {
                ReportYear = year,
                ReportMonth = month,
                TotalServices = total,
                SuccessServices = success,
                FailedServices = failed,
                IsSuccess = isSuccess,
                TokenObtained = tokenObtained,
                RequestBody = requestBody,
                ResponseBody = responseBody,
                ErrorMessage = errorMessage,
                PushedAt = DateTime.UtcNow
            };
            db.RTSMahaITDashboardPushLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging MahaIT push attempt.");
        }
    }
}
