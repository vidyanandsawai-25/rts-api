using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Linq.Dynamic.Core;
namespace NtisPlatform.Application.Services;

public class RTSApplicationService : BaseCommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSApplicationQueryParameters, int>, IRTSApplicationService
{
    private readonly IRTSCitizenSessionService _sessionService;
    private readonly IRepository<RTSApprovalFlowMasterEntity, int> _approvalFlowRepository;


    public RTSApplicationService(
        IRepository<RTSApplicationDetailsEntity, int> repository,
        IRepository<RTSApprovalFlowMasterEntity, int> approvalFlowRepository,
        IRTSCitizenSessionService sessionService,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _sessionService = sessionService;
        _approvalFlowRepository = approvalFlowRepository;
    }
       public override async Task<RTSApplicationDetailsDto> CreateAsync(CreateRTSApplicationDetailsDto createDto, CancellationToken cancellationToken = default)
       {
            if (!string.IsNullOrWhiteSpace(createDto.SessionId))
            {
                var validationResult = await _sessionService.ValidateAndUpdateSessionAsync(createDto.SessionId, cancellationToken);
                if (!validationResult.Success)
                {
                    throw new UnauthorizedAccessException($"CitizenSession_{validationResult.Message}");
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
        entity.UserId = approvalFlowData?.FirstStage?.UserId??0;
        entity.ApprovalFlowId = approvalFlowData?.ApprovalFlowId??0;
        entity.CurrentApprovalFlowStageId = approvalFlowData?.FirstStage?.StageId??0;
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

       if(approvalFlowData!=null)
       {
            entity.TrackApplicationHistory.Add(new TrackApplicationHistoryEntity
            {
                ApprovalFlowId = approvalFlowData.ApprovalFlowId ,
                ApprovalFlowStageId = approvalFlowData?.FirstStage?.StageId ?? 0,
                ActionByUserId = approvalFlowData?.FirstStage?.UserId,
                Status = ApplicationStatus.Pending,
                Remark = ApplicationStatus.Remark,
                Action = ApplicationStatus.Submitted,
                IsReverted = false,
                IsActive = true
            });
       }

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return  _mapper.Map<RTSApplicationDetailsDto>(entity);

    }
}
