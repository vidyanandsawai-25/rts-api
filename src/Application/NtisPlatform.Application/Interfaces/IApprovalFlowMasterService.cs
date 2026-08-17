using NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ApprovalFlowMaster CRUD operations
/// </summary>
public interface IApprovalFlowMasterService : ICommonCrudService<RTSApprovalFlowMasterEntity, ApprovalFlowMasterDto, CreateApprovalFlowMasterDto, UpdateApprovalFlowMasterDto, ApprovalFlowMasterQueryParameters, int>
{
    Task<object?> GetWorkflowStagesByServiceIdAsync(int serviceId, CancellationToken ct = default);
}

/// <summary>
/// Service interface for ApprovalFlowStageMaster CRUD operations
/// </summary>
public interface IApprovalFlowStageMasterService : ICommonCrudService<RTSApprovalFlowStageMasterEntity, ApprovalFlowStageMasterDto, CreateApprovalFlowStageMasterDto, UpdateApprovalFlowStageMasterDto, ApprovalFlowStageMasterQueryParameters, int>
{
}
