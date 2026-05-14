using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class CombinePropertyMappingProfile : Profile
{
    public CombinePropertyMappingProfile()
    {
       
        CreateMap<PropertyEntity, CombinePropertyDto>()
            .ForMember(dest => dest.FromProperty, opt => opt.MapFrom(src => src.PartitionNo))
            .ForMember(dest => dest.ToProperty, opt => opt.MapFrom(src => src.PartitionNo));
        
        CreateMap<CreateCombinePropertyDto, PropertyEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WardId, opt => opt.MapFrom(src => src.WardId))
            .ForMember(dest => dest.TaxZoneId, opt => opt.MapFrom(src => src.TaxZoneId))
            .ForMember(dest => dest.PropertyNo, opt => opt.MapFrom(src => src.PropertyNo))
            .ForMember(dest => dest.PartitionNo, opt => opt.MapFrom(src => src.PartitionNo));

        CreateMap<UpdateCombinePropertyDto, PropertyEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WardId, opt => opt.MapFrom(src => src.WardId))
            .ForMember(dest => dest.TaxZoneId, opt => opt.MapFrom(src => src.TaxZoneId))
            .ForMember(dest => dest.PropertyNo, opt => opt.MapFrom(src => src.PropertyNo))
            .ForMember(dest => dest.PartitionNo, opt => opt.MapFrom(src => src.PartitionNo));
    }
}