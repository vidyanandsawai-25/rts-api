using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ModuleMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for ModuleMaster entity and DTOs
/// </summary>
public class ModuleMasterMappingProfile : Profile
{
    public ModuleMasterMappingProfile()
    {
        // Entity to DTO
        CreateMap<ModuleMasterEntity, ModuleMasterDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : null));
        
        // Create DTO to Entity
        CreateMap<CreateModuleMasterDto, ModuleMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
             .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));   
        // Update DTO to Entity
        CreateMap<UpdateModuleMasterDto, ModuleMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
           .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
         
    }
}
