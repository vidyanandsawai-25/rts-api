using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateFieldConfigDto : BaseDtos
{
    public int BulkUpdateMasterId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DisplayNameMarathi { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }
    public string? ValidationRegex { get; set; }
    public string? DefaultValue { get; set; }
    public int SequenceNo { get; set; }
    public bool IsReadonly { get; set; }
    public string? BindApi { get; set; }
}

public class CreateBulkUpdateFieldConfigDto : CreateBaseDtos
{
    [Required(ErrorMessage = "BulkUpdateFieldConfig_BulkUpdateMasterId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "BulkUpdateFieldConfig_BulkUpdateMasterId_Range")]
    public int BulkUpdateMasterId { get; set; }
   

    [Required(ErrorMessage = "BulkUpdateFieldConfig_FieldName_Required")]
    [StringLength(100, ErrorMessage = "BulkUpdateFieldConfig_FieldName_MaxLen_100")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateFieldConfig_DisplayName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateFieldConfig_DisplayName_MaxLen_200")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "BulkUpdateFieldConfig_DisplayNameMarathi_MaxLen_200")]
    public string DisplayNameMarathi { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateFieldConfig_ControlType_Required")]
    [StringLength(50, ErrorMessage = "BulkUpdateFieldConfig_ControlType_MaxLen_50")]
    public string ControlType { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateFieldConfig_DataType_Required")]
    [StringLength(50, ErrorMessage = "BulkUpdateFieldConfig_DataType_MaxLen_50")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_Placeholder_MaxLen_500")]
    public string? Placeholder { get; set; }

    public bool IsRequired { get; set; }

    public int? MaxLength { get; set; }

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_ValidationRegex_MaxLen_500")]
    public string? ValidationRegex { get; set; }

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }

    [Range(1, 9999, ErrorMessage = "BulkUpdateFieldConfig_SequenceNo_Range")]
    public int SequenceNo { get; set; }

    public bool IsReadonly { get; set; }

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_BindApi_MaxLen_500")]
    public string? BindApi { get; set; }
}

public class UpdateBulkUpdateFieldConfigDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "BulkUpdateFieldConfig_BulkUpdateMasterId_Required")]
    public int BulkUpdateMasterId { get; set; }

    [Required(ErrorMessage = "BulkUpdateFieldConfig_FieldName_Required")]
    [StringLength(100, ErrorMessage = "BulkUpdateFieldConfig_FieldName_MaxLen_100")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateFieldConfig_DisplayName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateFieldConfig_DisplayName_MaxLen_200")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "BulkUpdateFieldConfig_DisplayNameMarathi_MaxLen_200")]
    public string DisplayNameMarathi { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateFieldConfig_ControlType_Required")]
    [StringLength(50, ErrorMessage = "BulkUpdateFieldConfig_ControlType_MaxLen_50")]
    public string ControlType { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateFieldConfig_DataType_Required")]
    [StringLength(50, ErrorMessage = "BulkUpdateFieldConfig_DataType_MaxLen_50")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_Placeholder_MaxLen_500")]
    public string? Placeholder { get; set; }

    public bool IsRequired { get; set; }

    public int? MaxLength { get; set; }

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_ValidationRegex_MaxLen_500")]
    public string? ValidationRegex { get; set; }

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }

    [Range(1, 9999, ErrorMessage = "BulkUpdateFieldConfig_SequenceNo_Range")]
    public int SequenceNo { get; set; }

    public bool IsReadonly { get; set; }

    [StringLength(500, ErrorMessage = "BulkUpdateFieldConfig_BindApi_MaxLen_500")]
    public string? BindApi { get; set; }
}
