using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class RuleEffectTypeMappingProfile : Profile
    {
        public RuleEffectTypeMappingProfile()
        {
            // Map from RuleEffectTypeEntity with left join to EffectTypeConfiguration
            CreateMap<RuleEffectTypeEntity, RuleEffectTypeDto>()
                // Map fields from RuleEffectTypeEntity
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EffectType, opt => opt.MapFrom(src => src.EffectType))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
                // Map fields from EffectTypeConfiguration (nullable for left join)
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.DataType : string.Empty))
                .ForMember(dest => dest.InputType, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.InputType : string.Empty))
                .ForMember(dest => dest.HasApiSource, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.HasApiSource : false))
                .ForMember(dest => dest.ApiEndpoint, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.ApiEndpoint : null))
                .ForMember(dest => dest.ApiMethod, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.ApiMethod : null))
                .ForMember(dest => dest.ApiParameters, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.ApiParameters : null))
                // Map static API fields from EffectTypeConfiguration
                .ForMember(dest => dest.StaticApiEndpoint, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.StaticApiEndpoint : null))
                .ForMember(dest => dest.StaticApiInputType, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.StaticApiInputType : null))
                .ForMember(dest => dest.StaticApiMethod, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.StaticApiMethod : null))
                .ForMember(dest => dest.StaticApiParamter, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.StaticApiParamter : null))
                .ForMember(dest => dest.StaticApiResponseMapping, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.StaticApiResponseMapping : null))
                .ForMember(dest => dest.HasStaticValues, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.HasStaticValues : false))
                .ForMember(dest => dest.StaticValuesJson, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.StaticValuesJson : null))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.IsRequired : false))
                .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.DefaultValue : null))
                .ForMember(dest => dest.ValidationRegex, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.ValidationRegex : null))
                .ForMember(dest => dest.MinValue, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.MinValue : null))
                .ForMember(dest => dest.MaxValue, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.MaxValue : null))
                .ForMember(dest => dest.MinLength, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.MinLength : null))
                .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.MaxLength : null))
                .ForMember(dest => dest.ExpressionTemplate, opt => opt.MapFrom(src => src.EffectTypeConfiguration != null ? src.EffectTypeConfiguration.ExpressionTemplate : null));

            // Create mapping - only maps RuleEffectType fields
            // Configuration is handled separately in the service
            CreateMap<CreateRuleEffectTypeDto, RuleEffectTypeEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EffectTypeConfiguration, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            // Update mapping - only maps RuleEffectType fields
            // Configuration is handled separately in the service
            CreateMap<UpdateRuleEffectTypeDto, RuleEffectTypeEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EffectTypeConfiguration, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

            // EffectTypeConfiguration mappings
            CreateMap<CreateRuleEffectTypeDto, EffectTypeConfigurationEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EffectTypeId, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.EffectType, opt => opt.Ignore())
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.DataType ?? string.Empty))
                .ForMember(dest => dest.InputType, opt => opt.MapFrom(src => src.InputType ?? string.Empty))
                .ForMember(dest => dest.HasApiSource, opt => opt.MapFrom(src => src.HasApiSource ?? false))
                .ForMember(dest => dest.ApiEndpoint, opt => opt.MapFrom(src => src.ApiEndpoint))
                .ForMember(dest => dest.ApiMethod, opt => opt.MapFrom(src => src.ApiMethod))
                .ForMember(dest => dest.ApiParameters, opt => opt.MapFrom(src => src.ApiParameters))
                .ForMember(dest => dest.StaticApiEndpoint, opt => opt.MapFrom(src => src.StaticApiEndpoint))
                .ForMember(dest => dest.StaticApiInputType, opt => opt.MapFrom(src => src.StaticApiInputType))
                .ForMember(dest => dest.StaticApiMethod, opt => opt.MapFrom(src => src.StaticApiMethod))
                .ForMember(dest => dest.StaticApiParamter, opt => opt.MapFrom(src => src.StaticApiParamter))
                .ForMember(dest => dest.StaticApiResponseMapping, opt => opt.MapFrom(src => src.StaticApiResponseMapping))
                .ForMember(dest => dest.HasStaticValues, opt => opt.MapFrom(src => src.HasStaticValues ?? false))
                .ForMember(dest => dest.StaticValuesJson, opt => opt.MapFrom(src => src.StaticValuesJson))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired ?? false))
                .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.DefaultValue))
                .ForMember(dest => dest.ValidationRegex, opt => opt.MapFrom(src => src.ValidationRegex))
                .ForMember(dest => dest.MinValue, opt => opt.MapFrom(src => src.MinValue))
                .ForMember(dest => dest.MaxValue, opt => opt.MapFrom(src => src.MaxValue))
                .ForMember(dest => dest.MinLength, opt => opt.MapFrom(src => src.MinLength))
                .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => src.MaxLength))
                .ForMember(dest => dest.ExpressionTemplate, opt => opt.MapFrom(src => src.ExpressionTemplate))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

            CreateMap<UpdateRuleEffectTypeDto, EffectTypeConfigurationEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EffectTypeId, opt => opt.Ignore()) // Don't change FK
                .ForMember(dest => dest.EffectType, opt => opt.Ignore())
                .ForMember(dest => dest.DataType, opt => opt.MapFrom(src => src.DataType ?? string.Empty))
                .ForMember(dest => dest.InputType, opt => opt.MapFrom(src => src.InputType ?? string.Empty))
                .ForMember(dest => dest.HasApiSource, opt => opt.MapFrom(src => src.HasApiSource ?? false))
                .ForMember(dest => dest.ApiEndpoint, opt => opt.MapFrom(src => src.ApiEndpoint))
                .ForMember(dest => dest.ApiMethod, opt => opt.MapFrom(src => src.ApiMethod))
                .ForMember(dest => dest.ApiParameters, opt => opt.MapFrom(src => src.ApiParameters))
                .ForMember(dest => dest.StaticApiEndpoint, opt => opt.MapFrom(src => src.StaticApiEndpoint))
                .ForMember(dest => dest.StaticApiInputType, opt => opt.MapFrom(src => src.StaticApiInputType))
                .ForMember(dest => dest.StaticApiMethod, opt => opt.MapFrom(src => src.StaticApiMethod))
                .ForMember(dest => dest.StaticApiParamter, opt => opt.MapFrom(src => src.StaticApiParamter))
                .ForMember(dest => dest.StaticApiResponseMapping, opt => opt.MapFrom(src => src.StaticApiResponseMapping))
                .ForMember(dest => dest.HasStaticValues, opt => opt.MapFrom(src => src.HasStaticValues ?? false))
                .ForMember(dest => dest.StaticValuesJson, opt => opt.MapFrom(src => src.StaticValuesJson))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired ?? false))
                .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.DefaultValue))
                .ForMember(dest => dest.ValidationRegex, opt => opt.MapFrom(src => src.ValidationRegex))
                .ForMember(dest => dest.MinValue, opt => opt.MapFrom(src => src.MinValue))
                .ForMember(dest => dest.MaxValue, opt => opt.MapFrom(src => src.MaxValue))
                .ForMember(dest => dest.MinLength, opt => opt.MapFrom(src => src.MinLength))
                .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => src.MaxLength))
                .ForMember(dest => dest.ExpressionTemplate, opt => opt.MapFrom(src => src.ExpressionTemplate))
                .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Keep existing value
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
        }
    }
}
