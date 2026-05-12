using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class WaterRateMasterMappingProfile : Profile
{
    public WaterRateMasterMappingProfile()
    {
        CreateMap<WaterRateMasterEntity, WaterRateMasterDto>()
            .ForMember(dest => dest.ConnectionTypeName, opt => opt.MapFrom(src => src.WaterConnectionType.ConnectionTypeName))
            .ForMember(dest => dest.ConnectionSizeDisplay, opt => opt.MapFrom(src =>
                src.WaterConnectionSize.ConnectionSize.ToString("G29") + " " + src.WaterConnectionSize.ConnectionSizeUnit))
            .ForMember(dest => dest.YearCode, opt => opt.MapFrom(src => src.FinanceYear.YearCode));

        CreateMap<CreateWaterRateMasterDto, WaterRateMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionType, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionSize, opt => opt.Ignore())
            .ForMember(dest => dest.FinanceYear, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateWaterRateMasterDto, WaterRateMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionType, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionSize, opt => opt.Ignore())
            .ForMember(dest => dest.FinanceYear, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
