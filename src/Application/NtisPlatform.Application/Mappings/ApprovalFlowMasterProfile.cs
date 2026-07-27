using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;
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
    }
}
