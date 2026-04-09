using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ScreenGroupMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for ScreenGroupMaster entity and DTOs
/// </summary>
public class ScreenGroupMasterProfile : Profile
{
    public ScreenGroupMasterProfile()
    {
        // Entity to DTO
        CreateMap<ScreenGroupMasterEntity, ScreenGroupMasterDto>()
            ;

        // Create DTO to Entity
        CreateMap<CreateScreenGroupMasterDto, ScreenGroupMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
            
        
        // Update DTO to Entity
        CreateMap<UpdateScreenGroupMasterDto, ScreenGroupMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
