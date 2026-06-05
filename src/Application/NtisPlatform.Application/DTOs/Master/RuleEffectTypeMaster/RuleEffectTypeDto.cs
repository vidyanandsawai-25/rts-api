using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster
{
    public class RuleEffectTypeDto : BaseDtos
    {
        public string? EffectType { get; set; }
        public string DataType { get; set; } = string.Empty;

        // Input Type Configuration
        public string InputType { get; set; } = string.Empty;

        // API Configuration (for dynamic dropdowns)
        public bool HasApiSource { get; set; } = false;
        public string? ApiEndpoint { get; set; }
        public string? ApiMethod { get; set; }
        public string? ApiParameters { get; set; }
        // Static API Configuration (global/default API settings)
        public string? StaticApiEndpoint { get; set; }
        public string? StaticApiInputType { get; set; }
        public string? StaticApiMethod { get; set; }
        public string? StaticApiParamter { get; set; }
        public string? StaticApiResponseMapping { get; set; }
        // Static Value Configuration (for non-API dropdowns)
        public bool HasStaticValues { get; set; } = false;
        public string? StaticValuesJson { get; set; }

        // Validation & Default Configuration
        public bool IsRequired { get; set; } = false;
        public string? DefaultValue { get; set; }
        public string? ValidationRegex { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string? ExpressionTemplate { get; set; }
    }

    public class CreateRuleEffectTypeDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "EffectType_Required")]
        [StringLength(100, ErrorMessage = "EffectType_MaxLen_100")]
        public string EffectType { get; set; } = string.Empty;

        // Configuration fields (optional - if provided, creates EffectTypeConfiguration)
        [StringLength(50, ErrorMessage = "DataType_MaxLen_50")]
        public string? DataType { get; set; }

        [StringLength(50, ErrorMessage = "InputType_MaxLen_50")]
        public string? InputType { get; set; }

        // API Configuration
        public bool? HasApiSource { get; set; }

        [StringLength(500, ErrorMessage = "ApiEndpoint_MaxLen_500")]
        public string? ApiEndpoint { get; set; }

        [StringLength(10, ErrorMessage = "ApiMethod_MaxLen_10")]
        public string? ApiMethod { get; set; }

        public string? ApiParameters { get; set; }

        // Static API Configuration
        [StringLength(500, ErrorMessage = "StaticApiEndpoint_MaxLen_500")]
        public string? StaticApiEndpoint { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiInputType_MaxLen_500")]
        public string? StaticApiInputType { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiMethod_MaxLen_500")]
        public string? StaticApiMethod { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiParamter_MaxLen_500")]
        public string? StaticApiParamter { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiResponseMapping_MaxLen_500")]
        public string? StaticApiResponseMapping { get; set; }

        // Static Value Configuration
        public bool? HasStaticValues { get; set; }
        public string? StaticValuesJson { get; set; }

        // Validation & Default Configuration
        public bool? IsRequired { get; set; }

        [StringLength(255, ErrorMessage = "DefaultValue_MaxLen_255")]
        public string? DefaultValue { get; set; }

        [StringLength(500, ErrorMessage = "ValidationRegex_MaxLen_500")]
        public string? ValidationRegex { get; set; }

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }

        [StringLength(500, ErrorMessage = "ExpressionTemplate_MaxLen_500")]
        public string? ExpressionTemplate { get; set; }
    }

    public class UpdateRuleEffectTypeDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "EffectType_Required")]
        [StringLength(100, ErrorMessage = "EffectType_MaxLen_100")]
        public string EffectType { get; set; } = string.Empty;

        // Configuration fields (optional - if provided, creates/updates EffectTypeConfiguration)
        [StringLength(50, ErrorMessage = "DataType_MaxLen_50")]
        public string? DataType { get; set; }

        [StringLength(50, ErrorMessage = "InputType_MaxLen_50")]
        public string? InputType { get; set; }

        // API Configuration
        public bool? HasApiSource { get; set; }

        [StringLength(500, ErrorMessage = "ApiEndpoint_MaxLen_500")]
        public string? ApiEndpoint { get; set; }

        [StringLength(10, ErrorMessage = "ApiMethod_MaxLen_10")]
        public string? ApiMethod { get; set; }

        public string? ApiParameters { get; set; }

        // Static API Configuration
        [StringLength(500, ErrorMessage = "StaticApiEndpoint_MaxLen_500")]
        public string? StaticApiEndpoint { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiInputType_MaxLen_500")]
        public string? StaticApiInputType { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiMethod_MaxLen_500")]
        public string? StaticApiMethod { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiParamter_MaxLen_500")]
        public string? StaticApiParamter { get; set; }

        [StringLength(500, ErrorMessage = "StaticApiResponseMapping_MaxLen_500")]
        public string? StaticApiResponseMapping { get; set; }

        // Static Value Configuration
        public bool? HasStaticValues { get; set; }
        public string? StaticValuesJson { get; set; }

        // Validation & Default Configuration
        public bool? IsRequired { get; set; }

        [StringLength(255, ErrorMessage = "DefaultValue_MaxLen_255")]
        public string? DefaultValue { get; set; }

        [StringLength(500, ErrorMessage = "ValidationRegex_MaxLen_500")]
        public string? ValidationRegex { get; set; }

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }

        [StringLength(500, ErrorMessage = "ExpressionTemplate_MaxLen_500")]
        public string? ExpressionTemplate { get; set; }
    }

}