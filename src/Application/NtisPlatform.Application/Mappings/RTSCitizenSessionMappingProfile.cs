using AutoMapper;
using NtisPlatform.Application.DTOs.RTSCitizenSession;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RTSCitizenSessionMappingProfile:Profile
{
    public RTSCitizenSessionMappingProfile()
    {
        CreateMap<RTSCitizenSessionEntity, RTSCitizenSessionDto>();

        CreateMap<CreateRTSCitizenSessionDto, RTSCitizenSessionEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest=>dest.CreatedDate,opt=>opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<UpdateRTSCitizenSessionDto, RTSCitizenSessionEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
