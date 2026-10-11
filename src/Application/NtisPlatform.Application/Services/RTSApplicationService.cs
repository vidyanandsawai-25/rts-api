using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.DTOs.RTSPayment;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.Linq.Dynamic.Core;
namespace NtisPlatform.Application.Services;

public class RTSApplicationService : BaseCommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSApplicationQueryParameters, int>, IRTSApplicationService
{
    private readonly IRTSCitizenSessionService _sessionService;
    private readonly IRepository<RTSApprovalFlowMasterEntity, int> _approvalFlowRepository;
    private readonly IRepository<RTSFieldDefinitionEntity, int> _fieldDefinitionRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly IAapleSarkarIntegrationService _aapleSarkarService;
    private readonly IRTSPaymentService _paymentService;
    private readonly IRTSStageAllocationRuleService _allocationRules;

    public RTSApplicationService(
        IRepository<RTSApplicationDetailsEntity, int> repository,
        IRepository<RTSApprovalFlowMasterEntity, int> approvalFlowRepository,
        IRTSCitizenSessionService sessionService,
        IRepository<RTSFieldDefinitionEntity, int> fieldDefinitionRepository,
        IRepository<RTSServiceEntity, int> serviceRepository,
        IRTSSmsNotificationService smsNotificationService,
        IAapleSarkarIntegrationService aapleSarkarService,
        IRTSPaymentService paymentService,
        IRTSStageAllocationRuleService allocationRules,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _sessionService = sessionService;
        _approvalFlowRepository = approvalFlowRepository;
        _fieldDefinitionRepository = fieldDefinitionRepository;
        _serviceRepository = serviceRepository;
        _smsNotificationService = smsNotificationService;
        _aapleSarkarService = aapleSarkarService;
        _paymentService = paymentService;
        _allocationRules = allocationRules;
    }
    public override async Task<RTSApplicationDetailsDto> CreateAsync(CreateRTSApplicationDetailsDto createDto, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(createDto.SessionId))
        {
            var sessionValidation = await _sessionService.ValidateAndUpdateSessionAsync(createDto.SessionId, cancellationToken);
            if (!sessionValidation.Success)
            {
                createDto.SessionId = null;
            }
        }

