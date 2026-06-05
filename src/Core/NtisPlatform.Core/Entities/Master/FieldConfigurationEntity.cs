namespace NtisPlatform.Core.Entities.Master
{
    public class FieldConfigurationEntity : BaseEntity
    {
        public int RulesFieldId { get; set; }
        public string DataType { get; set; } = string.Empty;

        // Input Type Configuration
        public string InputType { get; set; } = string.Empty;

        // API Configuration (for dynamic dropdowns)
        public bool HasApiSource { get; set; } = false;
        public string? ApiEndpoint { get; set; }
        public string? ApiMethod { get; set; }
        public string? ApiParameters { get; set; }

        /// <summary>
        /// JSON configuration for mapping API response to field values
        /// Example: {"responsePath": "data", "valuePath": "id", "labelPath": "name", "displayTemplate": "{code} - {name}"}
        /// </summary>
        public string? ApiResponseMapping { get; set; }

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

        // Navigation property
        public virtual RulesFieldEntity? RulesField { get; set; }
    }
}
