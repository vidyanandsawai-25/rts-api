using NtisPlatform.Application.DTOs.FieldConfiguration;

namespace NtisPlatform.Application.DTOs.RuleEngine
{
    public class CreateRuleFieldsDto : CreateBaseDtos
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? RuleScopeId { get; set; }

        /// <summary>
        /// Nested field configuration (optional). If provided, overrides individual configuration properties.
        /// </summary>
        public CreateFieldConfigurationDto? FieldConfiguration { get; set; }

        // FieldConfiguration properties (nullable - optional)
        public string? DataType { get; set; }
        public string? InputType { get; set; }
        public bool? HasApiSource { get; set; }
        public string? ApiEndpoint { get; set; }
        public string? ApiMethod { get; set; }
        public string? ApiParameters { get; set; }
        public string? ApiResponseMapping { get; set; }
        public bool? HasStaticValues { get; set; }
        public string? StaticValuesJson { get; set; }
        public bool? IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? ValidationRegex { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
    }
}
