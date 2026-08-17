using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RTSPayment;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSPaymentService : IRTSPaymentService
{
    private readonly IRepository<RTSPaymentTransactionEntity, long> _paymentRepository;
    private readonly IRepository<RTSPaymentGatewayConfigEntity, int> _gatewayConfigRepository;
    private readonly IRepository<RTSPaymentStatusMasterEntity, int> _statusRepository;
    private readonly IRepository<RTSPaymentModeMasterEntity, int> _modeRepository;
    private readonly IRepository<RTSPaymentWebhookLogEntity, long> _webhookLogRepository;
    private readonly IRepository<RTSApplicationDetailsEntity, int> _applicationRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IRepository<RTSDepartmentEntity, int> _departmentRepository;
    private readonly IRepository<RTSFieldValueEntity, int> _fieldValueRepository;
    private readonly IRepository<TrackApplicationHistoryEntity, int> _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly ILogger<RTSPaymentService> _logger;

    public RTSPaymentService(
        IRepository<RTSPaymentTransactionEntity, long> paymentRepository,
        IRepository<RTSPaymentGatewayConfigEntity, int> gatewayConfigRepository,
        IRepository<RTSPaymentStatusMasterEntity, int> statusRepository,
        IRepository<RTSPaymentModeMasterEntity, int> modeRepository,
        IRepository<RTSPaymentWebhookLogEntity, long> webhookLogRepository,
        IRepository<RTSApplicationDetailsEntity, int> applicationRepository,
        IRepository<RTSServiceEntity, int> serviceRepository,
        IRepository<RTSDepartmentEntity, int> departmentRepository,
        IRepository<RTSFieldValueEntity, int> fieldValueRepository,
        IRepository<TrackApplicationHistoryEntity, int> historyRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IRTSSmsNotificationService smsNotificationService,
        ILogger<RTSPaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _gatewayConfigRepository = gatewayConfigRepository;
        _statusRepository = statusRepository;
        _modeRepository = modeRepository;
        _webhookLogRepository = webhookLogRepository;
        _applicationRepository = applicationRepository;
        _serviceRepository = serviceRepository;
        _departmentRepository = departmentRepository;
        _fieldValueRepository = fieldValueRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _smsNotificationService = smsNotificationService;
        _logger = logger;
    }

    private async Task<RTSPaymentGatewayConfigEntity> GetActiveGatewayConfigAsync(string? requestedGatewayCode, CancellationToken ct)
    {
        var query = _gatewayConfigRepository.GetQueryable().Where(g => g.IsActive);

        if (!string.IsNullOrWhiteSpace(requestedGatewayCode))
        {
            var matched = await query.FirstOrDefaultAsync(g => g.GatewayCode.ToUpper() == requestedGatewayCode.ToUpper(), ct);
            if (matched != null) return matched;
        }

        var defaultGateway = await query.FirstOrDefaultAsync(g => g.IsDefault, ct)
                           ?? await query.FirstOrDefaultAsync(ct);

        if (defaultGateway == null)
        {
            throw new InvalidOperationException("No active Payment Gateway configuration found in RTS.PaymentGatewayConfig.");
        }

        return defaultGateway;
    }

    private static string GetGatewayBaseApiUrl(RTSPaymentGatewayConfigEntity gatewayConfig)
    {
        if (string.IsNullOrWhiteSpace(gatewayConfig.ServiceUrl))
            return "https://api.razorpay.com/v1";

        var url = gatewayConfig.ServiceUrl.Trim().TrimEnd('/');
        if (url.Contains("/checkout", StringComparison.OrdinalIgnoreCase) || url.Contains("/session", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return $"{uri.Scheme}://{uri.Authority}/v1";
            }
        }
        return url;
    }

    private async Task<RTSPaymentStatusMasterEntity> GetStatusByCodeAsync(string statusCode, CancellationToken ct)
    {
        var status = await _statusRepository.GetQueryable()
            .FirstOrDefaultAsync(s => s.StatusCode.ToUpper() == statusCode.ToUpper() && s.IsActive, ct);

        if (status == null)
        {
            throw new InvalidOperationException($"Payment status '{statusCode}' is not defined in RTS.PaymentStatusMaster.");
        }

        return status;
    }

    private async Task<int?> ResolvePaymentModeIdAsync(string? modeText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(modeText)) return null;

        var normalized = modeText.ToUpperInvariant();
        var modes = await _modeRepository.GetQueryable().Where(m => m.IsActive).ToListAsync(ct);

        if (normalized.Contains("UPI") || normalized.Contains("QR"))
            return modes.FirstOrDefault(m => m.ModeCode == "UPI")?.Id;
        if (normalized.Contains("NET") || normalized.Contains("BANK"))
            return modes.FirstOrDefault(m => m.ModeCode == "NETBANKING")?.Id;
        if (normalized.Contains("CREDIT"))
            return modes.FirstOrDefault(m => m.ModeCode == "CREDIT_CARD")?.Id;
        if (normalized.Contains("DEBIT") || normalized.Contains("CARD"))
            return modes.FirstOrDefault(m => m.ModeCode == "DEBIT_CARD")?.Id;
        if (normalized.Contains("WALLET"))
            return modes.FirstOrDefault(m => m.ModeCode == "WALLET")?.Id;

        return modes.FirstOrDefault(m => m.ModeCode == "UPI")?.Id;
    }

    public async Task<PaymentOrderResponseDto> CreatePaymentOrderAsync(CreatePaymentOrderRequestDto request, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.MarkedForDeletion, ct);

        if (app == null)
        {
            throw new ArgumentException($"Application with ID {request.ApplicationId} does not exist.");
        }

        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);
        var existingSuccess = await _paymentRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.ApplicationId == app.Id && t.PaymentStatusId == successStatus.Id, ct);

        if (existingSuccess != null)
        {
            throw new InvalidOperationException($"Fee for Application {app.ApplicationNo} has already been paid successfully under Receipt {existingSuccess.ReceiptNo}.");
        }

        // Dynamic Amount Logic from ServiceMaster
        decimal amount = app.Service?.Fees ?? 0;
        if (amount <= 0)
        {
            throw new InvalidOperationException($"No payable fee configured for service '{app.Service?.ServiceName}'.");
        }

        long amountInPaise = (long)Math.Round(amount * 100);

        // Dynamic Active Gateway from DB
        var gatewayConfig = await GetActiveGatewayConfigAsync(request.PaymentGateway, ct);
        var initiatedStatus = await GetStatusByCodeAsync("INITIATED", ct);

        // Dynamic Applicant details from FieldValues
        string? applicantName = request.CustomerName;
        string? applicantMobile = request.MobileNo;
        string? applicantEmail = request.Email;

        if (string.IsNullOrWhiteSpace(applicantName) || string.IsNullOrWhiteSpace(applicantMobile))
        {
            var fieldValues = await _fieldValueRepository.GetQueryable()
                .Include(f => f.FieldDefinition)
                .Where(f => f.ApplicationId == app.Id && !f.MarkedForDeletion)
                .ToListAsync(ct);

            foreach (var fv in fieldValues)
            {
                var label = (fv.FieldDefinition?.FieldLabel ?? fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var code = (fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var val = fv.TextValue?.Trim();

                if (string.IsNullOrWhiteSpace(val)) continue;

                if (string.IsNullOrWhiteSpace(applicantName) &&
                    (label.Contains("applicant") || label.Contains("name") || label.Contains("नाव") || code.Contains("name")))
                {
                    applicantName = val;
                }
                else if (string.IsNullOrWhiteSpace(applicantMobile) &&
                    (label.Contains("mobile") || label.Contains("phone") || label.Contains("contact") || label.Contains("मोबाईल") || code.Contains("mobile") || code.Contains("phone")))
                {
                    applicantMobile = val;
                }
                else if (string.IsNullOrWhiteSpace(applicantEmail) &&
                    (label.Contains("email") || label.Contains("mail") || label.Contains("ईमेल") || code.Contains("email")))
                {
                    applicantEmail = val;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(applicantName) && app.User != null)
        {
            applicantName = $"{app.User.FirstName} {app.User.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(applicantMobile)) applicantMobile = app.User.MobileNo;
            if (string.IsNullOrWhiteSpace(applicantEmail)) applicantEmail = app.User.Email;
        }

        string gatewayOrderId = string.Empty;

        // Connect directly to Razorpay Orders API
        try
        {
            var client = _httpClientFactory.CreateClient();
            var authBytes = Encoding.ASCII.GetBytes($"{gatewayConfig.KeyId}:{gatewayConfig.SecretKey}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var orderPayload = new
            {
                amount = amountInPaise,
                currency = gatewayConfig.Currency ?? "INR",
                receipt = $"RTS_{app.ApplicationNo ?? app.Id.ToString()}",
                notes = new
                {
                    applicationId = app.Id.ToString(),
                    applicationNo = app.ApplicationNo ?? "",
                    serviceId = app.ServiceId.ToString(),
                    serviceName = app.Service?.ServiceName ?? ""
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json");
            var orderEndpoint = $"{GetGatewayBaseApiUrl(gatewayConfig)}/orders";
            var response = await client.PostAsync(orderEndpoint, content, ct);

            if (response.IsSuccessStatusCode)
            {
                var resJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(resJson);
                if (doc.RootElement.TryGetProperty("id", out var orderIdProp))
                {
                    gatewayOrderId = orderIdProp.GetString() ?? "";
                }
            }
            else
            {
                var errContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Razorpay live order API error: {StatusCode} {Content}", response.StatusCode, errContent);
                throw new InvalidOperationException($"Gateway Error: Unable to generate order ({response.StatusCode}).");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed calling Razorpay API for application {AppId}", app.Id);
            throw;
        }

        if (string.IsNullOrWhiteSpace(gatewayOrderId))
        {
            throw new InvalidOperationException("Failed to receive valid order reference from Gateway.");
        }

        string transactionNo = $"TXN/RTS/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var transaction = new RTSPaymentTransactionEntity
        {
            TransactionNo = transactionNo,
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
            ServiceId = app.ServiceId,
            DepartmentId = app.DepartmentId,
            GatewayConfigId = gatewayConfig.Id,
            PaymentStatusId = initiatedStatus.Id,
            BaseAmount = amount,
            LateFeeAmount = 0,
            DiscountAmount = 0,
            TotalAmount = amount,
            Currency = gatewayConfig.Currency ?? "INR",
            GatewayOrderId = gatewayOrderId,
            Remarks = $"Order initiated via {gatewayConfig.GatewayName}",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        await _paymentRepository.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PaymentOrderResponseDto
        {
            Success = true,
            Message = "Payment order initiated successfully.",
            TransactionId = (int)transaction.Id,
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
            ServiceName = app.Service?.ServiceName ?? "RTS Service",
            DepartmentName = app.Department?.DepartmentName ?? "RTS Department",
            Amount = amount,
            AmountInPaise = amountInPaise,
            Currency = gatewayConfig.Currency ?? "INR",
            Gateway = gatewayConfig.GatewayCode,
            GatewayOrderId = gatewayOrderId,
            KeyId = gatewayConfig.KeyId,
            Description = $"Government Service Fee for {app.Service?.ServiceName ?? "RTS Service"}",
            CustomerName = applicantName,
            CustomerEmail = applicantEmail,
            CustomerMobile = applicantMobile
        };
    }

    public async Task<VerifyPaymentResponseDto> VerifyPaymentAsync(VerifyPaymentRequestDto request, CancellationToken ct = default)
    {
        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.GatewayConfig)
            .Include(t => t.PaymentStatus)
            .FirstOrDefaultAsync(t => t.ApplicationId == request.ApplicationId && t.GatewayOrderId == request.GatewayOrderId, ct);

        if (txn == null)
        {
            txn = await _paymentRepository.GetQueryable()
                .Include(t => t.GatewayConfig)
                .Include(t => t.PaymentStatus)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(t => t.ApplicationId == request.ApplicationId, ct);
        }

        if (txn == null)
        {
            throw new ArgumentException("No matching payment transaction found for this application order.");
        }

        var gatewayConfig = txn.GatewayConfig ?? await GetActiveGatewayConfigAsync(null, ct);
        string secret = gatewayConfig.SecretKey;

        // Verify cryptographic HMAC-SHA256 signature
        string payload = $"{request.GatewayOrderId}|{request.GatewayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string generatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        bool isSignatureValid = string.Equals(generatedSignature, request.GatewaySignature?.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!isSignatureValid)
        {
            var failedStatus = await GetStatusByCodeAsync("FAILED", ct);
            txn.PaymentStatusId = failedStatus.Id;
            txn.FailureReason = "Digital HMAC-SHA256 signature verification mismatch";
            txn.Remarks = "Client returned signature could not be verified by server key.";
            txn.UpdatedDate = DateTime.Now;
            await _unitOfWork.SaveChangesAsync(ct);

            return new VerifyPaymentResponseDto
            {
                Success = false,
                Message = "Payment verification failed: Digital signature mismatch.",
                ApplicationNo = txn.ApplicationNo,
                Amount = txn.TotalAmount,
                PaymentStatus = "FAILED"
            };
        }

        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);
        string receiptNo = $"REC/RTS/{DateTime.Now:yyyyMMdd}/{txn.Id:D6}";

        txn.GatewayPaymentId = request.GatewayPaymentId;
        txn.GatewaySignature = request.GatewaySignature;
        txn.PaymentStatusId = successStatus.Id;
        txn.PaymentModeId = await ResolvePaymentModeIdAsync(request.PaymentMode, ct);
        txn.ReceiptNo = receiptNo;
        txn.ReceiptDate = DateTime.Now;
        txn.PaymentDate = DateTime.Now;
        txn.Remarks = $"Verified successfully via {gatewayConfig.GatewayName}. Payment ID: {request.GatewayPaymentId}";
        txn.UpdatedDate = DateTime.Now;

        // Fetch detailed payment data from Razorpay API for ERP ledger reconciliation
        try
        {
            if (!string.IsNullOrWhiteSpace(gatewayConfig.KeyId) && !string.IsNullOrWhiteSpace(gatewayConfig.SecretKey) && !string.IsNullOrWhiteSpace(request.GatewayPaymentId))
            {
                var client = _httpClientFactory.CreateClient();
                var authBytes = Encoding.ASCII.GetBytes($"{gatewayConfig.KeyId}:{gatewayConfig.SecretKey}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                var paymentEndpoint = $"{GetGatewayBaseApiUrl(gatewayConfig)}/payments/{request.GatewayPaymentId}";
                var rzpRes = await client.GetAsync(paymentEndpoint, ct);
                if (rzpRes.IsSuccessStatusCode)
                {
                    var rzpJson = await rzpRes.Content.ReadAsStringAsync(ct);
                    txn.GatewayResponseJson = rzpJson;

                    using var doc = JsonDocument.Parse(rzpJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("fee", out var feeProp) && feeProp.TryGetInt64(out var feePaise))
                        txn.GatewayFee = (decimal)feePaise / 100m;

                    if (root.TryGetProperty("tax", out var taxProp) && taxProp.TryGetInt64(out var taxPaise))
                        txn.GatewayTax = (decimal)taxPaise / 100m;

                    if (root.TryGetProperty("method", out var methodProp))
                    {
                        var method = methodProp.GetString();
                        if (string.Equals(method, "upi", StringComparison.OrdinalIgnoreCase) && root.TryGetProperty("vpa", out var vpaProp))
                            txn.PayerVpaOrAccount = vpaProp.GetString();
                        else if (string.Equals(method, "bank_transfer", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "netbanking", StringComparison.OrdinalIgnoreCase))
                        {
                            if (root.TryGetProperty("bank", out var bankProp))
                                txn.PayerVpaOrAccount = bankProp.GetString();
                        }
                        else if (root.TryGetProperty("card", out var cardProp) && cardProp.TryGetProperty("last4", out var last4Prop))
                        {
                            txn.PayerVpaOrAccount = $"Card **** {last4Prop.GetString()}";
                        }
                    }

                    if (root.TryGetProperty("acquirer_data", out var acqProp))
                    {
                        if (acqProp.TryGetProperty("rrn", out var rrnProp))
                            txn.BankRefNo = rrnProp.GetString();
                        else if (acqProp.TryGetProperty("bank_transaction_id", out var btxProp))
                            txn.BankRefNo = btxProp.GetString();
                        else if (acqProp.TryGetProperty("upi_transaction_id", out var upiTxProp))
                            txn.BankRefNo = upiTxProp.GetString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch additional payment details from gateway API for payment {PaymentId}", request.GatewayPaymentId);
        }

        // Advance application workflow state
        var app = await _applicationRepository.GetByIdAsync(txn.ApplicationId, ct);
        if (app != null)
        {
            if (string.Equals(app.ApplicationStatus, "Payment Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(app.ApplicationStatus, "Pending Payment", StringComparison.OrdinalIgnoreCase))
            {
                app.ApplicationStatus = "In Progress";
                app.UpdatedDate = DateTime.Now;
            }

            var history = new TrackApplicationHistoryEntity
            {
                ApplicationId = app.Id,
                ApprovalFlowId = app.ApprovalFlowId,
                ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                Action = "Payment Received",
                Status = "Payment Done",
                Remark = $"Government Fee of ₹{txn.TotalAmount:F2} received via {gatewayConfig.GatewayName}. Ref: {request.GatewayPaymentId}, Receipt: {receiptNo}",
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            await _historyRepository.AddAsync(history, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Asynchronous SMS dispatch to applicant
        _ = Task.Run(async () =>
        {
            try
            {
                var receipt = await GetPaymentReceiptByApplicationIdAsync(txn.ApplicationId, CancellationToken.None);
                if (receipt != null && !string.IsNullOrWhiteSpace(receipt.CustomerMobile))
                {
                    await _smsNotificationService.SendPaymentSuccessAsync(
                        receipt.ApplicationId,
                        receipt.ApplicationNo,
                        receipt.CustomerName ?? "Citizen",
                        receipt.CustomerMobile,
                        receipt.Amount,
                        receipt.ReceiptNo,
                        CancellationToken.None);
                }
            }
            catch (Exception smsEx)
            {
                _logger.LogError(smsEx, "Failed to send payment receipt SMS for application {AppId}", txn.ApplicationId);
            }
        });

        return new VerifyPaymentResponseDto
        {
            Success = true,
            Message = "Payment verified successfully. Official digital receipt generated.",
            ReceiptNo = receiptNo,
            TransactionId = txn.Id.ToString(),
            ApplicationNo = txn.ApplicationNo,
            Amount = txn.TotalAmount,
            PaymentDate = txn.PaymentDate,
            PaymentStatus = "SUCCESS"
        };
    }

    public async Task<bool> ProcessWebhookAsync(string webhookPayload, string? signatureHeader, CancellationToken ct = default)
    {
        var gatewayConfig = await GetActiveGatewayConfigAsync(null, ct);
        string secret = gatewayConfig.WebhookSecret ?? gatewayConfig.SecretKey;

        // Verify Webhook Signature
        bool isSignatureValid = false;
        if (!string.IsNullOrWhiteSpace(signatureHeader) && !string.IsNullOrWhiteSpace(secret))
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(webhookPayload));
            string computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            isSignatureValid = string.Equals(computedSignature, signatureHeader.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        string eventType = "unknown";
        string? eventId = null;
        string? orderId = null;
        string? paymentId = null;
        string? bankRef = null;
        decimal? gatewayFee = null;
        decimal? gatewayTax = null;
        string? payerVpaOrAccount = null;

        try
        {
            using var doc = JsonDocument.Parse(webhookPayload);
            var root = doc.RootElement;
            if (root.TryGetProperty("event", out var evProp)) eventType = evProp.GetString() ?? "unknown";
            if (root.TryGetProperty("id", out var idProp)) eventId = idProp.GetString();

            if (root.TryGetProperty("payload", out var payloadObj))
            {
                if (payloadObj.TryGetProperty("payment", out var paymentObj) &&
                    paymentObj.TryGetProperty("entity", out var entityObj))
                {
                    if (entityObj.TryGetProperty("id", out var pId)) paymentId = pId.GetString();
                    if (entityObj.TryGetProperty("order_id", out var oId)) orderId = oId.GetString();
                    if (entityObj.TryGetProperty("fee", out var feeProp) && feeProp.TryGetInt64(out var fPaise))
                        gatewayFee = (decimal)fPaise / 100m;
                    if (entityObj.TryGetProperty("tax", out var taxProp) && taxProp.TryGetInt64(out var tPaise))
                        gatewayTax = (decimal)tPaise / 100m;
                    if (entityObj.TryGetProperty("vpa", out var vpaProp))
                        payerVpaOrAccount = vpaProp.GetString();
                    else if (entityObj.TryGetProperty("bank", out var bankProp))
                        payerVpaOrAccount = bankProp.GetString();

                    if (entityObj.TryGetProperty("acquirer_data", out var acqData))
                    {
                        if (acqData.TryGetProperty("rrn", out var rrnProp))
                            bankRef = rrnProp.GetString();
                        else if (acqData.TryGetProperty("bank_transaction_id", out var bId))
                            bankRef = bId.GetString();
                        else if (acqData.TryGetProperty("upi_transaction_id", out var upiId))
                            bankRef = upiId.GetString();
                    }
                }
                else if (payloadObj.TryGetProperty("order", out var orderObj) &&
                         orderObj.TryGetProperty("entity", out var ordEntity))
                {
                    if (ordEntity.TryGetProperty("id", out var oId)) orderId = oId.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed parsing webhook payload JSON");
        }

        var webhookLog = new RTSPaymentWebhookLogEntity
        {
            GatewayConfigId = gatewayConfig.Id,
            EventId = eventId,
            EventType = eventType,
            SignatureHeader = signatureHeader,
            PayloadJson = webhookPayload,
            IsSignatureValid = isSignatureValid,
            ReceivedDate = DateTime.Now
        };

        if (!isSignatureValid)
        {
            webhookLog.IsProcessed = false;
            webhookLog.ProcessingError = "Signature verification failed.";
            await _webhookLogRepository.AddAsync(webhookLog, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return false;
        }

        // Handle Event Idempotently
        if ((eventType == "payment.captured" || eventType == "order.paid") && !string.IsNullOrWhiteSpace(orderId))
        {
            var txn = await _paymentRepository.GetQueryable()
                .Include(t => t.PaymentStatus)
                .FirstOrDefaultAsync(t => t.GatewayOrderId == orderId, ct);

            if (txn != null && txn.PaymentStatus?.StatusCode != "SUCCESS")
            {
                var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);
                string receiptNo = $"REC/RTS/{DateTime.Now:yyyyMMdd}/{txn.Id:D6}";

                txn.PaymentStatusId = successStatus.Id;
                txn.GatewayPaymentId = paymentId ?? txn.GatewayPaymentId;
                txn.BankRefNo = bankRef ?? txn.BankRefNo;
                txn.GatewayFee = gatewayFee ?? txn.GatewayFee;
                txn.GatewayTax = gatewayTax ?? txn.GatewayTax;
                txn.PayerVpaOrAccount = payerVpaOrAccount ?? txn.PayerVpaOrAccount;
                txn.GatewayResponseJson = webhookPayload;
                txn.ReceiptNo = receiptNo;
                txn.ReceiptDate = DateTime.Now;
                txn.PaymentDate = DateTime.Now;
                txn.Remarks = $"Processed via Webhook event: {eventType}";
                txn.UpdatedDate = DateTime.Now;

                var app = await _applicationRepository.GetByIdAsync(txn.ApplicationId, ct);
                if (app != null)
                {
                    app.ApplicationStatus = "In Progress";
                    app.UpdatedDate = DateTime.Now;

                    var history = new TrackApplicationHistoryEntity
                    {
                        ApplicationId = app.Id,
                        ApprovalFlowId = app.ApprovalFlowId,
                        ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                        Action = "Payment Received (Webhook)",
                        Status = "Payment Done",
                        Remark = $"Fee of ₹{txn.TotalAmount:F2} confirmed by Webhook ({eventType}). Ref: {paymentId}, Receipt: {receiptNo}",
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };
                    await _historyRepository.AddAsync(history, ct);
                }

                webhookLog.IsProcessed = true;
                webhookLog.ProcessedDate = DateTime.Now;
            }
        }
        else if (eventType == "payment.failed" && !string.IsNullOrWhiteSpace(orderId))
        {
            var txn = await _paymentRepository.GetQueryable().FirstOrDefaultAsync(t => t.GatewayOrderId == orderId, ct);
            if (txn != null)
            {
                var failedStatus = await GetStatusByCodeAsync("FAILED", ct);
                txn.PaymentStatusId = failedStatus.Id;
                txn.FailureReason = "Payment failed as per gateway webhook event notification.";
                txn.UpdatedDate = DateTime.Now;
                webhookLog.IsProcessed = true;
                webhookLog.ProcessedDate = DateTime.Now;
            }
        }

        await _webhookLogRepository.AddAsync(webhookLog, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PaymentReceiptDto?> GetPaymentReceiptByApplicationIdAsync(int applicationId, CancellationToken ct = default)
    {
        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);

        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.Service)
            .Include(t => t.Department)
            .Include(t => t.Application)
            .Include(t => t.GatewayConfig)
            .Include(t => t.PaymentMode)
            .Include(t => t.PaymentStatus)
            .Where(t => t.ApplicationId == applicationId && t.PaymentStatusId == successStatus.Id)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (txn == null) return null;

        // Dynamic applicant info
        var fieldValues = await _fieldValueRepository.GetQueryable()
            .Include(f => f.FieldDefinition)
            .Where(f => f.ApplicationId == applicationId && !f.MarkedForDeletion)
            .ToListAsync(ct);

        string? customerName = null;
        string? customerMobile = null;

        foreach (var fv in fieldValues)
        {
            var label = (fv.FieldDefinition?.FieldLabel ?? fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
            var code = (fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
            var val = fv.TextValue?.Trim();

            if (string.IsNullOrWhiteSpace(val)) continue;

            if (string.IsNullOrWhiteSpace(customerName) &&
                (label.Contains("applicant") || label.Contains("name") || label.Contains("नाव") || code.Contains("name")))
            {
                customerName = val;
            }
            else if (string.IsNullOrWhiteSpace(customerMobile) &&
                (label.Contains("mobile") || label.Contains("phone") || label.Contains("contact") || label.Contains("मोबाईल") || code.Contains("mobile") || code.Contains("phone")))
            {
                customerMobile = val;
            }
        }

        return new PaymentReceiptDto
        {
            TransactionId = (int)txn.Id,
            ApplicationId = txn.ApplicationId,
            ApplicationNo = txn.ApplicationNo,
            ServiceName = txn.Service?.ServiceName ?? "RTS Service",
            ServiceNameLocal = txn.Service?.ServiceNameLocal ?? txn.Service?.ServiceName ?? "",
            DepartmentName = txn.Department?.DepartmentName ?? "RTS Department",
            DepartmentNameLocal = txn.Department?.DepartmentNameLocal ?? txn.Department?.DepartmentName ?? "",
            Amount = txn.TotalAmount,
            Currency = txn.Currency,
            PaymentGateway = txn.GatewayConfig?.GatewayName ?? "Razorpay",
            GatewayPaymentId = txn.GatewayPaymentId ?? "",
            ReceiptNo = txn.ReceiptNo ?? $"REC/RTS/{txn.Id:D6}",
            PaymentDate = txn.PaymentDate ?? txn.ReceiptDate,
            PaymentStatus = txn.PaymentStatus?.StatusNameEn ?? "Success",
            PaymentMode = txn.PaymentMode?.ModeNameEn ?? "Online Gateway",
            CustomerName = customerName ?? "Applicant",
            CustomerMobile = customerMobile
        };
    }

    public async Task<PaymentReceiptDto?> GetPaymentReceiptByReceiptNoAsync(string receiptNo, CancellationToken ct = default)
    {
        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);

        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.Service)
            .Include(t => t.Department)
            .Include(t => t.Application)
            .Include(t => t.GatewayConfig)
            .Include(t => t.PaymentMode)
            .Include(t => t.PaymentStatus)
            .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo && t.PaymentStatusId == successStatus.Id, ct);

        if (txn == null) return null;

        return await GetPaymentReceiptByApplicationIdAsync(txn.ApplicationId, ct);
    }

    public async Task<object?> GetPaymentStatusAsync(int applicationId, CancellationToken ct = default)
    {
        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.PaymentStatus)
            .Include(t => t.GatewayConfig)
            .Where(t => t.ApplicationId == applicationId)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(ct);

        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (app == null) return null;

        return new
        {
            applicationId = app.Id,
            applicationNo = app.ApplicationNo,
            serviceName = app.Service?.ServiceName,
            requiredFee = app.Service?.Fees ?? 0,
            isFeeRequired = app.Service?.FeesRequired ?? false,
            paymentStatus = txn?.PaymentStatus?.StatusCode ?? (app.Service?.FeesRequired == true ? "PENDING" : "NOT_REQUIRED"),
            statusNameEn = txn?.PaymentStatus?.StatusNameEn,
            statusNameMr = txn?.PaymentStatus?.StatusNameMr,
            badgeColor = txn?.PaymentStatus?.BadgeColor,
            receiptNo = txn?.ReceiptNo,
            paymentDate = txn?.PaymentDate,
            gatewayPaymentId = txn?.GatewayPaymentId
        };
    }
}
