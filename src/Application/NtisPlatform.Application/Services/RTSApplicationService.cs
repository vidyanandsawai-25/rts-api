using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSApplication;
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
    private readonly IRepository<TrackApplicationHistoryEntity, int> _historyRepository;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly IAapleSarkarIntegrationService _aapleSarkarService;

    public RTSApplicationService(
        IRepository<RTSApplicationDetailsEntity, int> repository,
        IRepository<RTSApprovalFlowMasterEntity, int> approvalFlowRepository,
        IRTSCitizenSessionService sessionService,
        IRepository<RTSFieldDefinitionEntity, int> fieldDefinitionRepository,
        IRepository<RTSServiceEntity, int> serviceRepository,
        IRepository<RTSFieldValueEntity, int> fieldValueRepository,
        IRepository<TrackApplicationHistoryEntity, int> historyRepository,
        IRepository<RTSApprovalFlowStageMasterEntity, int> approvalFlowStageRepository,
        IRTSSmsNotificationService smsNotificationService,
        IAapleSarkarIntegrationService aapleSarkarService,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _sessionService = sessionService;
        _approvalFlowRepository = approvalFlowRepository;
        _fieldDefinitionRepository = fieldDefinitionRepository;
        _serviceRepository = serviceRepository;
        _fieldValueRepository = fieldValueRepository;
        _smsNotificationService = smsNotificationService;
        _aapleSarkarService = aapleSarkarService;
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
                        s.UserId,
                        s.IsFinalStage
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);


        var rtsService = await _serviceRepository.GetByIdAsync(createDto.ServiceId, cancellationToken);
        bool isPaymentRequired = rtsService != null && rtsService.FeesRequired && (rtsService.Fees ?? 0) > 0;

        var entity = _mapper.Map<RTSApplicationDetailsEntity>(createDto);

        entity.Remark = ApplicationStatus.Remark;
        entity.ApplicationStatus = isPaymentRequired ? "Payment Pending" : ApplicationStatus.Pending;
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

        if (approvalFlowData != null)
        {
            entity.TrackApplicationHistory.Add(new TrackApplicationHistoryEntity
            {
                ApprovalFlowId = approvalFlowData.ApprovalFlowId,
                ApprovalFlowStageId = approvalFlowData?.FirstStage?.StageId ?? 0,
                Status = isPaymentRequired ? "Payment Pending" : ApplicationStatus.Pending,
                Remark = ApplicationStatus.Remark,
                Action = ApplicationStatus.Submitted,
                IsReverted = false,
                IsActive = true
            });
        }

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await DispatchSubmissionSmsAsync(entity, createDto, cancellationToken);

        if (!string.IsNullOrWhiteSpace(createDto.TdToken))
        {
            var appNo = entity.ApplicationNo ?? $"RTS{entity.Id:D8}";
            var targetStatus = isPaymentRequired ? "PaymentPending" : "UnderScrutiny";
            await _aapleSarkarService.MapApplicationAsync(createDto.TdToken, appNo, targetStatus, cancellationToken);
        }

        return _mapper.Map<RTSApplicationDetailsDto>(entity);
    }



    public override async Task<RTSApplicationDetailsDto?> UpdateAsync(int Id, UpdateRTSApplicationDetailsDto updateDto,CancellationToken cancellationToken = default)
    {
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

        foreach (var item in updateDto.FieldValues)
        {
            var fieldValue = fieldValues.FirstOrDefault(x =>
                x.ApplicationId == Id &&
                x.FieldDefinitionId == item.FieldDefinitionId);

            if (fieldValue == null)
                continue;

            fieldValue.TextValue = item.TextValue;
            fieldValue.NumberValue = item.NumberValue;
            fieldValue.DateValue = item.DateValue;
            fieldValue.BooleanValue = item.BooleanValue;
            fieldValue.DocumentGuid = item.DocumentGuid;
            fieldValue.UpdatedBy = updateDto.UpdatedBy;
            fieldValue.UpdatedDate = DateTime.Now;
            await _fieldValueRepository.UpdateAsync(fieldValue, cancellationToken);
        }

        application.ApplicationStatus = ApplicationStatus.Pending;
        application.IsReverted = false;
        application.Remark = "Application Corrected & Resubmitted";
        application.UpdatedBy = updateDto.UpdatedBy;
        application.UpdatedDate = DateTime.Now;

        var history = new TrackApplicationHistoryEntity
        {
            ApplicationId = Id,
            ApprovalFlowId = application.ApprovalFlowId,
            ApprovalFlowStageId = application.CurrentApprovalFlowStageId,
            ActionByUserId = updateDto.UpdatedBy,
            Status = ApplicationStatus.Pending ,
            Action = "Application Resubmitted ",
            Remark = "Application Corrected & Resubmitted",
            IsReverted = false,
            IsActive = true,
            CreatedBy = updateDto.UpdatedBy,
            CreatedDate = DateTime.Now
        };
        
        await _historyRepository.AddAsync(history, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
       

            var statusText =  "RESUBMITTED";
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

