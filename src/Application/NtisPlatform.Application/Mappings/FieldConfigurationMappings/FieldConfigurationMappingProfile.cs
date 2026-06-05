using AutoMapper;
using NtisPlatform.Application.DTOs.FieldConfiguration;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings.FieldConfigurationMappings
{
    /// <summary>
    /// AutoMapper profile for FieldConfiguration entity and DTOs
    /// </summary>
    public class FieldConfigurationMappingProfile : Profile
    {
        public FieldConfigurationMappingProfile()
        {
            // Entity to DTO mapping
            CreateMap<FieldConfigurationEntity, FieldConfigurationDto>()
                .ForMember(dest => dest.FieldName, opt => opt.MapFrom(src => src.RulesField != null ? src.RulesField.FieldName : null));

            // Create DTO to Entity mapping
            CreateMap<CreateFieldConfigurationDto, FieldConfigurationEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RulesField, opt => opt.Ignore());

            // Update DTO to Entity mapping
            CreateMap<UpdateFieldConfigurationDto, FieldConfigurationEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.RulesField, opt => opt.Ignore());
        }
    }
}
