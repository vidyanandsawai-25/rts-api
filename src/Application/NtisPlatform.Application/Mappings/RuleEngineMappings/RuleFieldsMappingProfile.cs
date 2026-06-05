using AutoMapper;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings.RuleEngineMappings
{
    public class RuleFieldsMappingProfile : Profile
    {
        public RuleFieldsMappingProfile()
        {
            // Map from RulesFieldEntity with left join to FieldConfiguration
            CreateMap<RulesFieldEntity, RuleFieldsDto>()
                // Map fields from RulesFieldEntity
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FieldName, opt => opt.MapFrom(src => src.FieldName))
                .ForMember(dest => dest.FieldType, opt => opt.MapFrom(src => src.FieldType))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => (string?)null)) // Not in entity, map to null
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedOn, opt => opt.MapFrom(src => src.UpdatedDate))
                // Map fields from FieldConfiguration (nullable for left join)
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.DataType : null))
                .ForMember(dest => dest.InputType, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.InputType : null))
                .ForMember(dest => dest.HasApiSource, opt => opt.MapFrom(src => src.FieldConfiguration != null ? (bool?)src.FieldConfiguration.HasApiSource : null))
                .ForMember(dest => dest.ApiEndpoint, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.ApiEndpoint : null))
                .ForMember(dest => dest.ApiMethod, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.ApiMethod : null))
                .ForMember(dest => dest.ApiParameters, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.ApiParameters : null))
                .ForMember(dest => dest.ApiResponseMapping, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.ApiResponseMapping : null))
                .ForMember(dest => dest.HasStaticValues, opt => opt.MapFrom(src => src.FieldConfiguration != null ? (bool?)src.FieldConfiguration.HasStaticValues : null))
                .ForMember(dest => dest.StaticValuesJson, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.StaticValuesJson : null))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.FieldConfiguration != null ? (bool?)src.FieldConfiguration.IsRequired : null))
                .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.DefaultValue : null))
                .ForMember(dest => dest.ValidationRegex, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.ValidationRegex : null))
                .ForMember(dest => dest.MinValue, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.MinValue : null))
                .ForMember(dest => dest.MaxValue, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.MaxValue : null))
                .ForMember(dest => dest.MinLength, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.MinLength : null))
                .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => src.FieldConfiguration != null ? src.FieldConfiguration.MaxLength : null));

            // Create mapping - only maps RulesField fields
            // Configuration is handled separately in the service
            CreateMap<CreateRuleFieldsDto, RulesFieldEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FieldConfiguration, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // Update mapping - only maps RulesField fields
            // Configuration is handled separately in the service
            CreateMap<UpdateRuleFieldsDto, RulesFieldEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FieldConfiguration, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // FieldConfiguration mappings
            CreateMap<CreateRuleFieldsDto, FieldConfigurationEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RulesFieldId, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.RulesField, opt => opt.Ignore())
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.DataType ?? string.Empty))
                .ForMember(dest => dest.InputType, opt => opt.MapFrom(src => src.InputType ?? string.Empty))
                .ForMember(dest => dest.HasApiSource, opt => opt.MapFrom(src => src.HasApiSource ?? false))
                .ForMember(dest => dest.ApiEndpoint, opt => opt.MapFrom(src => src.ApiEndpoint))
                .ForMember(dest => dest.ApiMethod, opt => opt.MapFrom(src => src.ApiMethod))
                .ForMember(dest => dest.ApiParameters, opt => opt.MapFrom(src => src.ApiParameters))
                .ForMember(dest => dest.ApiResponseMapping, opt => opt.MapFrom(src => src.ApiResponseMapping))
                .ForMember(dest => dest.HasStaticValues, opt => opt.MapFrom(src => src.HasStaticValues ?? false))
                .ForMember(dest => dest.StaticValuesJson, opt => opt.MapFrom(src => src.StaticValuesJson))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired ?? false))
                .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.DefaultValue))
                .ForMember(dest => dest.ValidationRegex, opt => opt.MapFrom(src => src.ValidationRegex))
                .ForMember(dest => dest.MinValue, opt => opt.MapFrom(src => src.MinValue))
                .ForMember(dest => dest.MaxValue, opt => opt.MapFrom(src => src.MaxValue))
                .ForMember(dest => dest.MinLength, opt => opt.MapFrom(src => src.MinLength))
                .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => src.MaxLength))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

            CreateMap<UpdateRuleFieldsDto, FieldConfigurationEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RulesFieldId, opt => opt.Ignore()) // Don't change FK
                .ForMember(dest => dest.RulesField, opt => opt.Ignore())
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.DataType ?? string.Empty))
                .ForMember(dest => dest.InputType, opt => opt.MapFrom(src => src.InputType ?? string.Empty))
                .ForMember(dest => dest.HasApiSource, opt => opt.MapFrom(src => src.HasApiSource ?? false))
                .ForMember(dest => dest.ApiEndpoint, opt => opt.MapFrom(src => src.ApiEndpoint))
                .ForMember(dest => dest.ApiMethod, opt => opt.MapFrom(src => src.ApiMethod))
                .ForMember(dest => dest.ApiParameters, opt => opt.MapFrom(src => src.ApiParameters))
                .ForMember(dest => dest.ApiResponseMapping, opt => opt.MapFrom(src => src.ApiResponseMapping))
                .ForMember(dest => dest.HasStaticValues, opt => opt.MapFrom(src => src.HasStaticValues ?? false))
                .ForMember(dest => dest.StaticValuesJson, opt => opt.MapFrom(src => src.StaticValuesJson))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired ?? false))
                .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.DefaultValue))
                .ForMember(dest => dest.ValidationRegex, opt => opt.MapFrom(src => src.ValidationRegex))
                .ForMember(dest => dest.MinValue, opt => opt.MapFrom(src => src.MinValue))
                .ForMember(dest => dest.MaxValue, opt => opt.MapFrom(src => src.MaxValue))
                .ForMember(dest => dest.MinLength, opt => opt.MapFrom(src => src.MinLength))
                .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => src.MaxLength))
                .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Preserve existing value
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Preserve existing value
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore()) // Preserve existing value
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
        }
    }
}
