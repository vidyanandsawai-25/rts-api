using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowStageUserAllocationMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RTSApprovalFlowStageUserAllocationMasterProfile : Profile
{
    public RTSApprovalFlowStageUserAllocationMasterProfile()
    {
        CreateMap<RTSApprovalFlowStageUserAllocationMasterEntity, RTSApprovalFlowStageUserAllocationMasterDto>();
        CreateMap<CreateRTSApprovalFlowStageUserAllocationMasterDto, RTSApprovalFlowStageUserAllocationMasterEntity>();
        CreateMap<UpdateRTSApprovalFlowStageUserAllocationMasterDto, RTSApprovalFlowStageUserAllocationMasterEntity>();
    }
}
