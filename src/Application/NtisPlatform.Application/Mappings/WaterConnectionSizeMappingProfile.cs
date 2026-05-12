using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class WaterConnectionSizeMappingProfile : Profile
{
    public WaterConnectionSizeMappingProfile()
    {
        CreateMap<WaterConnectionSizeEntity, WaterConnectionSizeDto>()
            .ForMember(dest => dest.DisplayLabel, opt => opt.MapFrom(src =>
                src.ConnectionSize.ToString("G29") + " " + src.ConnectionSizeUnit));

        CreateMap<WaterConnectionSizeDto, WaterConnectionSizeEntity>()
            .ForMember(dest => dest.ConnectionSize, opt => opt.MapFrom(src => src.ConnectionSize))
            .ForMember(dest => dest.ConnectionSizeUnit, opt => opt.MapFrom(src => src.ConnectionSizeUnit));

        CreateMap<CreateWaterConnectionSizeDto, WaterConnectionSizeEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateWaterConnectionSizeDto, WaterConnectionSizeEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
