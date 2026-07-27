using NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ApprovalFlowMaster CRUD operations
/// </summary>
public interface IApprovalFlowMasterService : ICommonCrudService<ApprovalFlowMasterEntity, ApprovalFlowMasterDto, CreateApprovalFlowMasterDto, UpdateApprovalFlowMasterDto, ApprovalFlowMasterQueryParameters, int>
{
    Task<object?> GetWorkflowStagesByServiceIdAsync(int serviceId, CancellationToken ct = default);
}

/// <summary>
/// Service interface for ApprovalFlowStageMaster CRUD operations
/// </summary>
public interface IApprovalFlowStageMasterService : ICommonCrudService<ApprovalFlowStageMasterEntity, ApprovalFlowStageMasterDto, CreateApprovalFlowStageMasterDto, UpdateApprovalFlowStageMasterDto, ApprovalFlowStageMasterQueryParameters, int>
{
}
