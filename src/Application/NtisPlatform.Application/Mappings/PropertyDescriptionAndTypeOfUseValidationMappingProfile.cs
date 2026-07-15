using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PropertyDescriptionAndTypeOfUseValidationMappingProfile : Profile
{
    public PropertyDescriptionAndTypeOfUseValidationMappingProfile()
    {
        CreateMap<PropertyDescriptionAndTypeOfUseValidationEntity, PropertyDescriptionAndTypeOfUseValidationDto>();

        CreateMap<CreatePropertyDescriptionAndTypeOfUseValidationDto, PropertyDescriptionAndTypeOfUseValidationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.TypeOfUse, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTypeMaster, opt => opt.Ignore());

        CreateMap<UpdatePropertyDescriptionAndTypeOfUseValidationDto, PropertyDescriptionAndTypeOfUseValidationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.TypeOfUse, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTypeMaster, opt => opt.Ignore());
    }
}
