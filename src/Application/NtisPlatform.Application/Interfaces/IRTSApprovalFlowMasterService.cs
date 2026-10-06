using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ApprovalFlowMaster CRUD operations
/// </summary>
public interface IRTSApprovalFlowMasterService : ICommonCrudService<RTSApprovalFlowMasterEntity, RTSApprovalFlowMasterDto, CreateRTSApprovalFlowMasterDto, UpdateRTSApprovalFlowMasterDto, RTSApprovalFlowMasterQueryParameters, int>
{
    Task<object?> GetWorkflowStagesByServiceIdAsync(int serviceId, CancellationToken ct = default);
}

/// <summary>
/// Service interface for ApprovalFlowStageMaster CRUD operations
/// </summary>
public interface IRTSApprovalFlowStageMasterService : ICommonCrudService<RTSApprovalFlowStageMasterEntity, RTSApprovalFlowStageMasterDto, CreateRTSApprovalFlowStageMasterDto, UpdateRTSApprovalFlowStageMasterDto, RTSApprovalFlowStageMasterQueryParameters, int>
{
}