        var approvalFlowData = await _approvalFlowRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.ServiceId == createDto.ServiceId &&
                x.IsActive)
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                ApprovalFlowId = x.Id,
                ApprovalFlowName = x.ApprovalFlowName,

                FirstStage = x.ApprovalFlowStages
                    .OrderBy(s => s.StageOrder)
                    .Select(s => new
                    {
                        StageId = s.Id,
                        s.StageName,
                        s.StageOrder,
                        s.UserId,
                        s.IsFinalStage
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);


        var entity = _mapper.Map<RTSApplicationDetailsEntity>(createDto);

        entity.Remark = ApplicationStatus.Remark;
        entity.ApplicationStatus = ApplicationStatus.Pending;
        entity.UserId = approvalFlowData?.FirstStage?.UserId ?? 0;
        entity.ApprovalFlowId = approvalFlowData?.ApprovalFlowId ?? 0;
        entity.CurrentApprovalFlowStageId = approvalFlowData?.FirstStage?.StageId ?? 0;
        entity.CurrentStageOrder = approvalFlowData?.FirstStage?.StageOrder ?? 0;


        if (createDto.FieldValues?.Any() == true)
        {
            entity.FieldValueData = createDto.FieldValues
                .Select(f =>
                {
                    var field = _mapper.Map<RTSFieldValueEntity>(f);
                    field.CreatedBy = createDto.CreatedBy;
                    return field;
                })
                .ToList();
        }

        if (approvalFlowData?.FirstStage != null)
            entity.UserId = await _allocationRules.ResolveOfficerAsync(
                createDto.ServiceId, approvalFlowData.ApprovalFlowId,
                approvalFlowData.FirstStage.StageId, approvalFlowData.FirstStage.UserId,
                _allocationRules.NormalizeValues(entity.FieldValueData), cancellationToken);

        TrackApplicationHistoryEntity? submissionHistory = null;
        if (approvalFlowData != null)
        {
            submissionHistory = new TrackApplicationHistoryEntity
            {
                ApprovalFlowId = approvalFlowData.ApprovalFlowId,
                ApprovalFlowStageId = approvalFlowData?.FirstStage?.StageId ?? 0,
                Status = ApplicationStatus.Pending,
                Remark = ApplicationStatus.Remark,
                Action = ApplicationStatus.Submitted,
                IsReverted = false,
                IsActive = true
            };
            entity.TrackApplicationHistory.Add(submissionHistory);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        PaymentStatusDto paymentSnapshot;
        try
        {
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            paymentSnapshot = await _paymentService.InitializePendingPaymentAsync(entity.Id, cancellationToken);
            if (paymentSnapshot.IsFeeRequired)
            {
                entity.ApplicationStatus = "Payment Pending";
                if (submissionHistory != null)
                {
                    submissionHistory.Status = "Payment Pending";
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }

        await DispatchSubmissionSmsAsync(entity, createDto, paymentSnapshot.RequiredFee, cancellationToken);

        if (!string.IsNullOrWhiteSpace(createDto.TdToken))
        {
            var appNo = entity.ApplicationNo ?? $"RTS{entity.Id:D8}";
            var targetStatus = paymentSnapshot.IsFeeRequired ? "PaymentPending" : "UnderScrutiny";
            await _aapleSarkarService.MapApplicationAsync(createDto.TdToken, appNo, targetStatus, cancellationToken);
        }

        var result = _mapper.Map<RTSApplicationDetailsDto>(entity);
        result.PaymentTransactionId = paymentSnapshot.TransactionId;
        result.PaymentRequiredFee = paymentSnapshot.RequiredFee;
        result.PaymentStatus = paymentSnapshot.PaymentStatus;
        return result;
    }

    private async Task DispatchSubmissionSmsAsync(
        RTSApplicationDetailsEntity entity,
        CreateRTSApplicationDetailsDto createDto,
        decimal feeAmount,
        CancellationToken ct)
    {
        try
        {
            string? applicantName = "Citizen";
            string? applicantMobile = null;

            var fieldDefinitions = await _fieldDefinitionRepository.GetQueryable()
                .Where(x => x.ServiceId == entity.ServiceId && x.IsActive)
                .ToListAsync(ct);

            if (createDto.FieldValues != null)
            {
                foreach (var f in createDto.FieldValues)
                {
                    var def = fieldDefinitions.FirstOrDefault(d => d.Id == f.FieldDefinitionId);
                    var label = (def?.FieldLabel ?? def?.FieldCode ?? string.Empty).ToLowerInvariant();
                    var code = (def?.FieldCode ?? string.Empty).ToLowerInvariant();
                    var val = (!string.IsNullOrWhiteSpace(f.TextValue) ? f.TextValue : f.NumberValue?.ToString())?.Trim();

                    if (string.IsNullOrWhiteSpace(val)) continue;

                    if (string.IsNullOrWhiteSpace(applicantMobile) &&
                        (label.Contains("mobile") || label.Contains("phone") || label.Contains("contact") || label.Contains("मोबाईल") || code.Contains("mobile") || code.Contains("phone")))
                    {
                        applicantMobile = val;
                    }
                    else if (string.IsNullOrWhiteSpace(applicantName) &&
                        (label.Contains("name") || label.Contains("नाव") || code.Contains("name") || code.Contains("fullname")))
                    {
                        applicantName = val;
                    }
                }
            }

            // Fallback: If mobile not found in form fields, try fetching from citizen session
            if (!string.IsNullOrWhiteSpace(entity.SessionId))
            {
                try
                {
                    var sessionResult = await _sessionService.ValidateAndUpdateSessionAsync(entity.SessionId, ct);
                    if (sessionResult?.Session != null)
                    {
                        if (string.IsNullOrWhiteSpace(applicantMobile) && !string.IsNullOrWhiteSpace(sessionResult.Session.MobileNo))
                        {
                            applicantMobile = sessionResult.Session.MobileNo;
                        }
                        if (string.IsNullOrWhiteSpace(applicantName) && !string.IsNullOrWhiteSpace(sessionResult.Session.CitizenName))
                        {
                            applicantName = sessionResult.Session.CitizenName;
                        }
                    }
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(applicantMobile))
            {
                var service = await _serviceRepository.GetByIdAsync(entity.ServiceId, ct);
                await _smsNotificationService.SendApplicationSubmittedAsync(
                    entity.Id,
                    entity.ApplicationNo ?? $"RTS{entity.Id:D8}",
                    applicantName ?? "Citizen",
                    applicantMobile,
                    service?.ServiceName ?? "RTS Service",
                    feeAmount,
                    ct);
            }
        }
        catch
        {
            // Non-blocking for application creation
        }
    }
}

