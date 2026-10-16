using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.Linq.Dynamic.Core;
namespace NtisPlatform.Application.Services;

public class RTSApplicationService : BaseCommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSApplicationDetailsDto, RTSApplicationQueryParameters, int>, IRTSApplicationService
{
    private readonly IRTSCitizenSessionService _sessionService;
    private readonly IRepository<RTSApprovalFlowMasterEntity, int> _approvalFlowRepository;
    private readonly IRepository<RTSFieldDefinitionEntity, int> _fieldDefinitionRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IRepository<RTSFieldValueEntity, int> _fieldValueRepository;
    private readonly IRepository<RTSPaymentTransactionEntity, int> _PaymentTransactionRepository;
    private readonly IRepository<TrackApplicationHistoryEntity, int> _historyRepository;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly IAapleSarkarIntegrationService _aapleSarkarService;
    private readonly IRTSStageAllocationRuleService _allocationRules;

    public RTSApplicationService(
        IRepository<RTSApplicationDetailsEntity, int> repository,
        IRepository<RTSApprovalFlowMasterEntity, int> approvalFlowRepository,
        IRepository<RTSPaymentTransactionEntity, int> PaymentTransactionRepository,
        IRTSCitizenSessionService sessionService,
        IRepository<RTSFieldDefinitionEntity, int> fieldDefinitionRepository,
        IRepository<RTSServiceEntity, int> serviceRepository,
        IRepository<RTSFieldValueEntity, int> fieldValueRepository,
        IRepository<TrackApplicationHistoryEntity, int> historyRepository,
        IRepository<RTSApprovalFlowStageMasterEntity, int> approvalFlowStageRepository,
        IRTSSmsNotificationService smsNotificationService,
        IAapleSarkarIntegrationService aapleSarkarService,
        IRTSStageAllocationRuleService allocationRules,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _sessionService = sessionService;
        _approvalFlowRepository = approvalFlowRepository;
        _fieldDefinitionRepository = fieldDefinitionRepository;
        _serviceRepository = serviceRepository;
        _PaymentTransactionRepository = PaymentTransactionRepository;
        _fieldValueRepository = fieldValueRepository;
        _smsNotificationService = smsNotificationService;
        _aapleSarkarService = aapleSarkarService;
        _allocationRules = allocationRules;
        _historyRepository = historyRepository;
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
                        s.IsFinalStage
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var rtsService = await _serviceRepository.GetByIdAsync(createDto.ServiceId, cancellationToken);
        if (rtsService == null || !rtsService.IsActive)
            throw new ValidationException("The selected RTS service is unavailable.", OperationType.Create);

        var approvalFlow = approvalFlowData ?? throw new InvalidOperationException(
            "This service has no active approval workflow stage. Configure an active workflow and stage allocation before accepting applications.");

        var firstStage = approvalFlow.FirstStage ?? throw new InvalidOperationException(
            "This service has no active approval workflow stage. Configure an active workflow and stage allocation before accepting applications.");

        bool isPaymentRequired = rtsService != null && rtsService.FeesRequired && (rtsService.Fees ?? 0) > 0 && !rtsService.IsPaymentAfterApproval;

        var entity = _mapper.Map<RTSApplicationDetailsEntity>(createDto);

        entity.Remark = ApplicationStatus.Remark;
        entity.ApplicationStatus = isPaymentRequired ? ApplicationStatus.PaymentPending : ApplicationStatus.Pending;
        entity.UserId = 0;
        entity.ApprovalFlowId = approvalFlow.ApprovalFlowId;
        entity.CurrentApprovalFlowStageId = firstStage.StageId;
        entity.CurrentStageOrder = firstStage.StageOrder;


        var submittedFieldValues = createDto.FieldValues ?? new List<CreateRTSFieldValueDto>();
        await ValidateFieldValuesAsync(createDto.ServiceId, submittedFieldValues.Select(x => x.FieldDefinitionId), cancellationToken);

        if (submittedFieldValues.Count != 0)
        {
            entity.FieldValueData = submittedFieldValues
                .Select(f =>
                {
                    var field = _mapper.Map<RTSFieldValueEntity>(f);
                    field.CreatedBy = createDto.CreatedBy;
                    return field;
                })
                .ToList();
        }

        entity.UserId = await _allocationRules.ResolveOfficerAsync(
            createDto.ServiceId, approvalFlow.ApprovalFlowId,
            firstStage.StageId,
            _allocationRules.NormalizeValues(entity.FieldValueData), cancellationToken);

        TrackApplicationHistoryEntity? submissionHistory = null;
        submissionHistory = new TrackApplicationHistoryEntity
        {
            ApprovalFlowId = approvalFlow.ApprovalFlowId,
            ApprovalFlowStageId = firstStage.StageId,
            Status = isPaymentRequired ? ApplicationStatus.PaymentPending : ApplicationStatus.Pending,
            Remark = ApplicationStatus.Remark,
            Action = ApplicationStatus.Submitted,
            IsReverted = false,
            IsActive = true
        };
        entity.TrackApplicationHistory.Add(submissionHistory);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }

