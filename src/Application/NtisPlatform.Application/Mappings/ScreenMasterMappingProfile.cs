using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for ScreenMaster entity and DTOs
/// </summary>
public class ScreenMasterMappingProfile : Profile
{
    public ScreenMasterMappingProfile()
    {
        CreateMap<ScreenMasterEntity, ScreenMasterDto>();

        CreateMap<CreateScreenMasterDto, ScreenMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));           

        CreateMap<UpdateScreenMasterDto, ScreenMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
