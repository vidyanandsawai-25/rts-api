using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowStageUserAllocationMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSApprovalFlowStageUserAllocationMasterService : ICommonCrudService<
    RTSApprovalFlowStageUserAllocationMasterEntity,
    RTSApprovalFlowStageUserAllocationMasterDto,
    CreateRTSApprovalFlowStageUserAllocationMasterDto,
    UpdateRTSApprovalFlowStageUserAllocationMasterDto,
    RTSApprovalFlowStageUserAllocationMasterQueryParameters,
    int>
{
}
