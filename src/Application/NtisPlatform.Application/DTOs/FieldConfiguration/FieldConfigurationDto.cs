namespace NtisPlatform.Application.DTOs.FieldConfiguration
{
    /// <summary>
    /// DTO for retrieving field configuration
    /// </summary>
    public class FieldConfigurationDto : BaseDtos
    {
        public int RulesFieldId { get; set; }
        public string DataType { get; set; } = string.Empty;
        public string InputType { get; set; } = string.Empty;

        // API Source Configuration
        public bool HasApiSource { get; set; }
        public string? ApiEndpoint { get; set; }
        public string? ApiMethod { get; set; }
        public string? ApiParameters { get; set; }
        public string? ApiResponseMapping { get; set; }

        // Static Values Configuration
        public bool HasStaticValues { get; set; }
        public string? StaticValuesJson { get; set; }

        // Validation Configuration
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? ValidationRegex { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }

        // Navigation
        public string? FieldName { get; set; }
    }
}
