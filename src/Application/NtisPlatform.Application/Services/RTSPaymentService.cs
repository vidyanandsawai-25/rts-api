using System.Linq;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RTSPayment;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
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
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<BankMasterEntity, int> _bankRepository;
    private readonly IRepository<RTSReceiptCancellationLogEntity, int> _cancellationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly IUlbConfigService _ulbConfigService;
    private readonly IAapleSarkarIntegrationService _aapleSarkarService;
    private readonly IRTSRuleMasterService _ruleMasterService;
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
        IRepository<UserEntity, int> userRepository,
        IRepository<BankMasterEntity, int> bankRepository,
        IRepository<RTSReceiptCancellationLogEntity, int> cancellationLogRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IRTSSmsNotificationService smsNotificationService,
        IUlbConfigService ulbConfigService,
        IAapleSarkarIntegrationService aapleSarkarService,
        IRTSRuleMasterService ruleMasterService,
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
        _userRepository = userRepository;
        _bankRepository = bankRepository;
        _cancellationLogRepository = cancellationLogRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _smsNotificationService = smsNotificationService;
        _ulbConfigService = ulbConfigService;
        _aapleSarkarService = aapleSarkarService;
        _ruleMasterService = ruleMasterService;
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

        var normalized = modeText.Trim().ToUpperInvariant();
        var modes = await _modeRepository.GetQueryable().Where(m => m.IsActive).ToListAsync(ct);

        // Match exact ModeCode or ModeName
        var exactMode = modes.FirstOrDefault(m =>
            string.Equals(m.ModeCode, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ModeNameEn, modeText.Trim(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ModeNameMr, modeText.Trim(), StringComparison.OrdinalIgnoreCase));

        if (exactMode != null) return exactMode.Id;

        // Auto-resolve or create Cash mode
        if (normalized.Contains("CASH") || normalized.Contains("रोख"))
        {
            var cashMode = modes.FirstOrDefault(m => m.ModeCode == "CASH");
            if (cashMode == null)
            {
                cashMode = new RTSPaymentModeMasterEntity
                {
                    ModeCode = "CASH",
                    ModeNameEn = "Cash",
                    ModeNameMr = "रोख",
                    IconName = "Banknote",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                await _modeRepository.AddAsync(cashMode, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            return cashMode.Id;
        }

        // Auto-resolve or create Cheque mode
        if (normalized.Contains("CHEQUE") || normalized.Contains("धनादेश"))
        {
            var chequeMode = modes.FirstOrDefault(m => m.ModeCode == "CHEQUE");
            if (chequeMode == null)
            {
                chequeMode = new RTSPaymentModeMasterEntity
                {
                    ModeCode = "CHEQUE",
                    ModeNameEn = "Cheque (धनादेश)",
                    ModeNameMr = "धनादेश",
                    IconName = "FileText",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                await _modeRepository.AddAsync(chequeMode, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            return chequeMode.Id;
        }

        // Auto-resolve or create Demand Draft mode
        if (normalized.Contains("DD") || normalized.Contains("DEMAND"))
        {
            var ddMode = modes.FirstOrDefault(m => m.ModeCode == "DD");
            if (ddMode == null)
            {
                ddMode = new RTSPaymentModeMasterEntity
                {
                    ModeCode = "DD",
                    ModeNameEn = "Demand Draft (डीडी)",
                    ModeNameMr = "डिमांड ड्राफ्ट",
                    IconName = "FileText",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                await _modeRepository.AddAsync(ddMode, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            return ddMode.Id;
        }

        // Auto-resolve or create POS mode
        if (normalized.Contains("POS") || normalized.Contains("SWIPE"))
        {
            var posMode = modes.FirstOrDefault(m => m.ModeCode == "POS");
            if (posMode == null)
            {
                posMode = new RTSPaymentModeMasterEntity
                {
                    ModeCode = "POS",
                    ModeNameEn = "POS Swipe Machine",
                    ModeNameMr = "पीओएस मशीन",
                    IconName = "CreditCard",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                await _modeRepository.AddAsync(posMode, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            return posMode.Id;
        }

        if (normalized.Contains("UPI") || normalized.Contains("QR") || normalized.Contains("GPAY") || normalized.Contains("PHONEPE"))
            return modes.FirstOrDefault(m => m.ModeCode == "UPI")?.Id;

        if (normalized.Contains("NET") || normalized.Contains("BANK") || normalized.Contains("NEFT") || normalized.Contains("RTGS"))
            return modes.FirstOrDefault(m => m.ModeCode == "NETBANKING")?.Id;

        if (normalized.Contains("CREDIT"))
            return modes.FirstOrDefault(m => m.ModeCode == "CREDIT_CARD")?.Id;

        if (normalized.Contains("DEBIT") || normalized.Contains("CARD"))
            return modes.FirstOrDefault(m => m.ModeCode == "DEBIT_CARD")?.Id;

        if (normalized.Contains("WALLET"))
            return modes.FirstOrDefault(m => m.ModeCode == "WALLET")?.Id;

        if (normalized.Contains("CHALLAN"))
            return modes.FirstOrDefault(m => m.ModeCode == "OFFLINE_CHALLAN")?.Id;

        return modes.FirstOrDefault()?.Id;
    }

    private async Task<RTSPaymentTransactionEntity?> GetLatestTransactionAsync(int applicationId, CancellationToken ct)
    {
        return await _paymentRepository.GetQueryable()
            .Include(t => t.PaymentStatus)
            .Include(t => t.GatewayConfig)
            .Where(t => t.ApplicationId == applicationId)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<RTSPaymentTransactionEntity?> CreatePendingPaymentSnapshotAsync(
        RTSApplicationDetailsEntity app,
        decimal? amountOverride,
        CancellationToken ct)
    {
        var amount = amountOverride;
        if (!amount.HasValue)
        {
            var fieldValues = await _fieldValueRepository.GetQueryable()
                .Include(fieldValue => fieldValue.FieldDefinition)
                .Where(fieldValue => fieldValue.ApplicationId == app.Id && !fieldValue.MarkedForDeletion)
                .ToListAsync(ct);

            var feeEvaluation = await _ruleMasterService.EvaluateFeeAsync(
                new RTSFeeEvaluationRequestDto
                {
                    ServiceId = app.ServiceId,
                    FieldValues = fieldValues.Select(fieldValue => new RTSFeeFieldValueDto
                    {
                        FieldDefinitionId = fieldValue.FieldDefinitionId,
                        FieldCode = fieldValue.FieldDefinition?.FieldCode,
                        Value = fieldValue.TextValue
                            ?? fieldValue.NumberValue?.ToString(CultureInfo.InvariantCulture)
                            ?? fieldValue.DateValue?.ToString("O", CultureInfo.InvariantCulture)
                            ?? fieldValue.BooleanValue?.ToString()
                    }).ToList()
                },
                ct);

            amount = feeEvaluation.PayableAmount;
        }

        var payableAmount = Math.Round(amount ?? 0m, 2, MidpointRounding.AwayFromZero);
        if (payableAmount <= 0)
        {
            return null;
        }

        var pendingStatus = await GetStatusByCodeAsync("PENDING", ct);
        var gatewayConfig = await GetActiveGatewayConfigAsync(null, ct);
        var transaction = new RTSPaymentTransactionEntity
        {
            TransactionNo = $"TXN/RTS/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
            ServiceId = app.ServiceId,
            DepartmentId = app.DepartmentId,
            GatewayConfigId = gatewayConfig.Id,
            PaymentStatusId = pendingStatus.Id,
            BaseAmount = payableAmount,
            LateFeeAmount = 0,
            DiscountAmount = 0,
            TotalAmount = payableAmount,
            Currency = gatewayConfig.Currency ?? "INR",
            GatewayOrderId = string.Empty,
            Remarks = "Payment snapshot created at application submission.",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        await _paymentRepository.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return transaction;
    }

    private static PaymentStatusDto MapPaymentStatus(
        RTSApplicationDetailsEntity app,
        RTSPaymentTransactionEntity? transaction)
    {
        return new PaymentStatusDto
        {
            TransactionId = transaction?.Id,
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
            ServiceName = app.Service?.ServiceName ?? "RTS Service",
            RequiredFee = transaction?.TotalAmount ?? 0m,
            IsFeeRequired = transaction?.TotalAmount > 0m,
            HasPaymentTransaction = transaction != null,
            PaymentStatus = transaction?.PaymentStatus?.StatusCode ?? "NOT_REQUIRED",
            StatusNameEn = transaction?.PaymentStatus?.StatusNameEn,
            StatusNameMr = transaction?.PaymentStatus?.StatusNameMr,
            BadgeColor = transaction?.PaymentStatus?.BadgeColor,
            ReceiptNo = transaction?.ReceiptNo,
            PaymentDate = transaction?.PaymentDate,
            GatewayPaymentId = transaction?.GatewayPaymentId
        };
    }

    public async Task<PaymentStatusDto> InitializePendingPaymentAsync(int applicationId, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == applicationId && !a.MarkedForDeletion, ct);

        if (app == null)
        {
            throw new ArgumentException($"Application with ID {applicationId} does not exist.");
        }

        var transaction = await GetLatestTransactionAsync(app.Id, ct);
        if (transaction == null)
        {
            transaction = await CreatePendingPaymentSnapshotAsync(app, null, ct);
            if (transaction != null)
            {
                transaction = await GetLatestTransactionAsync(app.Id, ct);
            }
        }

        return MapPaymentStatus(app, transaction);
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

        var fieldValues = await _fieldValueRepository.GetQueryable()
            .Include(fieldValue => fieldValue.FieldDefinition)
            .Where(fieldValue => fieldValue.ApplicationId == app.Id && !fieldValue.MarkedForDeletion)
            .ToListAsync(ct);

        var transaction = await GetLatestTransactionAsync(app.Id, ct);
        if (transaction == null)
        {
            await InitializePendingPaymentAsync(app.Id, ct);
            transaction = await GetLatestTransactionAsync(app.Id, ct);
        }

        var currentStatus = transaction?.PaymentStatus?.StatusCode?.ToUpperInvariant();
        if (currentStatus is "FAILED" or "EXPIRED")
        {
            await CreatePendingPaymentSnapshotAsync(app, transaction!.TotalAmount, ct);
            transaction = await GetLatestTransactionAsync(app.Id, ct);
            currentStatus = transaction?.PaymentStatus?.StatusCode?.ToUpperInvariant();
        }

        if (transaction == null || transaction.TotalAmount <= 0)
        {
            throw new InvalidOperationException($"No payable fee configured for service '{app.Service?.ServiceName}'.");
        }

        if (currentStatus is not ("PENDING" or "INITIATED"))
        {
            throw new InvalidOperationException(
                $"Payment for Application {app.ApplicationNo} cannot be initiated from status '{currentStatus ?? "UNKNOWN"}'.");
        }

        decimal amount = transaction.TotalAmount;
        long amountInPaise = decimal.ToInt64(amount * 100m);

        // Dynamic Applicant details from FieldValues
        string? applicantName = request.CustomerName;
        string? applicantMobile = request.MobileNo;
        string? applicantEmail = request.Email;

        if (string.IsNullOrWhiteSpace(applicantName) || string.IsNullOrWhiteSpace(applicantMobile))
        {
            foreach (var fv in fieldValues)
            {
                var label = (fv.FieldDefinition?.FieldLabel ?? fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var code = (fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var val = fv.TextValue?.Trim();

                if (string.IsNullOrWhiteSpace(val)) continue;

                if (string.IsNullOrWhiteSpace(applicantName) &&
                    (label.Contains("full name") || label.Contains("name") || label.Contains("नाव") || code.Contains("name")))
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

        if (string.IsNullOrWhiteSpace(applicantEmail))
        {
            applicantEmail = !string.IsNullOrWhiteSpace(applicantMobile)
                ? $"{applicantMobile}@citizen.portal"
                : $"citizen_{app.ApplicationNo?.Replace("/", "_") ?? app.Id.ToString()}@citizen.portal";
        }

        if (currentStatus == "INITIATED" && !string.IsNullOrWhiteSpace(transaction.GatewayOrderId))
        {
            var existingGateway = transaction.GatewayConfig
                ?? await _gatewayConfigRepository.GetByIdAsync(transaction.GatewayConfigId, ct)
                ?? throw new InvalidOperationException("The payment gateway for this transaction is unavailable.");

            return new PaymentOrderResponseDto
            {
                Success = true,
                Message = "Existing payment order retrieved successfully.",
                TransactionId = (int)transaction.Id,
                ApplicationId = app.Id,
                ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
                ServiceName = app.Service?.ServiceName ?? "RTS Service",
                DepartmentName = app.Department?.DepartmentName ?? "RTS Department",
                Amount = amount,
                AmountInPaise = amountInPaise,
                Currency = transaction.Currency,
                Gateway = existingGateway.GatewayCode,
                GatewayOrderId = transaction.GatewayOrderId,
                KeyId = existingGateway.KeyId,
                Description = $"Government Service Fee for {app.Service?.ServiceName ?? "RTS Service"}",
                CustomerName = applicantName,
                CustomerEmail = applicantEmail,
                CustomerMobile = applicantMobile
            };
        }

        var gatewayConfig = await GetActiveGatewayConfigAsync(request.PaymentGateway, ct);
        var initiatedStatus = await GetStatusByCodeAsync("INITIATED", ct);

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

        transaction.GatewayConfigId = gatewayConfig.Id;
        transaction.PaymentStatusId = initiatedStatus.Id;
        transaction.Currency = gatewayConfig.Currency ?? "INR";
        transaction.GatewayOrderId = gatewayOrderId;
        transaction.Remarks = $"Order initiated via {gatewayConfig.GatewayName}";
        transaction.UpdatedDate = DateTime.Now;
        await _paymentRepository.UpdateAsync(transaction, ct);
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
                        if (!txn.PaymentModeId.HasValue || txn.PaymentModeId == 0)
                        {
                            txn.PaymentModeId = await ResolvePaymentModeIdAsync(method, ct);
                        }

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
                app.ApplicationStatus = "Pending";
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

            // Notify Aaple Sarkar: PaymentDone -> UnderScrutiny (populates Payment Date and enters scrutiny)
            if (!string.IsNullOrWhiteSpace(app.ApplicationNo))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _aapleSarkarService.UpdateStatusAsync(app.ApplicationNo, "PaymentDone", ct: CancellationToken.None);
                        await Task.Delay(1000);
                        await _aapleSarkarService.UpdateStatusAsync(app.ApplicationNo, "UnderScrutiny", ct: CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update Aaple Sarkar status after payment for {AppNo}", app.ApplicationNo);
                    }
                });
            }
        }

        // Pre-resolve applicant contact for SMS dispatch
        string? smsMobile = null;
        string? smsName = null;
        try
        {
            var fieldValues = await _fieldValueRepository.GetQueryable()
                .Include(f => f.FieldDefinition)
                .Where(f => f.ApplicationId == txn.ApplicationId && !f.MarkedForDeletion)
                .ToListAsync(ct);

            foreach (var fv in fieldValues)
            {
                var label = (fv.FieldDefinition?.FieldLabel ?? fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var code = (fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var val = (!string.IsNullOrWhiteSpace(fv.TextValue) ? fv.TextValue : fv.NumberValue?.ToString())?.Trim();

                if (string.IsNullOrWhiteSpace(val)) continue;

                if (string.IsNullOrWhiteSpace(smsMobile) &&
                    (label.Contains("mobile") || label.Contains("phone") || label.Contains("contact") || label.Contains("मोबाईल") || label.Contains("फोन") || code.Contains("mobile") || code.Contains("phone")))
                {
                    smsMobile = val;
                }
                else if (string.IsNullOrWhiteSpace(smsName) &&
                    (label.Contains("Full Name") || label.Contains("name") || label.Contains("नाव") || code.Contains("name") || code.Contains("fullname")))
                {
                    smsName = val;
                }
            }

            // Fallback: Check Razorpay gateway contact if available
            if (string.IsNullOrWhiteSpace(smsMobile) && !string.IsNullOrWhiteSpace(txn.GatewayResponseJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(txn.GatewayResponseJson);
                    if (doc.RootElement.TryGetProperty("contact", out var cProp))
                    {
                        var cStr = cProp.GetString();
                        if (!string.IsNullOrWhiteSpace(cStr))
                        {
                            smsMobile = cStr.Replace("+91", "").Trim();
                        }
                    }
                }
                catch { }
            }
        }
        catch { }

        await _unitOfWork.SaveChangesAsync(ct);

        // Reliable inline SMS dispatch to applicant
        if (!string.IsNullOrWhiteSpace(smsMobile))
        {
            try
            {
                await _smsNotificationService.SendPaymentSuccessAsync(
                    txn.ApplicationId,
                    txn.ApplicationNo ?? $"RTS{txn.ApplicationId}",
                    smsName ?? "Citizen",
                    smsMobile,
                    txn.TotalAmount,
                    receiptNo,
                    ct);
            }
            catch (Exception smsEx)
            {
                _logger.LogError(smsEx, "Failed to send payment receipt SMS for application {AppId}", txn.ApplicationId);
            }
        }

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

    public async Task<PaymentReceiptDto> RecordOfflinePaymentAsync(RecordOfflinePaymentRequestDto request, int officerUserId, CancellationToken ct = default)
    {
        if (request == null || request.ApplicationId <= 0)
            throw new ArgumentException("Valid Application ID is required.");

        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.IsActive && !a.MarkedForDeletion, ct);

        if (app == null)
            throw new ArgumentException($"Application with ID {request.ApplicationId} not found.");

        // Check if already paid or has a payment pending clearance
        var activePayment = await _paymentRepository.GetQueryable()
            .Include(p => p.PaymentStatus)
            .Where(p => p.ApplicationId == request.ApplicationId && p.IsActive)
            .Where(p => p.PaymentStatus.StatusCode == "SUCCESS" || p.PaymentStatus.StatusCode == "PENDING_CLEARANCE")
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (activePayment != null)
        {
            if (activePayment.PaymentStatus?.StatusCode == "SUCCESS")
            {
                throw new InvalidOperationException($"Application {app.ApplicationNo} is already paid (Receipt: {activePayment.ReceiptNo}).");
            }
            if (activePayment.PaymentStatus?.StatusCode == "PENDING_CLEARANCE")
            {
                throw new InvalidOperationException($"Application {app.ApplicationNo} already has a payment awaiting clearance (Receipt: {activePayment.ReceiptNo}, Instrument: {activePayment.InstrumentNo}). Duplicate payment cannot be recorded.");
            }
        }

        // The amount was frozen when the application was submitted. Do not allow a
        // counter operator (or a stale client) to replace the evaluated fee with the
        // current Service Master amount.
        var txn = await GetLatestTransactionAsync(app.Id, ct);
        if (txn == null)
        {
            await InitializePendingPaymentAsync(app.Id, ct);
            txn = await GetLatestTransactionAsync(app.Id, ct);
        }

        if (txn == null || txn.TotalAmount <= 0)
        {
            decimal officialFee = app.Service?.Fees ?? 0;
            if (officialFee > 0 && txn != null)
            {
                txn.TotalAmount = officialFee;
                txn.BaseAmount = officialFee;
            }
            else
            {
                throw new InvalidOperationException("This application does not have a payable payment snapshot.");
            }
        }

        decimal amount = Math.Round(txn.TotalAmount, 2, MidpointRounding.AwayFromZero);
        if (request.Amount.HasValue &&
            Math.Round(request.Amount.Value, 2, MidpointRounding.AwayFromZero) != amount)
        {
            throw new InvalidOperationException(
                "The submitted counter amount does not match the application's payable fee snapshot.");
        }

        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);
        var offlineConfig = await _gatewayConfigRepository.GetQueryable()
            .FirstOrDefaultAsync(g => g.GatewayCode == "OFFLINE" || g.GatewayCode == "COUNTER", ct);

        if (offlineConfig == null)
        {
            offlineConfig = await _gatewayConfigRepository.GetQueryable().FirstOrDefaultAsync(ct);
        }

        int? paymentModeId = await ResolvePaymentModeIdAsync(request.PaymentMode ?? "Cash", ct);

        bool isInstrument = (request.PaymentMode?.IndexOf("CHEQUE", StringComparison.OrdinalIgnoreCase) >= 0)
                         || (request.PaymentMode?.IndexOf("धनादेश", StringComparison.OrdinalIgnoreCase) >= 0)
                         || (request.PaymentMode?.IndexOf("DD", StringComparison.OrdinalIgnoreCase) >= 0)
                         || (request.PaymentMode?.IndexOf("DEMAND", StringComparison.OrdinalIgnoreCase) >= 0);

        var pendingClearanceStatus = isInstrument ? await GetStatusByCodeAsync("PENDING_CLEARANCE", ct) : null;
        var initialStatus = isInstrument ? pendingClearanceStatus! : successStatus;

        // Preserve the transaction snapshot created at submission. Recording an
        // offline payment changes its state; it must not create a second payment
        // record with a potentially different fee calculation.
        txn.GatewayConfigId = offlineConfig?.Id ?? txn.GatewayConfigId;
        txn.PaymentStatusId = initialStatus.Id;
        txn.PaymentModeId = paymentModeId;
        txn.GatewayOrderId = $"OFFLINE_{app.Id}_{DateTime.Now:yyyyMMddHHmmss}";
        txn.GatewayPaymentId = !string.IsNullOrWhiteSpace(request.InstrumentNo)
            ? request.InstrumentNo
            : $"OFFLINE_{DateTime.Now:yyyyMMddHHmmss}";
        txn.BankRefNo = !string.IsNullOrWhiteSpace(request.BankName)
            ? $"{request.BankName} - {request.InstrumentNo}"
            : request.InstrumentNo;
        txn.BankId = request.BankId;
        txn.BankName = request.BankName;
        txn.BranchName = request.BranchName;
        txn.InstrumentNo = request.InstrumentNo;
        txn.InstrumentDate = request.InstrumentDate;
        txn.ClearanceDate = isInstrument ? null : DateTime.Now;
        txn.ApprovalStatus = isInstrument ? "PENDING" : "APPROVED";
        txn.ApprovedBy = isInstrument ? null : officerUserId;
        txn.ApprovedDate = isInstrument ? null : DateTime.Now;
        txn.PayerVpaOrAccount = request.PaymentMode ?? "Cash";
        txn.ReceiptDate = isInstrument ? null : DateTime.Now;
        txn.PaymentDate = isInstrument ? null : DateTime.Now;
        txn.Remarks = isInstrument
            ? $"Offline Cheque/DD accepted at counter by Officer ID {officerUserId}. Pending bank clearance. Mode: {request.PaymentMode}. {request.Remarks}"
            : $"Offline municipal counter fee collected by Officer ID {officerUserId}. Mode: {request.PaymentMode}. {request.Remarks}";
        txn.UpdatedBy = officerUserId;
        txn.UpdatedDate = DateTime.Now;
        await _unitOfWork.SaveChangesAsync(ct);

        // Update receipt number using generated transaction ID
        string receiptNo = isInstrument
            ? $"PROV/RTS/{DateTime.Now:yyyyMMdd}/{txn.Id:D6}"
            : $"REC/RTS/{DateTime.Now:yyyyMMdd}/{txn.Id:D6}";
        txn.ReceiptNo = receiptNo;

        if (isInstrument)
        {
            if (string.Equals(app.ApplicationStatus, "Payment Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(app.ApplicationStatus, "Pending Payment", StringComparison.OrdinalIgnoreCase))
            {
                app.ApplicationStatus = "Pending Clearance";
                app.UpdatedDate = DateTime.Now;
                app.UpdatedBy = officerUserId;
            }

            var history = new TrackApplicationHistoryEntity
            {
                ApplicationId = app.Id,
                ApprovalFlowId = app.ApprovalFlowId,
                ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                ActionByUserId = officerUserId,
                Action = "Instrument Received (Pending Clearance)",
                Status = "Pending Clearance",
                Remark = $"Cheque/DD No. {request.InstrumentNo} ({request.BankName}) received at counter for ₹{amount:F2}. Provisional No: {receiptNo}. Clearance pending.",
                CreatedDate = DateTime.Now,
                IsActive = true,
                CreatedBy = officerUserId
            };
            await _historyRepository.AddAsync(history, ct);
        }
        else
        {
            if (string.Equals(app.ApplicationStatus, "Payment Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(app.ApplicationStatus, "Pending Payment", StringComparison.OrdinalIgnoreCase))
            {
                app.ApplicationStatus = "Pending";
                app.UpdatedDate = DateTime.Now;
                app.UpdatedBy = officerUserId;
            }

            var history = new TrackApplicationHistoryEntity
            {
                ApplicationId = app.Id,
                ApprovalFlowId = app.ApprovalFlowId,
                ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                ActionByUserId = officerUserId,
                Action = "Offline Payment Received",
                Status = "Payment Done",
                Remark = $"Government fee of ₹{amount:F2} received at counter via {request.PaymentMode}. Receipt: {receiptNo}. {request.Remarks}",
                CreatedDate = DateTime.Now,
                IsActive = true,
                CreatedBy = officerUserId
            };
            await _historyRepository.AddAsync(history, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Return receipt DTO
        var receipt = await GetPaymentReceiptByApplicationIdAsync(app.Id, ct);
        return receipt!;
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
                    app.ApplicationStatus = "Pending";
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
            .Where(t => t.ApplicationId == applicationId && (t.PaymentStatusId == successStatus.Id || t.ReceiptNo != null))
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

        string? customerEmail = null;

        foreach (var fv in fieldValues)
        {
            var label = (fv.FieldDefinition?.FieldLabel ?? fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
            var code = (fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
            var val = fv.TextValue?.Trim();

            if (string.IsNullOrWhiteSpace(val)) continue;

            if (string.IsNullOrWhiteSpace(customerName) &&
                (label.Contains("Full Name") || label.Contains("name") || label.Contains("नाव") || code.Contains("name")))
            {
                customerName = val;
            }
            else if (string.IsNullOrWhiteSpace(customerMobile) &&
                (label.Contains("mobile") || label.Contains("phone") || label.Contains("contact") || label.Contains("मोबाईल") || code.Contains("mobile") || code.Contains("phone")))
            {
                customerMobile = val;
            }
            else if (string.IsNullOrWhiteSpace(customerEmail) &&
                (label.Contains("email") || label.Contains("mail") || label.Contains("ईमेल") || code.Contains("email")))
            {
                customerEmail = val;
            }
        }

        var ulbConfig = await _ulbConfigService.GetUlbConfigAsync(ct);

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            customerEmail = !string.IsNullOrWhiteSpace(customerMobile)
                ? $"{customerMobile}@citizen.portal"
                : $"citizen_{txn.ApplicationNo?.Replace("/", "_") ?? txn.ApplicationId.ToString()}@citizen.portal";
        }

        string? counterOfficerName = null;
        if (txn.CreatedBy.HasValue && txn.CreatedBy.Value > 0)
        {
            try
            {
                var officerUser = await _userRepository.GetByIdAsync(txn.CreatedBy.Value, ct);
                if (officerUser != null)
                {
                    counterOfficerName = !string.IsNullOrWhiteSpace(officerUser.FirstName)
                        ? $"{officerUser.FirstName} {officerUser.LastName}".Trim()
                        : officerUser.UserName;
                }
            }
            catch { }
        }

        bool isOffline = string.Equals(txn.GatewayConfig?.GatewayCode, "OFFLINE", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(txn.GatewayConfig?.GatewayCode, "COUNTER", StringComparison.OrdinalIgnoreCase) ||
                         (txn.TransactionNo != null && txn.TransactionNo.Contains("OFFLINE", StringComparison.OrdinalIgnoreCase)) ||
                         (txn.Remarks != null && txn.Remarks.Contains("Offline municipal counter", StringComparison.OrdinalIgnoreCase));

        string resolvedPaymentMode;
        if (isOffline)
        {
            if (txn.Remarks != null && txn.Remarks.Contains("Mode: Cheque", StringComparison.OrdinalIgnoreCase))
                resolvedPaymentMode = "Cheque (धनादेश)";
            else if (txn.Remarks != null && txn.Remarks.Contains("Mode: DD", StringComparison.OrdinalIgnoreCase))
                resolvedPaymentMode = "Demand Draft (डीडी)";
            else if (txn.Remarks != null && txn.Remarks.Contains("Mode: POS", StringComparison.OrdinalIgnoreCase))
                resolvedPaymentMode = "POS Card Swipe";
            else if (txn.Remarks != null && txn.Remarks.Contains("Mode: Challan", StringComparison.OrdinalIgnoreCase))
                resolvedPaymentMode = "Challan (चलन)";
            else if (txn.PaymentMode != null && !txn.PaymentMode.ModeCode.Equals("UPI", StringComparison.OrdinalIgnoreCase))
                resolvedPaymentMode = txn.PaymentMode.ModeNameEn;
            else if (!string.IsNullOrWhiteSpace(txn.PayerVpaOrAccount) && !txn.PayerVpaOrAccount.Contains("@"))
                resolvedPaymentMode = txn.PayerVpaOrAccount;
            else
                resolvedPaymentMode = "Cash (रोख)";
        }
        else
        {
            resolvedPaymentMode = txn.PaymentMode?.ModeNameEn ?? "Online Gateway";
            if (string.Equals(resolvedPaymentMode, "UPI", StringComparison.OrdinalIgnoreCase))
                resolvedPaymentMode = "UPI / QR Code (युपीआय)";
        }

        string channel = isOffline
            ? "CFC Municipal Counter (नागरी सुविधा केंद्र काऊंटर)"
            : "Online Citizen Portal (ऑनलाइन नागरिक पोर्टल)";

        string? bankName = null;
        string? instrumentNo = null;

        if (isOffline)
        {
            if (!string.IsNullOrWhiteSpace(txn.BankRefNo) && txn.BankRefNo.Contains("-"))
            {
                var parts = txn.BankRefNo.Split('-', 2);
                bankName = parts[0].Trim();
                instrumentNo = parts[1].Trim();
            }
            else
            {
                bankName = txn.BankRefNo;
                instrumentNo = !string.IsNullOrWhiteSpace(txn.GatewayPaymentId) && !txn.GatewayPaymentId.StartsWith("OFFLINE_")
                    ? txn.GatewayPaymentId
                    : null;
            }
        }
        else
        {
            bankName = txn.BankRefNo;
            instrumentNo = txn.GatewayPaymentId;
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
            BaseAmount = txn.BaseAmount > 0 ? txn.BaseAmount : txn.TotalAmount,
            LateFeeAmount = txn.LateFeeAmount,
            DiscountAmount = txn.DiscountAmount,
            AmountInWords = ConvertAmountToWordsEn(txn.TotalAmount),
            AmountInWordsLocal = ConvertAmountToWordsMr(txn.TotalAmount),
            Currency = txn.Currency,
            PaymentGateway = txn.GatewayConfig?.GatewayName ?? (isOffline ? "CFC Municipal Counter" : "Razorpay"),
            GatewayPaymentId = txn.GatewayPaymentId ?? "",
            TransactionNo = txn.TransactionNo,
            BankRefNo = txn.BankRefNo,
            BankName = bankName,
            InstrumentNo = instrumentNo,
            PayerVpaOrAccount = txn.PayerVpaOrAccount,
            ReceiptNo = txn.ReceiptNo ?? $"REC/RTS/{txn.Id:D6}",
            PaymentDate = txn.PaymentDate ?? txn.ReceiptDate,
            PaymentStatus = txn.PaymentStatus?.StatusNameEn ?? "Success",
            PaymentMode = resolvedPaymentMode,
            Channel = channel,
            CounterOfficerName = counterOfficerName,
            Remarks = txn.Remarks,
            CustomerName = customerName ?? "Applicant",
            CustomerMobile = customerMobile,
            CustomerEmail = customerEmail,
            UlbName = ulbConfig?.UlbName ?? "Municipal Corporation",
            UlbNameLocal = ulbConfig?.UlbNameLocal ?? "महानगरपालिका",
            UlbLogo = ulbConfig?.UlbLogo
        };
    }

    public async Task<PaymentReceiptDto?> GetPaymentReceiptByReceiptNoAsync(string receiptNo, CancellationToken ct = default)
    {
        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.Service)
            .Include(t => t.Department)
            .Include(t => t.Application)
            .Include(t => t.GatewayConfig)
            .Include(t => t.PaymentMode)
            .Include(t => t.PaymentStatus)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo, ct);

        if (txn == null) return null;

        return await GetPaymentReceiptByApplicationIdAsync(txn.ApplicationId, ct);
    }

    public async Task<PaymentStatusDto?> GetPaymentStatusAsync(int applicationId, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == applicationId && !a.MarkedForDeletion, ct);

        if (app == null) return null;

        var transaction = await GetLatestTransactionAsync(applicationId, ct);
        return MapPaymentStatus(app, transaction);
    }

    public async Task<IReadOnlyList<PaymentStatusDto>> GetPaymentStatusesByApplicationNosAsync(
        IEnumerable<string> applicationNos,
        CancellationToken ct = default)
    {
        var normalizedApplicationNos = applicationNos
            .Where(applicationNo => !string.IsNullOrWhiteSpace(applicationNo))
            .Select(applicationNo => applicationNo.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();

        if (normalizedApplicationNos.Count == 0)
        {
            return Array.Empty<PaymentStatusDto>();
        }

        var applications = await _applicationRepository.GetQueryable()
            .AsNoTracking()
            .Include(application => application.Service)
            .Where(application =>
                !application.MarkedForDeletion &&
                normalizedApplicationNos.Contains(application.ApplicationNo))
            .ToListAsync(ct);

        if (applications.Count == 0)
        {
            return Array.Empty<PaymentStatusDto>();
        }

        var applicationIds = applications.Select(application => application.Id).ToList();
        var latestTransactionIds = await _paymentRepository.GetQueryable()
            .AsNoTracking()
            .Where(transaction => applicationIds.Contains(transaction.ApplicationId))
            .GroupBy(transaction => transaction.ApplicationId)
            .Select(group => new
            {
                ApplicationId = group.Key,
                TransactionId = group.Max(transaction => transaction.Id)
            })
            .ToDictionaryAsync(item => item.ApplicationId, item => item.TransactionId, ct);

        var transactions = await _paymentRepository.GetQueryable()
            .AsNoTracking()
            .Include(transaction => transaction.PaymentStatus)
            .Include(transaction => transaction.GatewayConfig)
            .Where(transaction => latestTransactionIds.Values.Contains(transaction.Id))
            .ToListAsync(ct);
        var transactionsByApplicationId = transactions.ToDictionary(transaction => transaction.ApplicationId);

        return applications
            .Select(application =>
            {
                transactionsByApplicationId.TryGetValue(application.Id, out var transaction);
                return MapPaymentStatus(application, transaction);
            })
            .ToList();
    }

    public async Task<PagedResult<PaymentTransactionListItemDto>> GetTransactionsAsync(PaymentTransactionQueryDto query, CancellationToken ct = default)
    {
        var q = _paymentRepository.GetQueryable().AsNoTracking()
            .Include(t => t.Service)
            .Include(t => t.Department)
            .Include(t => t.PaymentMode)
            .Include(t => t.PaymentStatus)
            .Where(t => t.IsActive);

        if (query.DepartmentId.HasValue && query.DepartmentId.Value > 0)
            q = q.Where(t => t.DepartmentId == query.DepartmentId.Value);

        if (query.ServiceId.HasValue && query.ServiceId.Value > 0)
            q = q.Where(t => t.ServiceId == query.ServiceId.Value);

        if (query.PaymentStatusId.HasValue && query.PaymentStatusId.Value > 0)
            q = q.Where(t => t.PaymentStatusId == query.PaymentStatusId.Value);

        if (!string.IsNullOrWhiteSpace(query.StatusCode))
            q = q.Where(t => t.PaymentStatus != null && t.PaymentStatus.StatusCode.ToUpper() == query.StatusCode.Trim().ToUpper());

        if (query.PaymentModeId.HasValue && query.PaymentModeId.Value > 0)
            q = q.Where(t => t.PaymentModeId == query.PaymentModeId.Value);

        if (!string.IsNullOrWhiteSpace(query.PaymentModeCode))
            q = q.Where(t => t.PaymentMode != null && t.PaymentMode.ModeCode.ToUpper() == query.PaymentModeCode.Trim().ToUpper());

        if (query.FromDate.HasValue)
            q = q.Where(t => t.CreatedDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(t => t.CreatedDate <= query.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var s = query.SearchText.Trim();
            q = q.Where(t => t.ApplicationNo.Contains(s) || (t.ReceiptNo != null && t.ReceiptNo.Contains(s)) || (t.TransactionNo != null && t.TransactionNo.Contains(s)) || (t.GatewayPaymentId != null && t.GatewayPaymentId.Contains(s)));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(t => t.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new PaymentTransactionListItemDto
            {
                Id = t.Id,
                TransactionNo = t.TransactionNo,
                ApplicationId = t.ApplicationId,
                ApplicationNo = t.ApplicationNo,
                ServiceName = t.Service != null ? t.Service.ServiceName : "RTS Service",
                DepartmentName = t.Department != null ? t.Department.DepartmentName : "Department",
                TotalAmount = t.TotalAmount,
                Currency = t.Currency,
                PaymentStatus = t.PaymentStatus != null ? t.PaymentStatus.StatusNameEn : "Pending",
                StatusBadgeColor = t.PaymentStatus != null ? t.PaymentStatus.BadgeColor : "bg-amber-100 text-amber-800",
                PaymentMode = t.PaymentMode != null ? t.PaymentMode.ModeNameEn : "Online Gateway",
                GatewayPaymentId = t.GatewayPaymentId,
                ReceiptNo = t.ReceiptNo,
                PaymentDate = t.PaymentDate,
                CreatedDate = t.CreatedDate
            })
            .ToListAsync(ct);

        return new PagedResult<PaymentTransactionListItemDto>(items, totalCount, query.PageNumber, query.PageSize);
    }

    private static string ConvertAmountToWordsEn(decimal amount)
    {
        long whole = (long)Math.Floor(amount);
        if (whole == 0) return "Zero Rupees Only";
        string words = NumberToWordsEn(whole);
        return $"{words} Rupees Only";
    }

    private static string NumberToWordsEn(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + NumberToWordsEn(Math.Abs(number));

        string words = "";

        if ((number / 10000000) > 0)
        {
            words += NumberToWordsEn(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        if ((number / 100000) > 0)
        {
            words += NumberToWordsEn(number / 100000) + " Lakh ";
            number %= 100000;
        }

        if ((number / 1000) > 0)
        {
            words += NumberToWordsEn(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += NumberToWordsEn(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += " " + unitsMap[number % 10];
            }
        }

        return words.Trim();
    }

    private static string ConvertAmountToWordsMr(decimal amount)
    {
        long whole = (long)Math.Floor(amount);
        if (whole == 0) return "शून्य रुपये फक्त";
        if (whole == 30) return "तीस रुपये फक्त";
        if (whole == 50) return "पन्नास रुपये फक्त";
        if (whole == 100) return "एकशे रुपये फक्त";
        if (whole == 150) return "एकशे पन्नास रुपये फक्त";
        if (whole == 200) return "दोनशे रुपये फक्त";
        if (whole == 300) return "तीनशे रुपये फक्त";
        if (whole == 500) return "पाचशे रुपये फक्त";
        if (whole == 700) return "सातशे रुपये फक्त";
        if (whole == 1000) return "एक हजार रुपये फक्त";
        if (whole == 2000) return "दोन हजार रुपये फक्त";
        return $"{whole} रुपये फक्त";
    }

    public async Task<PagedResult<PendingClearanceItemDto>> GetPendingClearancePaymentsAsync(PendingClearanceQueryDto query, CancellationToken ct = default)
    {
        var q = _paymentRepository.GetQueryable().AsNoTracking()
            .Include(t => t.Service)
            .Include(t => t.Department)
            .Include(t => t.PaymentMode)
            .Include(t => t.PaymentStatus)
            .AsQueryable();

        // Payment Mode Filter
        if (!string.IsNullOrWhiteSpace(query.PaymentMode) && query.PaymentMode.ToUpper() != "ALL")
        {
            var pMode = query.PaymentMode.Trim().ToUpperInvariant();
            if (pMode == "CHEQUE" || pMode == "धनादेश")
            {
                q = q.Where(t => t.PaymentMode != null && (t.PaymentMode.ModeCode == "CHEQUE" || (t.PayerVpaOrAccount != null && t.PayerVpaOrAccount.Contains("Cheque")) || (t.Remarks != null && t.Remarks.Contains("Cheque"))));
            }
            else if (pMode == "DD" || pMode == "DEMAND_DRAFT")
            {
                q = q.Where(t => t.PaymentMode != null && (t.PaymentMode.ModeCode == "DD" || (t.PayerVpaOrAccount != null && t.PayerVpaOrAccount.Contains("DD")) || (t.Remarks != null && t.Remarks.Contains("DD"))));
            }
            else if (pMode == "ONLINE")
            {
                q = q.Where(t => t.PaymentMode == null || (t.PaymentMode.ModeCode != "CASH" && t.PaymentMode.ModeCode != "CHEQUE" && t.PaymentMode.ModeCode != "DD" && t.PaymentMode.ModeCode != "OFFLINE_CHALLAN"));
            }
            else if (pMode == "CASH" || pMode == "रोख")
            {
                q = q.Where(t => t.PaymentMode != null && t.PaymentMode.ModeCode == "CASH");
            }
            else
            {
                q = q.Where(t => t.PaymentMode != null && t.PaymentMode.ModeCode == pMode);
            }
        }

        // Status Filter
        if (!string.IsNullOrWhiteSpace(query.StatusCode) && query.StatusCode.ToUpper() != "ALL")
        {
            var code = query.StatusCode.Trim().ToUpperInvariant();
            q = q.Where(t => t.PaymentStatus != null && t.PaymentStatus.StatusCode == code);
        }

        // Date Range
        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value.Date;
            q = q.Where(t => t.CreatedDate >= from);
        }
        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            q = q.Where(t => t.CreatedDate <= to);
        }

        // Department Filter
        if (query.DepartmentId.HasValue && query.DepartmentId.Value > 0)
        {
            q = q.Where(t => t.DepartmentId == query.DepartmentId.Value);
        }

        // Service Filter
        if (query.ServiceId.HasValue && query.ServiceId.Value > 0)
        {
            q = q.Where(t => t.ServiceId == query.ServiceId.Value);
        }

        // Search Text
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var st = query.SearchText.Trim();
            q = q.Where(t =>
                t.ApplicationNo.Contains(st) ||
                t.TransactionNo.Contains(st) ||
                (t.InstrumentNo != null && t.InstrumentNo.Contains(st)) ||
                (t.BankName != null && t.BankName.Contains(st)) ||
                (t.ReceiptNo != null && t.ReceiptNo.Contains(st)) ||
                (t.BankRefNo != null && t.BankRefNo.Contains(st)));
        }

        var totalCount = await q.CountAsync(ct);
        var totalAmountSum = await q.SumAsync(t => (decimal?)t.TotalAmount, ct) ?? 0m;

        int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
        int pageSize = query.PageSize > 0 ? query.PageSize : 10;

        var items = await q.OrderByDescending(t => t.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Populate applicant details
        var appIds = items.Select(x => x.ApplicationId).Distinct().ToList();
        var fieldValues = await _fieldValueRepository.GetQueryable()
            .Include(f => f.FieldDefinition)
            .Where(f => appIds.Contains(f.ApplicationId) && !f.MarkedForDeletion)
            .ToListAsync(ct);

        var applicantNames = new Dictionary<int, string>();
        var applicantMobiles = new Dictionary<int, string>();

        foreach (var appId in appIds)
        {
            var fvs = fieldValues.Where(f => f.ApplicationId == appId).ToList();
            string? name = null;
            string? mobile = null;
            foreach (var fv in fvs)
            {
                var label = (fv.FieldDefinition?.FieldLabel ?? fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var code = (fv.FieldDefinition?.FieldCode ?? string.Empty).ToLowerInvariant();
                var val = fv.TextValue?.Trim();
                if (string.IsNullOrWhiteSpace(val)) continue;

                if (string.IsNullOrWhiteSpace(name) && (label.Contains("full name") || label.Contains("name") || label.Contains("नाव") || code.Contains("name")))
                    name = val;
                else if (string.IsNullOrWhiteSpace(mobile) && (label.Contains("mobile") || label.Contains("phone") || label.Contains("contact") || label.Contains("मोबाईल")))
                    mobile = val;
            }
            if (!string.IsNullOrWhiteSpace(name)) applicantNames[appId] = name;
            if (!string.IsNullOrWhiteSpace(mobile)) applicantMobiles[appId] = mobile;
        }

        var dtoList = items.Select(t => new PendingClearanceItemDto
        {
            TransactionId = t.Id,
            TransactionNo = t.TransactionNo,
            ApplicationId = t.ApplicationId,
            ApplicationNo = t.ApplicationNo,
            CitizenName = applicantNames.TryGetValue(t.ApplicationId, out var cName) ? cName : "Applicant",
            CitizenMobile = applicantMobiles.TryGetValue(t.ApplicationId, out var cMob) ? cMob : "",
            ServiceName = t.Service?.ServiceName ?? "RTS Service",
            ServiceNameLocal = t.Service?.ServiceNameLocal,
            DepartmentName = t.Department?.DepartmentName ?? "RTS Department",
            DepartmentNameLocal = t.Department?.DepartmentNameLocal,
            PaymentMode = t.PaymentMode?.ModeNameEn ?? (t.PayerVpaOrAccount ?? "Online"),
            TotalAmount = t.TotalAmount,
            Currency = t.Currency ?? "INR",
            BankId = t.BankId,
            BankName = t.BankName,
            BranchName = t.BranchName,
            InstrumentNo = t.InstrumentNo ?? t.BankRefNo,
            InstrumentDate = t.InstrumentDate,
            ClearanceDate = t.ClearanceDate,
            BankRefNo = t.BankRefNo,
            GatewayPaymentId = t.GatewayPaymentId,
            ReceiptNo = t.ReceiptNo,
            ReceiptDate = t.ReceiptDate,
            PaymentDate = t.PaymentDate,
            PaymentStatus = t.PaymentStatus?.StatusCode ?? "UNKNOWN",
            StatusBadgeColor = t.PaymentStatus?.BadgeColor ?? "bg-slate-50 text-slate-700",
            ApprovalStatus = t.ApprovalStatus ?? (t.PaymentStatus?.StatusCode == "SUCCESS" ? "APPROVED" : "PENDING"),
            IsReceiptCancelled = t.IsReceiptCancelled,
            CancellationReason = t.ReceiptCancellationReason,
            FailureReason = t.FailureReason,
            Remarks = t.Remarks,
            CreatedDate = t.CreatedDate
        }).ToList();

        var pagedResult = new PagedResult<PendingClearanceItemDto>(dtoList, totalCount, pageNumber, pageSize);
        pagedResult.TotalAmount = totalAmountSum;
        return pagedResult;
    }

    public async Task<PendingClearanceStatsDto> GetPendingClearanceStatsAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;

        var pendingDdChequeCount = await _paymentRepository.GetQueryable()
            .CountAsync(t => (t.PaymentStatus != null && t.PaymentStatus.StatusCode == "PENDING_CLEARANCE") ||
                (t.PaymentMode != null && (t.PaymentMode.ModeCode == "CHEQUE" || t.PaymentMode.ModeCode == "DD") && t.PaymentStatus != null && t.PaymentStatus.StatusCode == "PENDING"), ct);

        var pendingOnlineCount = await _paymentRepository.GetQueryable()
            .CountAsync(t => t.PaymentStatus != null && (t.PaymentStatus.StatusCode == "PENDING" || t.PaymentStatus.StatusCode == "INITIATED") &&
                (t.PaymentMode == null || (t.PaymentMode.ModeCode != "CHEQUE" && t.PaymentMode.ModeCode != "DD" && t.PaymentMode.ModeCode != "CASH")), ct);

        var clearedTodayQuery = _paymentRepository.GetQueryable()
            .Where(t => t.PaymentStatus != null && t.PaymentStatus.StatusCode == "SUCCESS" && !t.IsReceiptCancelled &&
                ((t.ClearanceDate.HasValue && t.ClearanceDate.Value >= today) || (t.PaymentDate.HasValue && t.PaymentDate.Value >= today)));

        var clearedTodayCount = await clearedTodayQuery.CountAsync(ct);
        var clearedTodayAmount = await clearedTodayQuery.SumAsync(t => (decimal?)t.TotalAmount, ct) ?? 0m;

        var bouncedCount = await _paymentRepository.GetQueryable()
            .CountAsync(t => (t.PaymentStatus != null && t.PaymentStatus.StatusCode == "BOUNCED") || t.ApprovalStatus == "DISAPPROVED", ct);

        var cancelledCount = await _paymentRepository.GetQueryable()
            .CountAsync(t => t.IsReceiptCancelled || (t.PaymentStatus != null && t.PaymentStatus.StatusCode == "CANCELLED"), ct);

        return new PendingClearanceStatsDto
        {
            TotalPendingDdCheque = pendingDdChequeCount,
            TotalPendingOnline = pendingOnlineCount,
            TotalClearedToday = clearedTodayCount,
            TotalClearedAmountToday = clearedTodayAmount,
            TotalBounced = bouncedCount,
            TotalCancelled = cancelledCount
        };
    }

    public async Task<VerifyPaymentResponseDto> ApproveTransactionAsync(ApprovePaymentRequestDto request, int officerUserId, CancellationToken ct = default)
    {
        if (request == null || request.TransactionId <= 0)
            throw new ArgumentException("Valid Transaction ID is required.");

        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.PaymentStatus)
            .Include(t => t.PaymentMode)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, ct);

        if (txn == null)
            throw new ArgumentException($"Transaction with ID {request.TransactionId} not found.");

        if (txn.IsReceiptCancelled)
            throw new InvalidOperationException($"Receipt for Transaction {txn.TransactionNo} has been cancelled and cannot be approved.");

        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);

        var clearanceDate = request.ClearanceDate ?? DateTime.Now;
        txn.PaymentStatusId = successStatus.Id;
        txn.ApprovalStatus = "APPROVED";
        txn.ApprovedBy = officerUserId;
        txn.ApprovedDate = DateTime.Now;
        txn.ClearanceDate = clearanceDate;
        txn.PaymentDate = clearanceDate;

        if (!string.IsNullOrWhiteSpace(request.BankRefNo))
            txn.BankRefNo = request.BankRefNo.Trim();

        if (string.IsNullOrWhiteSpace(txn.ReceiptNo) || txn.ReceiptNo.StartsWith("PROV/"))
        {
            txn.ReceiptNo = $"REC/RTS/{DateTime.Now:yyyyMMdd}/{txn.Id:D6}";
        }
        txn.ReceiptDate = DateTime.Now;

        txn.Remarks = !string.IsNullOrWhiteSpace(request.Remarks)
            ? $"{txn.Remarks} | Approved on {clearanceDate:dd/MM/yyyy}: {request.Remarks}"
            : $"{txn.Remarks} | Approved on {clearanceDate:dd/MM/yyyy}";

        txn.UpdatedBy = officerUserId;
        txn.UpdatedDate = DateTime.Now;

        // Advance Application Details
        var app = await _applicationRepository.GetByIdAsync(txn.ApplicationId, ct);
        if (app != null)
        {
            if (string.Equals(app.ApplicationStatus, "Payment Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(app.ApplicationStatus, "Pending Payment", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(app.ApplicationStatus, "Pending Clearance", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(app.ApplicationStatus, "Payment Verification Pending", StringComparison.OrdinalIgnoreCase))
            {
                app.ApplicationStatus = "Pending";
                app.UpdatedDate = DateTime.Now;
                app.UpdatedBy = officerUserId;
            }

            var history = new TrackApplicationHistoryEntity
            {
                ApplicationId = app.Id,
                ApprovalFlowId = app.ApprovalFlowId,
                ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                ActionByUserId = officerUserId,
                Action = "Payment Approved & Cleared",
                Status = "Payment Done",
                Remark = $"Payment of ₹{txn.TotalAmount:F2} cleared & approved by Officer ID {officerUserId}. Receipt: {txn.ReceiptNo}. Mode: {txn.PaymentMode?.ModeNameEn ?? "Instrument"}. Clearance Date: {clearanceDate:dd/MM/yyyy}",
                CreatedDate = DateTime.Now,
                IsActive = true,
                CreatedBy = officerUserId
            };
            await _historyRepository.AddAsync(history, ct);

            // Notify Aaple Sarkar
            if (!string.IsNullOrWhiteSpace(app.ApplicationNo))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _aapleSarkarService.UpdateStatusAsync(app.ApplicationNo, "PaymentDone", ct: CancellationToken.None);
                        await Task.Delay(1000);
                        await _aapleSarkarService.UpdateStatusAsync(app.ApplicationNo, "UnderScrutiny", ct: CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update Aaple Sarkar status after clearance approval for {AppNo}", app.ApplicationNo);
                    }
                });
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new VerifyPaymentResponseDto
        {
            Success = true,
            Message = $"Payment transaction approved and cleared successfully. Receipt: {txn.ReceiptNo}",
            ReceiptNo = txn.ReceiptNo,
            TransactionId = txn.Id.ToString(),
            ApplicationNo = txn.ApplicationNo,
            Amount = txn.TotalAmount,
            PaymentDate = txn.PaymentDate,
            PaymentStatus = "SUCCESS"
        };
    }

    public async Task<VerifyPaymentResponseDto> DisapproveTransactionAsync(DisapprovePaymentRequestDto request, int officerUserId, CancellationToken ct = default)
    {
        if (request == null || request.TransactionId <= 0)
            throw new ArgumentException("Valid Transaction ID is required.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Mandatory rejection / bounce reason is required.");

        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.PaymentStatus)
            .Include(t => t.PaymentMode)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, ct);

        if (txn == null)
            throw new ArgumentException($"Transaction with ID {request.TransactionId} not found.");

        var bouncedStatus = await GetStatusByCodeAsync("BOUNCED", ct);

        txn.PaymentStatusId = bouncedStatus.Id;
        txn.ApprovalStatus = "DISAPPROVED";
        txn.ApprovedBy = officerUserId;
        txn.ApprovedDate = DateTime.Now;
        txn.FailureReason = request.Reason.Trim();
        txn.ApprovalRemarks = request.Remarks?.Trim();
        txn.Remarks = $"{txn.Remarks} | Disapproved / Bounced: {request.Reason}";
        txn.UpdatedBy = officerUserId;
        txn.UpdatedDate = DateTime.Now;

        // Revert Application Details back to Payment Pending
        var app = await _applicationRepository.GetByIdAsync(txn.ApplicationId, ct);
        if (app != null)
        {
            app.ApplicationStatus = "Payment Pending";
            app.UpdatedDate = DateTime.Now;
            app.UpdatedBy = officerUserId;

            var history = new TrackApplicationHistoryEntity
            {
                ApplicationId = app.Id,
                ApprovalFlowId = app.ApprovalFlowId,
                ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                ActionByUserId = officerUserId,
                Action = "Payment Disapproved / Bounced",
                Status = "Payment Failed",
                Remark = $"Instrument {txn.InstrumentNo ?? txn.TransactionNo} was disapproved / bounced by Officer ID {officerUserId}. Reason: {request.Reason}. Applicant must re-pay.",
                CreatedDate = DateTime.Now,
                IsActive = true,
                CreatedBy = officerUserId
            };
            await _historyRepository.AddAsync(history, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new VerifyPaymentResponseDto
        {
            Success = true,
            Message = $"Payment transaction marked as Disapproved / Bounced. Reason: {request.Reason}",
            TransactionId = txn.Id.ToString(),
            ApplicationNo = txn.ApplicationNo,
            Amount = txn.TotalAmount,
            PaymentDate = txn.PaymentDate,
            PaymentStatus = "BOUNCED"
        };
    }

    public async Task<VerifyPaymentResponseDto> CancelReceiptAsync(CancelReceiptRequestDto request, int officerUserId, CancellationToken ct = default)
    {
        if (request == null || request.TransactionId <= 0)
            throw new ArgumentException("Valid Transaction ID is required.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Mandatory cancellation reason is required.");

        var txn = await _paymentRepository.GetQueryable()
            .Include(t => t.PaymentStatus)
            .Include(t => t.PaymentMode)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, ct);

        if (txn == null)
            throw new ArgumentException($"Transaction with ID {request.TransactionId} not found.");

        if (txn.IsReceiptCancelled)
            throw new InvalidOperationException($"Receipt {txn.ReceiptNo} is already cancelled.");

        var cancelledStatus = await GetStatusByCodeAsync("CANCELLED", ct);

        txn.PaymentStatusId = cancelledStatus.Id;
        txn.IsReceiptCancelled = true;
        txn.ReceiptCancelledDate = DateTime.Now;
        txn.ReceiptCancelledBy = officerUserId;
        txn.ReceiptCancellationReason = request.Reason.Trim();
        txn.Remarks = $"{txn.Remarks} | Receipt Cancelled by Officer ID {officerUserId}: {request.Reason}";
        txn.UpdatedBy = officerUserId;
        txn.UpdatedDate = DateTime.Now;

        // Revert Application Details back to Payment Pending
        var app = await _applicationRepository.GetByIdAsync(txn.ApplicationId, ct);
        if (app != null)
        {
            app.ApplicationStatus = "Payment Pending";
            app.UpdatedDate = DateTime.Now;
            app.UpdatedBy = officerUserId;

            var history = new TrackApplicationHistoryEntity
            {
                ApplicationId = app.Id,
                ApprovalFlowId = app.ApprovalFlowId,
                ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                ActionByUserId = officerUserId,
                Action = "Receipt Cancelled",
                Status = "Payment Pending",
                Remark = $"Official receipt {txn.ReceiptNo} cancelled by Officer ID {officerUserId}. Reason: {request.Reason}. {request.Remarks}",
                CreatedDate = DateTime.Now,
                IsActive = true,
                CreatedBy = officerUserId
            };
            await _historyRepository.AddAsync(history, ct);
        }

        // Insert immutable Audit Record in RTS.ReceiptCancellationLog
        var cancelLog = new RTSReceiptCancellationLogEntity
        {
            TransactionId = txn.Id,
            ApplicationId = txn.ApplicationId,
            ReceiptNumber = txn.ReceiptNo ?? $"TXN-{txn.Id}",
            OriginalAmount = txn.TotalAmount,
            PaymentMode = txn.PaymentMode?.ModeCode ?? (txn.PayerVpaOrAccount ?? "OFFLINE"),
            InstrumentNo = txn.InstrumentNo,
            BankId = txn.BankId,
            BankName = txn.BankName,
            CancelledBy = officerUserId,
            CancelledByName = $"Officer #{officerUserId}",
            CancelledDate = DateTime.Now,
            CancellationReason = request.Reason.Trim(),
            ApprovalRemarks = request.Remarks,
            CreatedDate = DateTime.Now
        };
        await _cancellationLogRepository.AddAsync(cancelLog, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return new VerifyPaymentResponseDto
        {
            Success = true,
            Message = $"Official receipt {txn.ReceiptNo} successfully cancelled.",
            ReceiptNo = txn.ReceiptNo,
            TransactionId = txn.Id.ToString(),
            ApplicationNo = txn.ApplicationNo,
            Amount = txn.TotalAmount,
            PaymentStatus = "CANCELLED"
        };
    }

    public async Task<BulkClearanceResponseDto> BulkClearanceUploadAsync(Stream stream, int officerUserId, CancellationToken ct = default)
    {
        var response = new BulkClearanceResponseDto();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            throw new ArgumentException("Uploaded Excel file has no worksheets.");
        }

        var rows = worksheet.RangeUsed()?.RowsUsed()?.ToList();
        if (rows == null || rows.Count <= 1)
        {
            throw new ArgumentException("Excel file contains no data rows (only header or empty).");
        }

        // Header Row = row 1
        var headerRow = rows[0];
        int colAppNo = -1, colTxnNo = -1, colInstrumentNo = -1, colBankName = -1, colClearanceDate = -1, colAction = -1, colRemarks = -1;

        for (int c = 1; c <= headerRow.CellCount(); c++)
        {
            var h = headerRow.Cell(c).GetString().Trim().ToLowerInvariant();
            if (h.Contains("app") || h.Contains("अर्ज")) colAppNo = c;
            else if (h.Contains("txn") || h.Contains("trans") || h.Contains("व्यवहार")) colTxnNo = c;
            else if (h.Contains("instrument") || h.Contains("cheque") || h.Contains("धनादेश") || h.Contains("dd")) colInstrumentNo = c;
            else if (h.Contains("bank") || h.Contains("बँक")) colBankName = c;
            else if (h.Contains("date") || h.Contains("तारीख")) colClearanceDate = c;
            else if (h.Contains("action") || h.Contains("status") || h.Contains("कृती")) colAction = c;
            else if (h.Contains("remark") || h.Contains("reason") || h.Contains("शेरा")) colRemarks = c;
        }

        // Fallbacks if headers didn't match keywords
        if (colAppNo == -1) colAppNo = 1;
        if (colInstrumentNo == -1) colInstrumentNo = 2;
        if (colBankName == -1) colBankName = 3;
        if (colClearanceDate == -1) colClearanceDate = 4;
        if (colAction == -1) colAction = 5;
        if (colRemarks == -1) colRemarks = 6;

        response.TotalRows = rows.Count - 1;

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            int rowIdx = i + 1;
            string appNo = colAppNo > 0 ? row.Cell(colAppNo).GetString().Trim() : "";
            string txnNo = colTxnNo > 0 ? row.Cell(colTxnNo).GetString().Trim() : "";
            string instrumentNo = colInstrumentNo > 0 ? row.Cell(colInstrumentNo).GetString().Trim() : "";
            string bankName = colBankName > 0 ? row.Cell(colBankName).GetString().Trim() : "";
            string actionStr = colAction > 0 ? row.Cell(colAction).GetString().Trim().ToUpperInvariant() : "APPROVE";
            string remarks = colRemarks > 0 ? row.Cell(colRemarks).GetString().Trim() : "";

            DateTime? clearanceDate = null;
            if (colClearanceDate > 0)
            {
                var cell = row.Cell(colClearanceDate);
                if (cell.DataType == XLDataType.DateTime)
                    clearanceDate = cell.GetDateTime();
                else if (DateTime.TryParse(cell.GetString(), out var dt))
                    clearanceDate = dt;
            }
            if (!clearanceDate.HasValue) clearanceDate = DateTime.Now;

            if (string.IsNullOrWhiteSpace(appNo) && string.IsNullOrWhiteSpace(txnNo) && string.IsNullOrWhiteSpace(instrumentNo))
            {
                continue; // empty row
            }

            response.ProcessedCount++;

            try
            {
                // Match transaction
                var txnQuery = _paymentRepository.GetQueryable()
                    .Include(t => t.PaymentStatus)
                    .Include(t => t.PaymentMode)
                    .AsQueryable();

                RTSPaymentTransactionEntity? matchedTxn = null;

                if (!string.IsNullOrWhiteSpace(appNo))
                {
                    matchedTxn = await txnQuery.FirstOrDefaultAsync(t => t.ApplicationNo == appNo && t.PaymentStatus != null && t.PaymentStatus.StatusCode != "SUCCESS", ct);
                    if (matchedTxn == null)
                        matchedTxn = await txnQuery.OrderByDescending(t => t.Id).FirstOrDefaultAsync(t => t.ApplicationNo == appNo, ct);
                }

                if (matchedTxn == null && !string.IsNullOrWhiteSpace(txnNo))
                {
                    matchedTxn = await txnQuery.FirstOrDefaultAsync(t => t.TransactionNo == txnNo, ct);
                }

                if (matchedTxn == null && !string.IsNullOrWhiteSpace(instrumentNo))
                {
                    matchedTxn = await txnQuery.FirstOrDefaultAsync(t => (t.InstrumentNo == instrumentNo || (t.BankRefNo != null && t.BankRefNo.Contains(instrumentNo))) && t.PaymentStatus != null && t.PaymentStatus.StatusCode != "SUCCESS", ct);
                }

                if (matchedTxn == null)
                {
                    response.FailureCount++;
                    response.Results.Add(new BulkClearanceRowResultDto
                    {
                        RowIndex = rowIdx,
                        ApplicationNo = appNo,
                        InstrumentNo = instrumentNo,
                        Action = actionStr,
                        Success = false,
                        Message = $"No matching payment transaction found for App: '{appNo}', Instrument: '{instrumentNo}'."
                    });
                    continue;
                }

                if (actionStr.Contains("DISAPPROVE") || actionStr.Contains("REJECT") || actionStr.Contains("BOUNCE"))
                {
                    await DisapproveTransactionAsync(new DisapprovePaymentRequestDto
                    {
                        TransactionId = matchedTxn.Id,
                        Reason = !string.IsNullOrWhiteSpace(remarks) ? remarks : "Bounced in bulk bank clearance scroll",
                        Remarks = remarks
                    }, officerUserId, ct);

                    response.SuccessCount++;
                    response.Results.Add(new BulkClearanceRowResultDto
                    {
                        RowIndex = rowIdx,
                        ApplicationNo = matchedTxn.ApplicationNo,
                        InstrumentNo = matchedTxn.InstrumentNo ?? instrumentNo,
                        Action = "DISAPPROVED",
                        Success = true,
                        Message = "Transaction successfully disapproved / marked bounced."
                    });
                }
                else
                {
                    // Approve
                    await ApproveTransactionAsync(new ApprovePaymentRequestDto
                    {
                        TransactionId = matchedTxn.Id,
                        ClearanceDate = clearanceDate,
                        BankRefNo = !string.IsNullOrWhiteSpace(instrumentNo) ? instrumentNo : matchedTxn.BankRefNo,
                        Remarks = !string.IsNullOrWhiteSpace(remarks) ? remarks : "Bulk bank clearance approved"
                    }, officerUserId, ct);

                    response.SuccessCount++;
                    response.Results.Add(new BulkClearanceRowResultDto
                    {
                        RowIndex = rowIdx,
                        ApplicationNo = matchedTxn.ApplicationNo,
                        InstrumentNo = matchedTxn.InstrumentNo ?? instrumentNo,
                        Action = "APPROVED",
                        Success = true,
                        Message = $"Approved successfully. Receipt: {matchedTxn.ReceiptNo}"
                    });
                }
            }
            catch (Exception ex)
            {
                response.FailureCount++;
                response.Results.Add(new BulkClearanceRowResultDto
                {
                    RowIndex = rowIdx,
                    ApplicationNo = appNo,
                    InstrumentNo = instrumentNo,
                    Action = actionStr,
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        return response;
    }

    public Task<byte[]> DownloadClearanceTemplateAsync(CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Clearance_Template");

        // Headers
        ws.Cell(1, 1).Value = "ApplicationNo";
        ws.Cell(1, 2).Value = "TransactionNo";
        ws.Cell(1, 3).Value = "InstrumentNo";
        ws.Cell(1, 4).Value = "BankName";
        ws.Cell(1, 5).Value = "ClearanceDate";
        ws.Cell(1, 6).Value = "Action";
        ws.Cell(1, 7).Value = "Remarks";

        // Styling Header
        var headerRange = ws.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(30, 58, 138); // Dark Navy Blue
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Sample Data
        ws.Cell(2, 1).Value = "RTS/2026/00001";
        ws.Cell(2, 2).Value = "TXN/OFFLINE/20261011/A1B2C3D4";
        ws.Cell(2, 3).Value = "458921";
        ws.Cell(2, 4).Value = "State Bank of India";
        ws.Cell(2, 5).Value = DateTime.Today.ToString("yyyy-MM-dd");
        ws.Cell(2, 6).Value = "APPROVE";
        ws.Cell(2, 7).Value = "Bank clearance scroll matched";

        ws.Cell(3, 1).Value = "RTS/2026/00002";
        ws.Cell(3, 2).Value = "";
        ws.Cell(3, 3).Value = "982143";
        ws.Cell(3, 4).Value = "Bank of Maharashtra";
        ws.Cell(3, 5).Value = DateTime.Today.ToString("yyyy-MM-dd");
        ws.Cell(3, 6).Value = "DISAPPROVE";
        ws.Cell(3, 7).Value = "Insufficient funds in drawer account";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public async Task<List<RTSBankLookupDto>> GetBanksAsync(CancellationToken ct = default)
    {
        return await _bankRepository.GetQueryable()
            .Where(b => b.IsActive)
            .OrderBy(b => b.BankName)
            .Select(b => new RTSBankLookupDto
            {
                Id = b.Id,
                BankCode = b.BankCode,
                BankName = b.BankName,
                BranchName = b.BranchName,
                IFSCCode = b.IFSCCode
            })
            .ToListAsync(ct);
    }

    public async Task<List<ReceiptCancellationLogDto>> GetCancellationLogsAsync(int? applicationId = null, long? transactionId = null, CancellationToken ct = default)
    {
        var query = _cancellationLogRepository.GetQueryable();
        if (applicationId.HasValue && applicationId.Value > 0)
        {
            query = query.Where(c => c.ApplicationId == applicationId.Value);
        }
        if (transactionId.HasValue && transactionId.Value > 0)
        {
            query = query.Where(c => c.TransactionId == transactionId.Value);
        }

        return await query
            .OrderByDescending(c => c.CancelledDate)
            .Select(c => new ReceiptCancellationLogDto
            {
                Id = c.Id,
                TransactionId = c.TransactionId,
                ApplicationId = c.ApplicationId,
                ReceiptNumber = c.ReceiptNumber,
                OriginalAmount = c.OriginalAmount,
                PaymentMode = c.PaymentMode,
                InstrumentNo = c.InstrumentNo,
                BankId = c.BankId,
                BankName = c.BankName,
                CancelledBy = c.CancelledBy,
                CancelledByName = c.CancelledByName,
                CancelledDate = c.CancelledDate,
                CancellationReason = c.CancellationReason,
                ApprovedBy = c.ApprovedBy,
                ApprovedByName = c.ApprovedByName,
                ApprovedDate = c.ApprovedDate,
                ApprovalRemarks = c.ApprovalRemarks,
                CreatedDate = c.CreatedDate
            })
            .ToListAsync(ct);
    }
}
