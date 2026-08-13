using AutoMapper;
using NtisPlatform.Application.DTOs.PropertyMapDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PropertyMapDetailMappingProfile : Profile
{
    public PropertyMapDetailMappingProfile()
    {
        CreateMap<PropertyMapDetailEntity, PropertyMapDetailDto>().ReverseMap();
        
        CreateMap<CreatePropertyMapDetailsDto, PropertyMapDetailEntity>()
            .ForMember(dest => dest.PropertyIdNew, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.TaxSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.AreaSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ChangeReason, opt => opt.Ignore())
            .ForMember(dest => dest.Remark, opt => opt.Ignore());
            
        CreateMap<UpdatePropertyMapDetailsDto, PropertyMapDetailEntity>()
            .ForMember(dest => dest.PropertyIdNew, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.TaxSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.AreaSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ChangeReason, opt => opt.Ignore())
            .ForMember(dest => dest.Remark, opt => opt.Ignore());

        CreateMap<CreatePropertyMapDetailsDto, PropertyMapDetailDto>()
            .ForMember(dest => dest.PropertyIdNew, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.TaxSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.AreaSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ChangeReason, opt => opt.Ignore())
            .ForMember(dest => dest.Remark, opt => opt.Ignore());

        CreateMap<UpdatePropertyMapDetailsDto, PropertyMapDetailDto>()
            .ForMember(dest => dest.PropertyIdNew, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.TaxSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.AreaSharePercent, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ChangeReason, opt => opt.Ignore())
            .ForMember(dest => dest.Remark, opt => opt.Ignore());
    }
}
