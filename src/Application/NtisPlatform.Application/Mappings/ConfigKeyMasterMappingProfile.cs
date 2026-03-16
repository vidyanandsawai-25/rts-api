using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigKeyMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for ConfigKeyMaster entity and DTOs
/// </summary>
public class ConfigKeyMasterMappingProfile : Profile
{
    public ConfigKeyMasterMappingProfile()
    {
        // Entity to DTO
        CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();        
        // Create DTO to Entity
        CreateMap<CreateConfigKeyMasterDto, ConfigKeyMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
        // Update DTO to Entity
        CreateMap<UpdateConfigKeyMasterDto, ConfigKeyMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())            
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));             
    }
}
