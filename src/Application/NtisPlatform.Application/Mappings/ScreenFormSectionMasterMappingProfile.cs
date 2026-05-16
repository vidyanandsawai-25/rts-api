using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class ScreenFormSectionMasterMappingProfile : Profile
{
    public ScreenFormSectionMasterMappingProfile()
    {
        CreateMap<ScreenFormSectionMasterEntity, ScreenFormSectionMasterDto>();

        CreateMap<CreateScreenFormSectionMasterDto, ScreenFormSectionMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateScreenFormSectionMasterDto, ScreenFormSectionMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}