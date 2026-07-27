using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ApprovalFlowMaster CRUD operations
/// </summary>
public class ApprovalFlowMasterService : BaseCommonCrudService<ApprovalFlowMasterEntity, ApprovalFlowMasterDto, CreateApprovalFlowMasterDto, UpdateApprovalFlowMasterDto, ApprovalFlowMasterQueryParameters, int>, IApprovalFlowMasterService
{
    private readonly IRepository<ApprovalFlowStageMasterEntity, int> _stageRepository;

    public ApprovalFlowMasterService(
        IRepository<ApprovalFlowMasterEntity, int> repository,
        IRepository<ApprovalFlowStageMasterEntity, int> stageRepository,
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

        var stages = await _stageRepository.GetAsync(s => s.ApprovalFlowId == flow.Id, ct);
        var orderedStages = stages.OrderBy(s => s.StageOrder).ToList();

        return new
        {
            flowId = flow.Id,
            serviceId = flow.ServiceId,
            flowName = flow.ApprovalFlowName,
            stages = _mapper.Map<List<ApprovalFlowStageMasterDto>>(orderedStages)
        };
    }
}

/// <summary>
/// Service for ApprovalFlowStageMaster CRUD operations
/// </summary>
public class ApprovalFlowStageMasterService : BaseCommonCrudService<ApprovalFlowStageMasterEntity, ApprovalFlowStageMasterDto, CreateApprovalFlowStageMasterDto, UpdateApprovalFlowStageMasterDto, ApprovalFlowStageMasterQueryParameters, int>, IApprovalFlowStageMasterService
{
    public ApprovalFlowStageMasterService(
        IRepository<ApprovalFlowStageMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
