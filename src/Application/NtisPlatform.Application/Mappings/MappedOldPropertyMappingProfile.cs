using AutoMapper;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// Mapping profile for mapped old property models.
/// Maps EF Core entities from old tables to their corresponding DTOs.
/// </summary>
public class MappedOldPropertyMappingProfile : Profile
{
    public MappedOldPropertyMappingProfile()
    {
        // Entity to DTO mapping for PropertyMastOldEntity -> MappedOldPropertyMastDto
        CreateMap<PropertyMastOldEntity, MappedOldPropertyMastDto>()
            .ForMember(dest => dest.FloorDetails, opt => opt.Ignore()); // Handled manually after mapping

        // Entity to DTO mapping for PropertyDetailsOldEntity -> MappedOldPropertyFloorDetailDto
        CreateMap<PropertyDetailsOldEntity, MappedOldPropertyFloorDetailDto>()
            .ForMember(dest => dest.PropertyMastOldId, opt => opt.MapFrom(src => src.PropertyMastOldId)); // Make sure the foreign key is mapped if needed
    }
}