        await DispatchSubmissionSmsAsync(entity, createDto, cancellationToken);

        if (!string.IsNullOrWhiteSpace(createDto.TdToken))
        {
            var appNo = entity.ApplicationNo ?? $"RTS{entity.Id:D8}";
            var targetStatus = isPaymentRequired ? "PaymentPending" : "UnderScrutiny";
            await _aapleSarkarService.MapApplicationAsync(createDto.TdToken, appNo, targetStatus, cancellationToken);
        }

        return _mapper.Map<RTSApplicationDetailsDto>(entity);
    }

    public override async Task<RTSApplicationDetailsDto?> UpdateAsync(int Id, UpdateRTSApplicationDetailsDto updateDto, CancellationToken cancellationToken = default)
    {

        if (!string.IsNullOrWhiteSpace(updateDto.SessionId))
        {
            var sessionValidation = await _sessionService.ValidateAndUpdateSessionAsync(updateDto.SessionId, cancellationToken);
            if (!sessionValidation.Success)
            {
                updateDto.SessionId = null;
            }
        }

        var application = await _repository
         .GetQueryable()
         .Include(x => x.Service)
         .FirstOrDefaultAsync(x => x.Id == Id && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (application == null)
            throw new ValidationException("Application not found.", OperationType.Update);

        var wasReverted = application.IsReverted || application.ApplicationStatus == ApplicationStatus.Reverted;
        if (!wasReverted)
            throw new ValidationException("Only reverted applications can be resubmitted.", OperationType.Update);


        var fieldValues = await _fieldValueRepository
            .GetQueryable()
            .Where(x =>
                x.ApplicationId == Id &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var submittedFieldValues = updateDto.FieldValues ?? new List<UpdateRTSFieldValueDto>();
        await ValidateFieldValuesAsync(application.ServiceId, submittedFieldValues.Select(x => x.FieldDefinitionId), cancellationToken);

        foreach (var item in submittedFieldValues)
        {
            var fieldValue = fieldValues.FirstOrDefault(x =>
                x.ApplicationId == Id &&
                x.FieldDefinitionId == item.FieldDefinitionId);

            if (fieldValue == null)
            {
                fieldValue = new RTSFieldValueEntity
                {
                    ApplicationId = application.Id,
                    FieldDefinitionId = item.FieldDefinitionId,
                    IsActive = true,
                    CreatedBy = updateDto.UpdatedBy,
                    CreatedDate = DateTime.Now
                };
                fieldValues.Add(fieldValue);
                await _fieldValueRepository.AddAsync(fieldValue, cancellationToken);
            }

            fieldValue.TextValue = item.TextValue;
            fieldValue.NumberValue = item.NumberValue;
            fieldValue.DateValue = item.DateValue;
            fieldValue.BooleanValue = item.BooleanValue;
            fieldValue.DocumentGuid = item.DocumentGuid;
            fieldValue.UpdatedBy = updateDto.UpdatedBy;
            fieldValue.UpdatedDate = DateTime.Now;
            if (fieldValue.Id != 0)
                await _fieldValueRepository.UpdateAsync(fieldValue, cancellationToken);
        }

        application.UserId = await _allocationRules.ResolveOfficerAsync(
            application.ServiceId,
            application.ApprovalFlowId,
            application.CurrentApprovalFlowStageId,
            _allocationRules.NormalizeValues(fieldValues),
            cancellationToken);

        var isPaymentRequired = application.Service != null && application.Service.FeesRequired && (application.Service.Fees ?? 0) > 0 && application.Service.IsPaymentAfterApproval;

        var isPaymentSuccessful = false;

        if (isPaymentRequired)
        {
            isPaymentSuccessful = await _PaymentTransactionRepository
                .GetQueryable()
                .Include(x => x.PaymentStatus)
                .AnyAsync(x =>
                    x.ApplicationId == application.Id &&
                    x.IsActive &&
                    x.PaymentStatus.StatusCode == "SUCCESS",
                    cancellationToken);
        }

        application.ApplicationStatus = (isPaymentRequired && !isPaymentSuccessful) ? ApplicationStatus.PaymentPending : ApplicationStatus.Pending;
        application.IsReverted = false;
        application.UpdatedBy = updateDto.UpdatedBy;
        application.UpdatedDate = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(updateDto.ApplicantName))
            application.ApplicantName = updateDto.ApplicantName;
        if (!string.IsNullOrWhiteSpace(updateDto.ApplicantMobileNo))
            application.ApplicantMobileNo = updateDto.ApplicantMobileNo;

        var resubmitRemark = string.IsNullOrWhiteSpace(updateDto.Remark)
            ? "Application Resubmitted"
            : updateDto.Remark.Trim();
        application.Remark = resubmitRemark;


        var history = new TrackApplicationHistoryEntity
        {
            ApplicationId = Id,
            ApprovalFlowId = application.ApprovalFlowId,
            ApprovalFlowStageId = application.CurrentApprovalFlowStageId,
            ActionByUserId = updateDto.UpdatedBy,
            Status = application.ApplicationStatus,
            Action = "Application Resubmitted By Citizen",
            Remark = resubmitRemark,
            IsReverted = false,
            IsActive = true,
            CreatedBy = updateDto.UpdatedBy,
            CreatedDate = DateTime.Now
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _historyRepository.AddAsync(history, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }


        var statusText = "RESUBMITTED";
        if (!string.IsNullOrWhiteSpace(application.ApplicantMobileNo))
        {
            await _smsNotificationService.SendApplicationStatusUpdateAsync(
             application.Id,
             application.ApplicationNo ?? $"APP{application.Id}",
             "Citizen",
             application.ApplicantMobileNo,
             application.Service?.ServiceName ?? "RTS Service",
             statusText,
             null,
             cancellationToken);
        }

        return _mapper.Map<RTSApplicationDetailsDto>(application);

    }

    /// <summary>
    /// The same values which are persisted for an application are supplied to the
    /// nested fee and allocation rule matchers.  Reject foreign or duplicate fields
    /// before an application can be routed with data from another service.
    /// </summary>
    private async Task ValidateFieldValuesAsync(
        int serviceId,
        IEnumerable<int> fieldDefinitionIds,
        CancellationToken cancellationToken)
    {
        var fieldIds = fieldDefinitionIds.ToList();
        if (fieldIds.Any(id => id <= 0))
            throw new ValidationException("Each submitted field value must identify a valid field.", OperationType.Create);

        var distinctIds = fieldIds.Distinct().ToList();
        if (distinctIds.Count != fieldIds.Count)
            throw new ValidationException("A field can be submitted only once.", OperationType.Create);
        if (distinctIds.Count == 0)
            return;

        var validCount = await _fieldDefinitionRepository.GetQueryable()
            .AsNoTracking()
            .CountAsync(field => field.ServiceId == serviceId && field.IsActive &&
                !field.MarkedForDeletion && distinctIds.Contains(field.Id), cancellationToken);
        if (validCount != distinctIds.Count)
            throw new ValidationException(
                "Every submitted field must be an active field of the selected RTS service.",
                OperationType.Create);
    }


    private async Task DispatchSubmissionSmsAsync(RTSApplicationDetailsEntity entity, CreateRTSApplicationDetailsDto createDto, CancellationToken ct)
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
                var feeAmount = service?.FeesRequired == true ? (service?.Fees ?? 0) : 0;

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

