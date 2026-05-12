using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class WaterConnectionStatusMappingProfile : Profile
{
    public WaterConnectionStatusMappingProfile()
    {
        CreateMap<WaterConnectionStatusEntity, WaterConnectionStatusDto>();
        CreateMap<WaterConnectionStatusDto, WaterConnectionStatusEntity>();

        CreateMap<CreateWaterConnectionStatusDto, WaterConnectionStatusEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateWaterConnectionStatusDto, WaterConnectionStatusEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
