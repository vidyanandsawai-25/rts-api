using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.FieldConfiguration
{
    /// <summary>
    /// DTO for updating an existing field configuration
    /// </summary>
    public class UpdateFieldConfigurationDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "RulesFieldId is required")]
        public int RulesFieldId { get; set; }

        [Required(ErrorMessage = "DataType is required")]
        [StringLength(50, ErrorMessage = "DataType cannot exceed 50 characters")]
        public string DataType { get; set; } = string.Empty;

        [Required(ErrorMessage = "InputType is required")]
        [StringLength(50, ErrorMessage = "InputType cannot exceed 50 characters")]
        public string InputType { get; set; } = string.Empty;

        // API Source Configuration
        public bool HasApiSource { get; set; } = false;

        [StringLength(500, ErrorMessage = "ApiEndpoint cannot exceed 500 characters")]
        public string? ApiEndpoint { get; set; }

        [StringLength(10, ErrorMessage = "ApiMethod cannot exceed 10 characters")]
        public string? ApiMethod { get; set; }

        public string? ApiParameters { get; set; }

        public string? ApiResponseMapping { get; set; }

        // Static Values Configuration
        public bool HasStaticValues { get; set; } = false;

        public string? StaticValuesJson { get; set; }

        // Validation Configuration
        public bool IsRequired { get; set; } = false;

        [StringLength(255, ErrorMessage = "DefaultValue cannot exceed 255 characters")]
        public string? DefaultValue { get; set; }

        [StringLength(500, ErrorMessage = "ValidationRegex cannot exceed 500 characters")]
        public string? ValidationRegex { get; set; }

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }

        public int? MinLength { get; set; }

        public int? MaxLength { get; set; }
    }
}
