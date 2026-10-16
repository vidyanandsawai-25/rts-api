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
    private readonly IRepository<RTSPaymentDemandEntity, long> _demandRepository;
    private readonly IRepository<RTSPaymentGatewayOrderEntity, long> _gatewayOrderRepository;
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
        IRepository<RTSPaymentDemandEntity, long> demandRepository,
        IRepository<RTSPaymentGatewayOrderEntity, long> gatewayOrderRepository,
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
        _demandRepository = demandRepository;
        _gatewayOrderRepository = gatewayOrderRepository;
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
            .Where(t => t.ApplicationId == applicationId && t.IsActive)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaymentDemandDto> CreateOrUpdatePaymentDemandAsync(CreatePaymentDemandRequestDto request, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.MarkedForDeletion, ct)
            ?? throw new ArgumentException($"Application with ID {request.ApplicationId} does not exist.");

        var existingDemand = await _demandRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);

        decimal payableAmount;
        string? ruleDetailsJson = null;

        if (request.AmountOverride.HasValue)
        {
            payableAmount = Math.Round(request.AmountOverride.Value, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            var fieldValues = await _fieldValueRepository.GetQueryable()
                .Include(f => f.FieldDefinition)
                .Where(f => f.ApplicationId == app.Id && !f.MarkedForDeletion)
                .ToListAsync(ct);

            var feeEvaluation = await _ruleMasterService.EvaluateFeeAsync(
                new RTSFeeEvaluationRequestDto
                {
                    ServiceId = app.ServiceId,
                    FieldValues = fieldValues.Select(fv => new RTSFeeFieldValueDto
                    {
                        FieldDefinitionId = fv.FieldDefinitionId,
                        FieldCode = fv.FieldDefinition?.FieldCode,
                        Value = fv.TextValue
                            ?? fv.NumberValue?.ToString(CultureInfo.InvariantCulture)
                            ?? fv.DateValue?.ToString("O", CultureInfo.InvariantCulture)
                            ?? fv.BooleanValue?.ToString()
                    }).ToList()
                },
                ct);

            payableAmount = Math.Round(feeEvaluation.PayableAmount, 2, MidpointRounding.AwayFromZero);
            ruleDetailsJson = JsonSerializer.Serialize(feeEvaluation);
        }

        if (existingDemand != null)
        {
            if (existingDemand.DemandStatus != "Paid")
            {
                existingDemand.BaseAmount = payableAmount;
                existingDemand.TotalAmount = payableAmount;
                if (!string.IsNullOrWhiteSpace(ruleDetailsJson))
                    existingDemand.RuleDetailsJson = ruleDetailsJson;
                if (!string.IsNullOrWhiteSpace(request.Remarks))
                    existingDemand.Remarks = request.Remarks;
                existingDemand.UpdatedDate = DateTime.Now;
                await _demandRepository.UpdateAsync(existingDemand, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return MapDemandDto(existingDemand);
        }

        var demandNo = $"DEM/RTS/{DateTime.Now:yyyyMMdd}/{app.Id}";
        var newDemand = new RTSPaymentDemandEntity
        {
            DemandNo = demandNo,
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
            ServiceId = app.ServiceId,
            DepartmentId = app.DepartmentId,
            BaseAmount = payableAmount,
            LateFeeAmount = 0,
            DiscountAmount = 0,
            TotalAmount = payableAmount,
            Currency = "INR",
            DemandStatus = payableAmount > 0 ? "Pending" : "Exempted",
            RuleDetailsJson = ruleDetailsJson,
            Remarks = request.Remarks ?? "Demand created at application submission",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        await _demandRepository.AddAsync(newDemand, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapDemandDto(newDemand);
    }

    public async Task<PaymentDemandDto?> GetPaymentDemandAsync(int applicationId, CancellationToken ct = default)
    {
        var demand = await _demandRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && d.IsActive, ct);

        return demand != null ? MapDemandDto(demand) : null;
    }

    private static PaymentDemandDto MapDemandDto(RTSPaymentDemandEntity d)
    {
        return new PaymentDemandDto
        {
            Id = d.Id,
            DemandNo = d.DemandNo,
            ApplicationId = d.ApplicationId,
            ApplicationNo = d.ApplicationNo,
            ServiceId = d.ServiceId,
            DepartmentId = d.DepartmentId,
            BaseAmount = d.BaseAmount,
            LateFeeAmount = d.LateFeeAmount,
            DiscountAmount = d.DiscountAmount,
            TotalAmount = d.TotalAmount,
            Currency = d.Currency,
            DemandStatus = d.DemandStatus,
            RuleDetailsJson = d.RuleDetailsJson,
            Remarks = d.Remarks,
            CreatedDate = d.CreatedDate
        };
    }

    private static PaymentStatusDto MapPaymentStatus(
        RTSApplicationDetailsEntity app,
        RTSPaymentDemandEntity? demand,
        RTSPaymentTransactionEntity? transaction)
    {
        var isPaid = string.Equals(demand?.DemandStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                     || (transaction != null && transaction.PaymentStatus?.StatusCode == "SUCCESS");

        var isPendingClearance = transaction != null && transaction.PaymentStatus?.StatusCode == "PENDING_CLEARANCE";

        string statusCode;
        string statusEn;
        string statusMr;
        string badge;

        if (isPaid)
        {
            statusCode = "SUCCESS";
            statusEn = "Paid";
            statusMr = "यशस्वी (Paid)";
            badge = "bg-emerald-50 text-emerald-700 ring-emerald-600/20";
        }
        else if (isPendingClearance)
        {
            statusCode = "PENDING_CLEARANCE";
            statusEn = "Pending Clearance";
            statusMr = "तपासणी प्रलंबित";
            badge = "bg-amber-50 text-amber-700 ring-amber-600/20";
        }
        else if (demand != null && demand.TotalAmount > 0)
        {
            statusCode = "PENDING";
            statusEn = "Payment Pending";
            statusMr = "शुल्क प्रलंबित";
            badge = "bg-blue-50 text-blue-700 ring-blue-600/20";
        }
        else
        {
            statusCode = "NOT_REQUIRED";
            statusEn = "Not Required / Free";
            statusMr = "शुल्क आवश्यक नाही";
            badge = "bg-slate-50 text-slate-700 ring-slate-600/20";
        }

        var fee = demand?.TotalAmount ?? transaction?.TotalAmount ?? 0m;

        return new PaymentStatusDto
        {
            TransactionId = transaction?.Id,
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
            ServiceName = app.Service?.ServiceName ?? "RTS Service",
            RequiredFee = fee,
            IsFeeRequired = fee > 0m,
            HasPaymentTransaction = transaction != null || (demand != null && demand.TotalAmount > 0) || fee > 0m,
            PaymentStatus = statusCode,
            StatusNameEn = statusEn,
            StatusNameMr = statusMr,
            BadgeColor = badge,
            ReceiptNo = transaction?.ReceiptNo,
            PaymentDate = transaction?.PaymentDate ?? transaction?.ReceiptDate,
            GatewayPaymentId = transaction?.GatewayPaymentId
        };
    }

    public async Task<PaymentStatusDto> InitializePendingPaymentAsync(int applicationId, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == applicationId && !a.MarkedForDeletion, ct)
            ?? throw new ArgumentException($"Application with ID {applicationId} does not exist.");

        var demand = await _demandRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);

        if (demand == null)
        {
            await CreateOrUpdatePaymentDemandAsync(new CreatePaymentDemandRequestDto { ApplicationId = app.Id }, ct);
            demand = await _demandRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);
        }

        var transaction = await GetLatestTransactionAsync(app.Id, ct);
        return MapPaymentStatus(app, demand, transaction);
    }

    public async Task<PaymentOrderResponseDto> CreatePaymentOrderAsync(CreatePaymentOrderRequestDto request, CancellationToken ct = default)
    {
        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.MarkedForDeletion, ct)
            ?? throw new ArgumentException($"Application with ID {request.ApplicationId} does not exist.");

        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);
        var existingSuccess = await _paymentRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.ApplicationId == app.Id && t.PaymentStatusId == successStatus.Id && t.IsActive && !t.IsReceiptCancelled, ct);

        if (existingSuccess != null)
        {
            throw new InvalidOperationException($"Fee for Application {app.ApplicationNo} has already been paid successfully under Receipt {existingSuccess.ReceiptNo}.");
        }

        var demand = await _demandRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);

        if (demand == null)
        {
            await CreateOrUpdatePaymentDemandAsync(new CreatePaymentDemandRequestDto { ApplicationId = app.Id }, ct);
            demand = await _demandRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);
        }

        if (demand == null || demand.TotalAmount <= 0)
        {
            throw new InvalidOperationException($"No payable statutory fee demand configured for service '{app.Service?.ServiceName}'.");
        }

        if (string.Equals(demand.DemandStatus, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Statutory fee demand {demand.DemandNo} has already been marked as Paid.");
        }

        decimal amount = demand.TotalAmount;
        long amountInPaise = decimal.ToInt64(amount * 100m);

        var fieldValues = await _fieldValueRepository.GetQueryable()
            .Include(fieldValue => fieldValue.FieldDefinition)
            .Where(fieldValue => fieldValue.ApplicationId == app.Id && !fieldValue.MarkedForDeletion)
            .ToListAsync(ct);

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

        var gatewayConfig = await GetActiveGatewayConfigAsync(request.PaymentGateway, ct);

        // Check for active order within last 15 minutes to avoid redundant Razorpay order calls
        var existingOrder = await _gatewayOrderRepository.GetQueryable()
            .Where(o => o.DemandId == demand.Id && o.OrderStatus == "Initiated" && o.CreatedDate >= DateTime.Now.AddMinutes(-15))
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync(ct);

        if (existingOrder != null && !string.IsNullOrWhiteSpace(existingOrder.GatewayOrderId))
        {
            return new PaymentOrderResponseDto
            {
                Success = true,
                Message = "Active payment session retrieved successfully.",
                TransactionId = (int)existingOrder.Id,
                ApplicationId = app.Id,
                ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
                ServiceName = app.Service?.ServiceName ?? "RTS Service",
                DepartmentName = app.Department?.DepartmentName ?? "RTS Department",
                Amount = amount,
                AmountInPaise = amountInPaise,
                Currency = existingOrder.Currency,
                Gateway = existingOrder.GatewayCode,
                GatewayOrderId = existingOrder.GatewayOrderId,
                KeyId = gatewayConfig.KeyId,
                Description = $"Government Service Fee for {app.Service?.ServiceName ?? "RTS Service"}",
                CustomerName = applicantName,
                CustomerEmail = applicantEmail,
                CustomerMobile = applicantMobile
            };
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
                    demandId = demand.Id.ToString(),
                    demandNo = demand.DemandNo,
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

        var newGatewayOrder = new RTSPaymentGatewayOrderEntity
        {
            DemandId = demand.Id,
            ApplicationId = app.Id,
            GatewayConfigId = gatewayConfig.Id,
            GatewayCode = gatewayConfig.GatewayCode,
            GatewayOrderId = gatewayOrderId,
            Amount = amount,
            Currency = gatewayConfig.Currency ?? "INR",
            OrderStatus = "Initiated",
            CustomerName = applicantName,
            CustomerEmail = applicantEmail,
            CustomerMobile = applicantMobile,
            CreatedDate = DateTime.Now
        };

        await _gatewayOrderRepository.AddAsync(newGatewayOrder, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PaymentOrderResponseDto
        {
            Success = true,
            Message = "Payment order initiated successfully.",
            TransactionId = (int)newGatewayOrder.Id,
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
        var gatewayOrder = await _gatewayOrderRepository.GetQueryable()
            .Include(o => o.GatewayConfig)
            .Include(o => o.Demand)
            .FirstOrDefaultAsync(o => o.GatewayOrderId == request.GatewayOrderId, ct);

        if (gatewayOrder == null)
        {
            gatewayOrder = await _gatewayOrderRepository.GetQueryable()
                .Include(o => o.GatewayConfig)
                .Include(o => o.Demand)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync(o => o.ApplicationId == request.ApplicationId, ct);
        }

        if (gatewayOrder == null)
        {
            throw new ArgumentException("No matching gateway order session found for this application.");
        }

        var gatewayConfig = gatewayOrder.GatewayConfig ?? await GetActiveGatewayConfigAsync(null, ct);
        string secret = gatewayConfig.SecretKey;

        // Verify cryptographic HMAC-SHA256 signature
        string payload = $"{request.GatewayOrderId}|{request.GatewayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string generatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        bool isSignatureValid = string.Equals(generatedSignature, request.GatewaySignature?.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!isSignatureValid)
        {
            gatewayOrder.OrderStatus = "Failed";
            gatewayOrder.FailureReason = "Digital HMAC-SHA256 signature verification mismatch";
            gatewayOrder.UpdatedDate = DateTime.Now;
            await _gatewayOrderRepository.UpdateAsync(gatewayOrder, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new VerifyPaymentResponseDto
            {
                Success = false,
                Message = "Payment verification failed: Digital signature mismatch.",
                ApplicationNo = gatewayOrder.Demand?.ApplicationNo ?? $"APP{gatewayOrder.ApplicationId}",
                Amount = gatewayOrder.Amount,
                PaymentStatus = "FAILED"
            };
        }

        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);
        int? resolvedPaymentModeId = await ResolvePaymentModeIdAsync(request.PaymentMode, ct);

        decimal? gatewayFee = null;
        decimal? gatewayTax = null;
        string? payerVpaOrAccount = null;
        string? bankRefNo = null;
        string? rzpResponseJson = null;

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
                    rzpResponseJson = await rzpRes.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(rzpResponseJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("fee", out var feeProp) && feeProp.TryGetInt64(out var feePaise))
                        gatewayFee = (decimal)feePaise / 100m;

                    if (root.TryGetProperty("tax", out var taxProp) && taxProp.TryGetInt64(out var taxPaise))
                        gatewayTax = (decimal)taxPaise / 100m;

                    if (root.TryGetProperty("method", out var methodProp))
                    {
                        var method = methodProp.GetString();
                        if (!resolvedPaymentModeId.HasValue || resolvedPaymentModeId == 0)
                        {
                            resolvedPaymentModeId = await ResolvePaymentModeIdAsync(method, ct);
                        }

                        if (string.Equals(method, "upi", StringComparison.OrdinalIgnoreCase) && root.TryGetProperty("vpa", out var vpaProp))
                            payerVpaOrAccount = vpaProp.GetString();
                        else if (string.Equals(method, "bank_transfer", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "netbanking", StringComparison.OrdinalIgnoreCase))
                        {
                            if (root.TryGetProperty("bank", out var bankProp))
                                payerVpaOrAccount = bankProp.GetString();
                        }
                        else if (root.TryGetProperty("card", out var cardProp) && cardProp.TryGetProperty("last4", out var last4Prop))
                        {
                            payerVpaOrAccount = $"Card **** {last4Prop.GetString()}";
                        }
                    }

                    if (root.TryGetProperty("acquirer_data", out var acqProp))
                    {
                        if (acqProp.TryGetProperty("rrn", out var rrnProp))
                            bankRefNo = rrnProp.GetString();
                        else if (acqProp.TryGetProperty("bank_transaction_id", out var btxProp))
                            bankRefNo = btxProp.GetString();
                        else if (acqProp.TryGetProperty("upi_transaction_id", out var upiTxProp))
                            bankRefNo = upiTxProp.GetString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch additional payment details from gateway API for payment {PaymentId}", request.GatewayPaymentId);
        }

        gatewayOrder.OrderStatus = "Paid";
        gatewayOrder.GatewayPaymentId = request.GatewayPaymentId;
        gatewayOrder.GatewaySignature = request.GatewaySignature;
        gatewayOrder.GatewayFee = gatewayFee;
        gatewayOrder.GatewayTax = gatewayTax;
        gatewayOrder.GatewayResponseJson = rzpResponseJson;
        gatewayOrder.UpdatedDate = DateTime.Now;
        await _gatewayOrderRepository.UpdateAsync(gatewayOrder, ct);

        var demand = await _demandRepository.GetByIdAsync(gatewayOrder.DemandId, ct)
            ?? await _demandRepository.GetQueryable().FirstOrDefaultAsync(d => d.ApplicationId == request.ApplicationId && d.IsActive, ct);

        if (demand != null)
        {
            demand.DemandStatus = "Paid";
            demand.UpdatedDate = DateTime.Now;
            await _demandRepository.UpdateAsync(demand, ct);
        }

        var app = await _applicationRepository.GetQueryable()
            .Include(a => a.Service)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.MarkedForDeletion, ct);

        string appNo = app?.ApplicationNo ?? demand?.ApplicationNo ?? $"APP{request.ApplicationId}";
        string transactionNo = $"TXN/RTS/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        string receiptNo = $"REC/RTS/{DateTime.Now:yyyyMMdd}/{request.ApplicationId:D6}";

        var confirmedTxn = new RTSPaymentTransactionEntity
        {
            TransactionNo = transactionNo,
            DemandId = demand?.Id,
            GatewayOrderRowId = gatewayOrder.Id,
            ApplicationId = request.ApplicationId,
            ApplicationNo = appNo,
            ServiceId = app?.ServiceId ?? demand?.ServiceId ?? 0,
            DepartmentId = app?.DepartmentId ?? demand?.DepartmentId ?? 0,
            GatewayConfigId = gatewayConfig.Id,
            PaymentStatusId = successStatus.Id,
            PaymentModeId = resolvedPaymentModeId,
            BaseAmount = demand?.BaseAmount ?? gatewayOrder.Amount,
            LateFeeAmount = demand?.LateFeeAmount ?? 0,
            DiscountAmount = demand?.DiscountAmount ?? 0,
            TotalAmount = demand?.TotalAmount ?? gatewayOrder.Amount,
            Currency = gatewayOrder.Currency,
            PayerName = gatewayOrder.CustomerName,
            PayerMobile = gatewayOrder.CustomerMobile,
            PayerVpaOrAccount = payerVpaOrAccount,
            GatewayOrderId = request.GatewayOrderId,
            GatewayPaymentId = request.GatewayPaymentId,
            GatewaySignature = request.GatewaySignature,
            GatewayFee = gatewayFee,
            GatewayTax = gatewayTax,
            BankRefNo = bankRefNo,
            ReceiptNo = receiptNo,
            ReceiptDate = DateTime.Now,
            PaymentDate = DateTime.Now,
            ApprovalStatus = "APPROVED",
            GatewayResponseJson = rzpResponseJson,
            Remarks = $"Verified online payment via {gatewayConfig.GatewayName}. Payment ID: {request.GatewayPaymentId}",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        await _paymentRepository.AddAsync(confirmedTxn, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Advance application workflow state
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
                Remark = $"Government Fee of ₹{confirmedTxn.TotalAmount:F2} received via {gatewayConfig.GatewayName}. Ref: {request.GatewayPaymentId}, Receipt: {receiptNo}",
                CreatedDate = DateTime.Now,
                IsActive = true
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
                        _logger.LogWarning(ex, "Failed to update Aaple Sarkar status after payment for {AppNo}", app.ApplicationNo);
                    }
                });
            }
        }

        // SMS dispatch
        string? smsMobile = gatewayOrder.CustomerMobile;
        string? smsName = gatewayOrder.CustomerName;

        await _unitOfWork.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(smsMobile))
        {
            try
            {
                await _smsNotificationService.SendPaymentSuccessAsync(
                    confirmedTxn.ApplicationId,
                    confirmedTxn.ApplicationNo ?? $"RTS{confirmedTxn.ApplicationId}",
                    smsName ?? "Citizen",
                    smsMobile,
                    confirmedTxn.TotalAmount,
                    receiptNo,
                    ct);
            }
            catch (Exception smsEx)
            {
                _logger.LogError(smsEx, "Failed to send payment receipt SMS for application {AppId}", confirmedTxn.ApplicationId);
            }
        }

        return new VerifyPaymentResponseDto
        {
            Success = true,
            Message = "Payment verified successfully. Official digital receipt generated.",
            ReceiptNo = receiptNo,
            TransactionId = confirmedTxn.Id.ToString(),
            ApplicationNo = confirmedTxn.ApplicationNo,
            Amount = confirmedTxn.TotalAmount,
            PaymentDate = confirmedTxn.PaymentDate,
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
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.IsActive && !a.MarkedForDeletion, ct)
            ?? throw new ArgumentException($"Application with ID {request.ApplicationId} not found.");

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

        // Fetch or create statutory demand
        var demand = await _demandRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);

        if (demand == null)
        {
            await CreateOrUpdatePaymentDemandAsync(new CreatePaymentDemandRequestDto { ApplicationId = app.Id }, ct);
            demand = await _demandRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);
        }

        if (demand == null || demand.TotalAmount <= 0)
        {
            decimal officialFee = app.Service?.Fees ?? 0;
            if (officialFee > 0)
            {
                await CreateOrUpdatePaymentDemandAsync(new CreatePaymentDemandRequestDto { ApplicationId = app.Id, AmountOverride = officialFee }, ct);
                demand = await _demandRepository.GetQueryable().FirstOrDefaultAsync(d => d.ApplicationId == app.Id && d.IsActive, ct);
            }
            else
            {
                throw new InvalidOperationException("This application does not have a payable statutory fee demand.");
            }
        }

        decimal amount = Math.Round(demand!.TotalAmount, 2, MidpointRounding.AwayFromZero);
        if (request.Amount.HasValue &&
            Math.Round(request.Amount.Value, 2, MidpointRounding.AwayFromZero) != amount)
        {
            throw new InvalidOperationException(
                $"The submitted counter amount (₹{request.Amount.Value:F2}) does not match the assessed statutory fee demand (₹{amount:F2}).");
        }

        int? paymentModeId = await ResolvePaymentModeIdAsync(request.PaymentMode ?? "Cash", ct);
        var modeEntity = paymentModeId.HasValue ? await _modeRepository.GetByIdAsync(paymentModeId.Value, ct) : null;
        bool isInstrument = modeEntity != null
            ? (modeEntity.ModeCode == "CHEQUE" || modeEntity.ModeCode == "DD")
            : (!string.IsNullOrWhiteSpace(request.PaymentMode) && (request.PaymentMode.Contains("CHEQUE", StringComparison.OrdinalIgnoreCase) || request.PaymentMode.Contains("DD", StringComparison.OrdinalIgnoreCase) || request.PaymentMode.Contains("धनादेश", StringComparison.OrdinalIgnoreCase)));

        var successStatus = await GetStatusByCodeAsync("SUCCESS", ct);
        var pendingClearanceStatus = isInstrument ? await GetStatusByCodeAsync("PENDING_CLEARANCE", ct) : null;
        var initialStatus = isInstrument ? pendingClearanceStatus! : successStatus;

        string transactionNo = $"TXN/RTS/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        string receiptNo = isInstrument
            ? $"PROV/RTS/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid().ToString("N")[..6].ToUpper()}"
            : $"REC/RTS/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        // Insert confirmed/active payment into RTS.PaymentTransaction
        var txn = new RTSPaymentTransactionEntity
        {
            TransactionNo = transactionNo,
            DemandId = demand.Id,
            ApplicationId = app.Id,
            ApplicationNo = app.ApplicationNo ?? $"APP{app.Id}",
            ServiceId = app.ServiceId,
            DepartmentId = app.DepartmentId,
            PaymentStatusId = initialStatus.Id,
            PaymentModeId = paymentModeId,
            BaseAmount = demand.BaseAmount,
            LateFeeAmount = demand.LateFeeAmount,
            DiscountAmount = demand.DiscountAmount,
            TotalAmount = demand.TotalAmount,
            Currency = demand.Currency ?? "INR",
            GatewayOrderId = $"OFFLINE_{app.Id}_{DateTime.Now:yyyyMMddHHmmss}",
            GatewayPaymentId = !string.IsNullOrWhiteSpace(request.InstrumentNo)
                ? request.InstrumentNo
                : $"OFFLINE_{DateTime.Now:yyyyMMddHHmmss}",
            BankRefNo = !string.IsNullOrWhiteSpace(request.BankName)
                ? $"{request.BankName} - {request.InstrumentNo}"
                : request.InstrumentNo,
            BankId = request.BankId,
            BankName = request.BankName,
            BranchName = request.BranchName,
            InstrumentNo = request.InstrumentNo,
            InstrumentDate = request.InstrumentDate,
            ClearanceDate = isInstrument ? null : DateTime.Now,
            ApprovalStatus = isInstrument ? "PENDING" : "APPROVED",
            ApprovedBy = isInstrument ? null : officerUserId,
            ApprovedDate = isInstrument ? null : DateTime.Now,
            PayerVpaOrAccount = request.PaymentMode ?? "Cash",
            ReceiptNo = receiptNo,
            ReceiptDate = DateTime.Now,
            PaymentDate = isInstrument ? null : DateTime.Now,
            Remarks = isInstrument
                ? $"Offline Cheque/DD accepted at counter by Officer ID {officerUserId}. Pending bank clearance. Mode: {request.PaymentMode}. {request.Remarks}"
                : $"Offline municipal counter fee collected by Officer ID {officerUserId}. Mode: {request.PaymentMode}. {request.Remarks}",
            CreatedBy = officerUserId,
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        await _paymentRepository.AddAsync(txn, ct);

        // Update Demand Status
        if (!isInstrument)
        {
            demand.DemandStatus = "Paid";
            demand.UpdatedDate = DateTime.Now;
            demand.UpdatedBy = officerUserId;
            await _demandRepository.UpdateAsync(demand, ct);
        }

        // Advance Application Status & Add History
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
            .Include(t => t.Demand)
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
            DemandNo = txn.Demand?.DemandNo,
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
            CustomerName = !string.IsNullOrWhiteSpace(txn.PayerName) ? txn.PayerName : (customerName ?? "Applicant"),
            CustomerMobile = !string.IsNullOrWhiteSpace(txn.PayerMobile) ? txn.PayerMobile : customerMobile,
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

        var demand = await _demandRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && d.IsActive, ct);

        var transaction = await GetLatestTransactionAsync(applicationId, ct);
        return MapPaymentStatus(app, demand, transaction);
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

        var demands = await _demandRepository.GetQueryable()
            .AsNoTracking()
            .Where(d => applicationIds.Contains(d.ApplicationId) && d.IsActive)
            .ToListAsync(ct);
        var demandsByAppId = demands.ToDictionary(d => d.ApplicationId);

        var latestTransactionIds = await _paymentRepository.GetQueryable()
            .AsNoTracking()
            .Where(transaction => applicationIds.Contains(transaction.ApplicationId) && transaction.IsActive)
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
                demandsByAppId.TryGetValue(application.Id, out var demand);
                transactionsByApplicationId.TryGetValue(application.Id, out var transaction);
                return MapPaymentStatus(application, demand, transaction);
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
        string words = NumberToWordsMr(whole);
        return $"{words} रुपये फक्त";
    }

    private static string NumberToWordsMr(long number)
    {
        if (number == 0) return "शून्य";
        if (number < 0) return "उणे " + NumberToWordsMr(Math.Abs(number));

        string words = "";

        if ((number / 10000000) > 0)
        {
            words += NumberToWordsMr(number / 10000000) + " कोटी ";
            number %= 10000000;
        }

        if ((number / 100000) > 0)
        {
            words += NumberToWordsMr(number / 100000) + " लाख ";
            number %= 100000;
        }

        if ((number / 1000) > 0)
        {
            words += NumberToWordsMr(number / 1000) + " हजार ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += NumberToWordsMr(number / 100) + " शे ";
            number %= 100;
        }

        if (number > 0)
        {
            var mrNumbers = new[]
            {
                "", "एक", "दोन", "तीन", "चार", "पाच", "सहा", "सात", "आठ", "नऊ", "दहा",
                "अकरा", "बारा", "तेरा", "चौदा", "पंधरा", "सोळा", "सतरा", "अठरा", "एकोणीस", "वीस",
                "एकवीस", "बावीस", "तेवीस", "चोवीस", "पंचवीस", "सव्वीस", "सत्तावीस", "अठ्ठावीस", "एकोणतीस", "तीस",
                "एकतीस", "बत्तीस", "तेहेतीस", "चौतीस", "पस्तीस", "छत्तीस", "सदतीस", "अडतीस", "एकेचाळीस", "चाळीस",
                "एक्केचाळीस", "बेचाळीस", "त्रेचाळीस", "चव्वेचाळीस", "पंचेचाळीस", "शेहेचाळीस", "सत्तेचाळीस", "अठ्ठेचाळीस", "एकोणपन्नास", "पन्नास",
                "एक्कावन्न", "बावन्न", "त्रेपन्न", "चोपन्न", "पंचावन्न", "छप्पन्न", "सत्तावन्न", "अठ्ठावन्न", "एकोणसाठ", "साठ",
                "एकसष्ठ", "बासष्ठ", "त्रेसष्ठ", "चौसष्ठ", "पासष्ठ", "सहासष्ठ", "सदुसष्ठ", "अडुसष्ठ", "एकोणसत्तर", "सत्तर",
                "एकाहत्तर", "बाहत्तर", "त्र्याहत्तर", "चौऱ्याहत्तर", "पंचाहत्तर", "शहात्तर", "सत्त्याहत्तर", "अठ्ठ्याहत्तर", "एकोणऐंशी", "ऐंशी",
                "ऐंशी", "ब्याऐंशी", "त्र्याऐंशी", "चौऱ्याऐंशी", "पंच्याऐंशी", "शहाऐंशी", "सत्त्याऐंशी", "अठ्ठ्याऐंशी", "एकोणनव्वद", "नव्वद",
                "एक्याण्णव", "ब्याण्णव", "त्र्याण्णव", "चौऱ्याण्णव", "पंच्याण्णव", "शहाण्णव", "सत्त्याण्णव", "अठ्ठ्याण्णव", "नव्व्याण्णव"
            };

            if (number < mrNumbers.Length)
            {
                words += mrNumbers[number];
            }
            else
            {
                words += number.ToString();
            }
        }

        return words.Trim();
    }

    public async Task<PagedResult<PendingClearanceItemDto>> GetPendingClearancePaymentsAsync(PendingClearanceQueryDto query, CancellationToken ct = default)
    {
        var q = _paymentRepository.GetQueryable().AsNoTracking()
            .Include(t => t.Demand)
            .Include(t => t.Application)
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
            if (code == "PENDING_CLEARANCE")
            {
                q = q.Where(t => t.PaymentStatus != null && t.PaymentStatus.StatusCode == code && (t.PaymentMode == null || t.PaymentMode.ModeCode != "CASH"));
            }
            else
            {
                q = q.Where(t => t.PaymentStatus != null && t.PaymentStatus.StatusCode == code);
            }
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
                (t.Application != null && t.Application.ApplicantName != null && t.Application.ApplicantName.Contains(st)) ||
                (t.Application != null && t.Application.ApplicantMobileNo != null && t.Application.ApplicantMobileNo.Contains(st)) ||
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
            DemandNo = t.Demand != null ? t.Demand.DemandNo : null,
            ApplicationId = t.ApplicationId,
            ApplicationNo = t.ApplicationNo,
            CitizenName = !string.IsNullOrWhiteSpace(t.PayerName)
                ? t.PayerName
                : (!string.IsNullOrWhiteSpace(t.Application?.ApplicantName)
                    ? t.Application.ApplicantName
                    : (applicantNames.TryGetValue(t.ApplicationId, out var cName) ? cName : "Applicant")),
            CitizenMobile = !string.IsNullOrWhiteSpace(t.PayerMobile)
                ? t.PayerMobile
                : (!string.IsNullOrWhiteSpace(t.Application?.ApplicantMobileNo)
                    ? t.Application.ApplicantMobileNo
                    : (applicantMobiles.TryGetValue(t.ApplicationId, out var cMob) ? cMob : "")),
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
            .CountAsync(t => ((t.PaymentStatus != null && t.PaymentStatus.StatusCode == "PENDING_CLEARANCE") ||
                (t.PaymentMode != null && (t.PaymentMode.ModeCode == "CHEQUE" || t.PaymentMode.ModeCode == "DD") && t.PaymentStatus != null && t.PaymentStatus.StatusCode == "PENDING")) &&
                (t.PaymentMode == null || t.PaymentMode.ModeCode != "CASH"), ct);

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
        {
            txn.BankRefNo = request.BankRefNo.Trim();
            if (string.IsNullOrWhiteSpace(txn.GatewayPaymentId))
            {
                txn.GatewayPaymentId = txn.BankRefNo;
            }
        }

        // Dynamically assign or resolve Payment Mode (no hardcoded IDs)
        if (request.PaymentModeId.HasValue && request.PaymentModeId.Value > 0)
        {
            txn.PaymentModeId = request.PaymentModeId.Value;
        }
        else if (!txn.PaymentModeId.HasValue || txn.PaymentModeId.Value <= 0)
        {
            var defaultMode = await _modeRepository.GetQueryable()
                .Where(m => m.IsActive)
                .OrderBy(m => m.ModeCode == "NETBANKING" ? 0 : (m.ModeCode == "ONLINE" ? 1 : 2))
                .FirstOrDefaultAsync(ct);
            if (defaultMode != null)
            {
                txn.PaymentModeId = defaultMode.Id;
            }
        }

        // Dynamically assign Bank if selected by officer
        if (request.BankId.HasValue && request.BankId.Value > 0)
        {
            txn.BankId = request.BankId.Value;
            var bank = await _bankRepository.GetByIdAsync(request.BankId.Value, ct);
            if (bank != null)
            {
                txn.BankName = bank.BankName;
            }
        }

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

        // Synchronize PaymentDemand
        if (txn.DemandId.HasValue)
        {
            var demand = await _demandRepository.GetByIdAsync(txn.DemandId.Value, ct);
            if (demand != null)
            {
                demand.DemandStatus = "Paid";
                demand.UpdatedDate = DateTime.Now;
                demand.UpdatedBy = officerUserId;
                await _demandRepository.UpdateAsync(demand, ct);
            }
        }
        else
        {
            var demand = await _demandRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.ApplicationId == txn.ApplicationId && d.IsActive, ct);
            if (demand != null)
            {
                demand.DemandStatus = "Paid";
                demand.UpdatedDate = DateTime.Now;
                demand.UpdatedBy = officerUserId;
                await _demandRepository.UpdateAsync(demand, ct);
            }
        }

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

            var dynamicModeName = txn.PaymentMode?.ModeNameEn;
            if (string.IsNullOrWhiteSpace(dynamicModeName) && txn.PaymentModeId.HasValue)
            {
                var modeEntity = await _modeRepository.GetByIdAsync(txn.PaymentModeId.Value, ct);
                dynamicModeName = modeEntity?.ModeNameEn;
            }

            var history = new TrackApplicationHistoryEntity
            {
                ApplicationId = app.Id,
                ApprovalFlowId = app.ApprovalFlowId,
                ApprovalFlowStageId = app.CurrentApprovalFlowStageId,
                ActionByUserId = officerUserId,
                Action = "Payment Approved & Cleared",
                Status = "Payment Done",
                Remark = $"Payment of ₹{txn.TotalAmount:F2} cleared & approved by Officer ID {officerUserId}. Receipt: {txn.ReceiptNo}. Mode: {dynamicModeName ?? "Instrument"}. Clearance Date: {clearanceDate:dd/MM/yyyy}",
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

        // Synchronize PaymentDemand
        if (txn.DemandId.HasValue)
        {
            var demand = await _demandRepository.GetByIdAsync(txn.DemandId.Value, ct);
            if (demand != null)
            {
                demand.DemandStatus = "Pending";
                demand.Remarks = $"Instrument disapproved / bounced: {request.Reason}";
                demand.UpdatedDate = DateTime.Now;
                demand.UpdatedBy = officerUserId;
                await _demandRepository.UpdateAsync(demand, ct);
            }
        }
        else
        {
            var demand = await _demandRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.ApplicationId == txn.ApplicationId && d.IsActive, ct);
            if (demand != null)
            {
                demand.DemandStatus = "Pending";
                demand.Remarks = $"Instrument disapproved / bounced: {request.Reason}";
                demand.UpdatedDate = DateTime.Now;
                demand.UpdatedBy = officerUserId;
                await _demandRepository.UpdateAsync(demand, ct);
            }
        }

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

        // Synchronize PaymentDemand
        if (txn.DemandId.HasValue)
        {
            var demand = await _demandRepository.GetByIdAsync(txn.DemandId.Value, ct);
            if (demand != null)
            {
                demand.DemandStatus = "Pending";
                demand.Remarks = $"Receipt {txn.ReceiptNo} cancelled: {request.Reason}";
                demand.UpdatedDate = DateTime.Now;
                demand.UpdatedBy = officerUserId;
                await _demandRepository.UpdateAsync(demand, ct);
            }
        }
        else
        {
            var demand = await _demandRepository.GetQueryable()
                .FirstOrDefaultAsync(d => d.ApplicationId == txn.ApplicationId && d.IsActive, ct);
            if (demand != null)
            {
                demand.DemandStatus = "Pending";
                demand.Remarks = $"Receipt {txn.ReceiptNo} cancelled: {request.Reason}";
                demand.UpdatedDate = DateTime.Now;
                demand.UpdatedBy = officerUserId;
                await _demandRepository.UpdateAsync(demand, ct);
            }
        }

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
            DemandId = txn.DemandId,
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
                        Remarks = !string.IsNullOrWhiteSpace(remarks) ? remarks : "Bulk bank clearance approved",
                        PaymentModeId = matchedTxn.PaymentModeId,
                        BankId = matchedTxn.BankId
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

    public async Task<List<RTSPaymentModeLookupDto>> GetPaymentModesAsync(CancellationToken ct = default)
    {
        return await _modeRepository.GetQueryable()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Id)
            .Select(m => new RTSPaymentModeLookupDto
            {
                Id = m.Id,
                ModeCode = m.ModeCode,
                ModeNameEn = m.ModeNameEn,
                ModeNameMr = m.ModeNameMr,
                IconName = m.IconName,
                IsActive = m.IsActive
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
                DemandId = c.DemandId,
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
