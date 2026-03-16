using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigCategoryMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for ConfigCategoryMaster entity and DTOs
/// </summary>
public class ConfigCategoryMasterMappingProfile : Profile
{
    public ConfigCategoryMasterMappingProfile()
    {
        // Entity to DTO
        CreateMap<ConfigCategoryMasterEntity, ConfigCategoryMasterDto>();
        
        // Create DTO to Entity
        CreateMap<CreateConfigCategoryMasterDto, ConfigCategoryMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
             .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
          
        
        // Update DTO to Entity
        CreateMap<UpdateConfigCategoryMasterDto, ConfigCategoryMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
