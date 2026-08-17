using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class ApprovalFlowMasterProfile : Profile
{
    public ApprovalFlowMasterProfile()
    {
        CreateMap<ApprovalFlowMasterEntity, ApprovalFlowMasterDto>();
        CreateMap<CreateApprovalFlowMasterDto, ApprovalFlowMasterEntity>();
        CreateMap<UpdateApprovalFlowMasterDto, ApprovalFlowMasterEntity>();

        CreateMap<ApprovalFlowStageMasterEntity, ApprovalFlowStageMasterDto>();
        CreateMap<CreateApprovalFlowStageMasterDto, ApprovalFlowStageMasterEntity>();
        CreateMap<UpdateApprovalFlowStageMasterDto, ApprovalFlowStageMasterEntity>();

        CreateMap<RTSApprovalFlowMasterEntity, ApprovalFlowMasterDto>();
        CreateMap<CreateApprovalFlowMasterDto, RTSApprovalFlowMasterEntity>();
        CreateMap<UpdateApprovalFlowMasterDto, RTSApprovalFlowMasterEntity>();

        CreateMap<RTSApprovalFlowStageMasterEntity, ApprovalFlowStageMasterDto>();
        CreateMap<CreateApprovalFlowStageMasterDto, RTSApprovalFlowStageMasterEntity>();
        CreateMap<UpdateApprovalFlowStageMasterDto, RTSApprovalFlowStageMasterEntity>();
    }
}
