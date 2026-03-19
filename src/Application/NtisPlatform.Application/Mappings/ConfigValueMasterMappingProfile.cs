using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigValueMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for ConfigValueMaster entity and DTOs
/// </summary>
public class ConfigValueMasterMappingProfile : Profile
{
    public ConfigValueMasterMappingProfile()
    {
        // Entity to DTO
        CreateMap<ConfigValueMasterEntity, ConfigValueMasterDto>();
        
        // Create DTO to Entity
        CreateMap<CreateConfigValueMasterDto, ConfigValueMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
        
        // Update DTO to Entity
        CreateMap<UpdateConfigValueMasterDto, ConfigValueMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
