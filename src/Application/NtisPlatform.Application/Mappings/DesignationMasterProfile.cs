using AutoMapper;
using NtisPlatform.Application.DTOs.Master.DesignationMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for DesignationMaster entity and DTOs
/// </summary>
public class DesignationMasterProfile : Profile
{
    public DesignationMasterProfile()
    {
        // Entity to DTO - AutoMapper will map by convention, ignored properties won't cause issues
        CreateMap<DesignationMasterEntity, DesignationMasterDto>()
            ;

        // Create DTO to Entity
        CreateMap<CreateDesignationMasterDto, DesignationMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));        
        // Update DTO to Entity
        CreateMap<UpdateDesignationMasterDto, DesignationMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())            
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
