using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;

 public class RTSFieldDefinitionDto : BaseDtos
 {
    public int DepartmentId { get; set; }
    public int ServiceId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
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

    [Required(ErrorMessage = "RTSFieldDefinition_FieldName_Required")]
    [StringLength(100, ErrorMessage = "RTSFieldDefinition_FieldName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldName_Invalid")]
    public string FieldName { get; set; } = string.Empty;

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

    [StringLength(1000, ErrorMessage = "RTSFieldDefinition_OptionsJson_MaxLengthExceeded_1000")]
    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_DisplayOrder_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_DisplayOrder_InvalidRange")]
    public int DisplayOrder { get; set; }

    [StringLength(200, ErrorMessage = "RTSFieldDefinition_ValidationRules_MaxLengthExceeded_200")]
    public string? ValidationRules { get; set; }

    [StringLength(200, ErrorMessage = "RTSFieldDefinition_DefaultValue_MaxLengthExceeded_200")]
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
    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_DepartmentId_InvalidRange")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_ServiceId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RTSFieldDefinition_ServiceId_InvalidRange")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "RTSFieldDefinition_FieldCode_Required")]
    [StringLength(50, ErrorMessage = "RTSFieldDefinition_FieldCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldCode_Invalid")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RTSFieldDefinition_FieldName_Required")]
    [StringLength(100, ErrorMessage = "RTSFieldDefinition_FieldName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "RTSFieldDefinition_FieldName_Invalid")]
    public string FieldName { get; set; } = string.Empty;

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

