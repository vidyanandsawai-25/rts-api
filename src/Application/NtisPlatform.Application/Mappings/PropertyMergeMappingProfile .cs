using AutoMapper;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PropertyMergeMappingProfile : Profile
{
    public PropertyMergeMappingProfile()
    {
        CreateMap<PropertyMapDetailEntity, PropertyMapDetailEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate,
                opt => opt.MapFrom(_ => DateTime.Now));

        CreateMap<MergeDetailEntity, MergeDetailEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive,
                opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.CreatedDate,
                opt => opt.MapFrom(_ => DateTime.Now));
    }
}
