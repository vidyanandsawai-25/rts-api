using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ApprovalFlowMaster CRUD operations
/// </summary>
public class ApprovalFlowMasterService : BaseCommonCrudService<RTSApprovalFlowMasterEntity, ApprovalFlowMasterDto, CreateApprovalFlowMasterDto, UpdateApprovalFlowMasterDto, ApprovalFlowMasterQueryParameters, int>, IApprovalFlowMasterService
{
    private readonly IRepository<RTSApprovalFlowStageMasterEntity, int> _stageRepository;

    public ApprovalFlowMasterService(
        IRepository<RTSApprovalFlowMasterEntity, int> repository,
        IRepository<RTSApprovalFlowStageMasterEntity, int> stageRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _stageRepository = stageRepository;
    }

    public async Task<object?> GetWorkflowStagesByServiceIdAsync(int serviceId, CancellationToken ct = default)
    {
        var flows = await _repository.GetAsync(f => f.ServiceId == serviceId && f.IsActive, ct);
        var flow = flows.FirstOrDefault();
        if (flow == null) return null;

        //var stages = await _stageRepository.GetAsync(s => s.ApprovalFlowId == flow.Id, ct);
        var stages = await _stageRepository
      .GetQueryable()
      .AsNoTracking()
      .Where(s => s.ApprovalFlowId == flow.Id)
      .OrderBy(s => s.StageOrder)
      .Select(s => new ApprovalFlowStageMasterDto
      {
          Id = s.Id,
          ApprovalFlowId = s.ApprovalFlowId,
          StageOrder = s.StageOrder,
          StageName = s.StageName,
          SlaDays = s.SLADays,

          UserName = s.User.UserName,
          FirstName = s.User.FirstName,
          MiddleName = s.User.MiddleName,
          LastName = s.User.LastName,

          CanVerifyDocument = s.CanVerifyDocument,
          CanApprove = s.CanApprove,
          CanReject = s.CanReject,
          CanReturn = s.CanReturn,
          CanPay = s.CanPay,
          IsFinalStage = s.IsFinalStage
      })
      .ToListAsync(ct);
        var orderedStages = stages.OrderBy(s => s.StageOrder).ToList();

        return new
        {
            flowId = flow.Id,
            serviceId = flow.ServiceId,
            flowName = flow.ApprovalFlowName,
            stages = stages
        };
    }
}

/// <summary>
/// Service for ApprovalFlowStageMaster CRUD operations
/// </summary>
public class ApprovalFlowStageMasterService : BaseCommonCrudService<RTSApprovalFlowStageMasterEntity, ApprovalFlowStageMasterDto, CreateApprovalFlowStageMasterDto, UpdateApprovalFlowStageMasterDto, ApprovalFlowStageMasterQueryParameters, int>, IApprovalFlowStageMasterService
{
    public ApprovalFlowStageMasterService(
        IRepository<RTSApprovalFlowStageMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
