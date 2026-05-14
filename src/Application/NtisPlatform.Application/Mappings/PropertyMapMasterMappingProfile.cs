using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PropertyMapMasterMappingProfile : Profile
{
    public PropertyMapMasterMappingProfile()
    {
        CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();

        CreateMap<CreatePropertyMapMasterDto, PropertyMapMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertyMapMasterDto, PropertyMapMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}