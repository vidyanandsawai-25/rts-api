using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;

public class RTSFieldDefinitionDto : BaseDtos
{
    public int DepartmentId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>
    /// Unique field identifier (camelCase). FieldName was removed — FieldCode serves both purposes.
    /// </summary>
    public string FieldCode { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? FieldLabelLocal { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public string? FieldGroup { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? MaxLength { get; set; }
    public string? OptionsJson { get; set; }
}

public class CreateRTSFieldDefinitionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RTSFieldDefinition_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_DepartmentId_InvalidRange")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_ServiceId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_ServiceId_InvalidRange")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_FieldCode_Required")]
    [StringLength(50, ErrorMessage = "RTSFieldDefinition_FieldCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldCode_Invalid")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RTSFieldDefinition_FieldLabel_Required")]
    [StringLength(200, ErrorMessage = "RTSFieldDefinition_FieldLabel_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldLabel_Invalid")]
    public string FieldLabel { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "RTSFieldDefinition_FieldLabelLocal_MaxLengthExceeded_200")]
    public string? FieldLabelLocal { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_FieldType_Required")]
    [StringLength(50, ErrorMessage = "RTSFieldDefinition_FieldType_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldType_Invalid")]
    public string FieldType { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "RTSFieldDefinition_FieldGroup_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldGroup_Invalid")]
    public string? FieldGroup { get; set; }
    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_DisplayOrder_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_DisplayOrder_InvalidRange")]
    public int DisplayOrder { get; set; }
    public string? ValidationRules { get; set; }

    [StringLength(500, ErrorMessage = "RTSFieldDefinition_DefaultValue_MaxLengthExceeded_500")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_DefaultValue_Invalid")]
    public string? DefaultValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_MaxLength_InvalidRange")]
    public int? MaxLength { get; set; }
}

public class UpdateRTSFieldDefinitionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RTSFieldDefinition_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_DepartmentId_InvalidRange")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_ServiceId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_ServiceId_InvalidRange")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_FieldCode_Required")]
    [StringLength(50, ErrorMessage = "RTSFieldDefinition_FieldCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldCode_Invalid")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RTSFieldDefinition_FieldLabel_Required")]
    [StringLength(200, ErrorMessage = "RTSFieldDefinition_FieldLabel_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldLabel_Invalid")]
    public string FieldLabel { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "RTSFieldDefinition_FieldLabelLocal_MaxLengthExceeded_200")]
    public string? FieldLabelLocal { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_FieldType_Required")]
    [StringLength(50, ErrorMessage = "RTSFieldDefinition_FieldType_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldType_Invalid")]
    public string FieldType { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "RTSFieldDefinition_FieldGroup_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldGroup_Invalid")]
    public string? FieldGroup { get; set; }
    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_DisplayOrder_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_DisplayOrder_InvalidRange")]
    public int DisplayOrder { get; set; }
    public string? ValidationRules { get; set; }

    [StringLength(500, ErrorMessage = "RTSFieldDefinition_DefaultValue_MaxLengthExceeded_500")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_DefaultValue_Invalid")]
    public string? DefaultValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_MaxLength_InvalidRange")]
    public int? MaxLength { get; set; }
}

/// <summary>
/// Query options for the field-configuration service summary.  A card is
/// returned only when the service has at least one configured field.
/// </summary>
public class RTSFieldDefinitionServiceCardQueryParameters : BaseQueryParameters
{
    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ServiceId { get; set; }

    public bool? IsActive { get; set; }
}

public class RTSFieldDefinitionServiceCardDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int FieldCount { get; set; }
    public int ActiveFieldCount { get; set; }
    public int SectionCount { get; set; }
}

/// <summary>
/// Complete, ordered editor snapshot for one RTS service. Sections are virtual
/// and are materialized on the field rows through FieldGroup.
/// </summary>
public class UpdateRTSFieldDefinitionFormDto
{
    public int? UpdatedBy { get; set; }

    [Required]
    public List<RTSFieldDefinitionFormSectionDto> Sections { get; set; } = [];
}

public class RTSFieldDefinitionFormSectionDto
{
    [Required]
    [StringLength(100)]
    // Section names are citizen-facing headings. Allow the common punctuation
    // used in government forms (for example "Survey No./CTS No." and
    // "Address (Current)") while retaining an explicit safe allowlist.
    [RegularExpression(@"^[\p{L}\p{N}\s.,&/()_:'?+\-–\u0900-\u097F\u0980-\u09FF]*$")]
    public string Name { get; set; } = string.Empty;

    [MinLength(1)]
    public List<RTSFieldDefinitionFormFieldDto> Fields { get; set; } = [];
}

public class RTSFieldDefinitionFormFieldDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[^@#]*$")]
    public string FieldCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [RegularExpression(@"^[^@#]*$")]
    public string FieldLabel { get; set; } = string.Empty;

    [StringLength(200)]
    public string? FieldLabelLocal { get; set; }

    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[^@#]*$")]
    public string FieldType { get; set; } = string.Empty;

    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }

    [Range(1, int.MaxValue)]
    public int DisplayOrder { get; set; }

    public string? ValidationRules { get; set; }

    [StringLength(500)]
    [RegularExpression(@"^[^@#]*$")]
    public string? DefaultValue { get; set; }

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxLength { get; set; }

    public bool IsActive { get; set; } = true;
}
