using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class WaterConnectionDetailsMappingProfile : Profile
{
    public WaterConnectionDetailsMappingProfile()
    {
        CreateMap<WaterConnectionDetailsEntity, WaterConnectionDetailsDto>()
            .ForMember(dest => dest.ConnectionNo, opt => opt.MapFrom(src =>
                src.WaterConnection != null ? src.WaterConnection.ConnectionNo : null))
            .ForMember(dest => dest.YearCode, opt => opt.MapFrom(src =>
                src.FinanceYear != null ? src.FinanceYear.YearCode : null));

        CreateMap<CreateWaterConnectionDetailsDto, WaterConnectionDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnection, opt => opt.Ignore())
            .ForMember(dest => dest.FinanceYear, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateWaterConnectionDetailsDto, WaterConnectionDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnection, opt => opt.Ignore())
            .ForMember(dest => dest.FinanceYear, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
