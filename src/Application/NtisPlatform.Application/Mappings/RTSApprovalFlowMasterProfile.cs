using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RTSApprovalFlowMasterProfile : Profile
{
    public RTSApprovalFlowMasterProfile()
    {
        CreateMap<RTSApprovalFlowMasterEntity, RTSApprovalFlowMasterDto>();
        CreateMap<CreateRTSApprovalFlowMasterDto, RTSApprovalFlowMasterEntity>();
        CreateMap<UpdateRTSApprovalFlowMasterDto, RTSApprovalFlowMasterEntity>();

        CreateMap<RTSApprovalFlowStageMasterEntity, RTSApprovalFlowStageMasterDto>();
        CreateMap<CreateRTSApprovalFlowStageMasterDto, RTSApprovalFlowStageMasterEntity>();
        CreateMap<UpdateRTSApprovalFlowStageMasterDto, RTSApprovalFlowStageMasterEntity>();
    }
}
