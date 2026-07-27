using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RTSServiceMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class RTSServiceMappingProfile:Profile
{
    public RTSServiceMappingProfile()
    {
        CreateMap<RTSServiceEntity, RTSServiceDto>();

        CreateMap<CreateRTSServiceDto, RTSServiceEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRTSServiceDto, RTSServiceEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
