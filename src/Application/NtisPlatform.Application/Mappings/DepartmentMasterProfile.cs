using AutoMapper;
using NtisPlatform.Application.DTOs.Master.DepartmentMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for DepartmentMaster entity and DTOs
/// </summary>
public class DepartmentMasterProfile : Profile
{
    public DepartmentMasterProfile()
    {
        // Entity to DTO - AutoMapper will map by convention, ignored properties won't cause issues
        CreateMap<DepartmentMasterEntity, DepartmentMasterDto>();
        
        // Create DTO to Entity
        CreateMap<CreateDepartmentMasterDto, DepartmentMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
          
        
        // Update DTO to Entity
        CreateMap<UpdateDepartmentMasterDto, DepartmentMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
             .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
            
    }
}
