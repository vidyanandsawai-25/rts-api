using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class ScreenFormFieldMasterDto : BaseDtos
{
    public int ScreenId { get; set; }
    public int SectionId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? FieldLabelLocal { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public int DisplayOrder { get; set; }
    public int ColumnSpan { get; set; }
    public string? CssClass { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadonly { get; set; }
    public bool IsVisible { get; set; }
    public bool IsUnique { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? RegexPattern { get; set; }
    public string? ValidationMessage { get; set; }
    public int? DropdownSourceId { get; set; }
    public string? StaticOptionsJson { get; set; }
    public bool IsCascading { get; set; }
    public int? ParentFieldId { get; set; }
    public bool IsMultiSelect { get; set; }
    public string? VisibilityConditionJson { get; set; }
    public string? ValidationJson { get; set; }
    public string? ExtraConfigJson { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
}

public class CreateScreenFormFieldMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ScreenFormFieldMaster_ScreenId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormFieldMaster_ScreenId_Required")]
    public int ScreenId { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_SectionId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormFieldMaster_SectionId_Required")]
    public int SectionId { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_FieldName_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldName_MaxLen_200")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormFieldMaster_FieldLabel_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldLabel_MaxLen_200")]
    public string FieldLabel { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldLabelLocal_MaxLen_200")]
    public string? FieldLabelLocal { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_FieldCode_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldCode_MaxLen_200")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormFieldMaster_DataType_Required")]
    [StringLength(50, ErrorMessage = "ScreenFormFieldMaster_DataType_MaxLen_50")]
    public string DataType { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormFieldMaster_ControlType_Required")]
    [StringLength(50, ErrorMessage = "ScreenFormFieldMaster_ControlType_MaxLen_50")]
    public string ControlType { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "ScreenFormFieldMaster_Placeholder_MaxLen_300")]
    public string? Placeholder { get; set; }

    [StringLength(500, ErrorMessage = "ScreenFormFieldMaster_DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormFieldMaster_ColumnSpan_Range")]
    public int? ColumnSpan { get; set; } = 1;

    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_CssClass_MaxLen_200")]
    public string? CssClass { get; set; }
    public bool? IsRequired { get; set; } = false;
    public bool? IsReadonly { get; set; } = false;
    public bool? IsVisible { get; set; } = true;
    public bool? IsUnique { get; set; } = false;
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [StringLength(500, ErrorMessage = "ScreenFormFieldMaster_RegexPattern_MaxLen_500")]
    public string? RegexPattern { get; set; }

    [StringLength(500, ErrorMessage = "ScreenFormFieldMaster_ValidationMessage_MaxLen_500")]
    public string? ValidationMessage { get; set; }
    public int? DropdownSourceId { get; set; }
    public string? StaticOptionsJson { get; set; }
    public bool? IsCascading { get; set; } = false;
    public int? ParentFieldId { get; set; }
    public bool? IsMultiSelect { get; set; } = false;
    public string? VisibilityConditionJson { get; set; }
    public string? ValidationJson { get; set; }
    public string? ExtraConfigJson { get; set; }
    public bool? IsSearchable { get; set; } = false;
    public bool? IsFilterable { get; set; } = false;
}

public class UpdateScreenFormFieldMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ScreenFormFieldMaster_ScreenId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormFieldMaster_ScreenId_Required")]
    public int ScreenId { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_SectionId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormFieldMaster_SectionId_Required")]
    public int SectionId { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_FieldName_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldName_MaxLen_200")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormFieldMaster_FieldLabel_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldLabel_MaxLen_200")]
    public string FieldLabel { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldLabelLocal_MaxLen_200")]
    public string? FieldLabelLocal { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_FieldCode_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_FieldCode_MaxLen_200")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormFieldMaster_DataType_Required")]
    [StringLength(50, ErrorMessage = "ScreenFormFieldMaster_DataType_MaxLen_50")]
    public string DataType { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormFieldMaster_ControlType_Required")]
    [StringLength(50, ErrorMessage = "ScreenFormFieldMaster_ControlType_MaxLen_50")]
    public string ControlType { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "ScreenFormFieldMaster_Placeholder_MaxLen_300")]
    public string? Placeholder { get; set; }

    [StringLength(500, ErrorMessage = "ScreenFormFieldMaster_DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "ScreenFormFieldMaster_ColumnSpan_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormFieldMaster_ColumnSpan_Range")]
    public int? ColumnSpan { get; set; }

    [StringLength(200, ErrorMessage = "ScreenFormFieldMaster_CssClass_MaxLen_200")]
    public string? CssClass { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadonly { get; set; }
    public bool? IsVisible { get; set; }
    public bool IsUnique { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [StringLength(500, ErrorMessage = "ScreenFormFieldMaster_RegexPattern_MaxLen_500")]
    public string? RegexPattern { get; set; }

    [StringLength(500, ErrorMessage = "ScreenFormFieldMaster_ValidationMessage_MaxLen_500")]
    public string? ValidationMessage { get; set; }
    public int? DropdownSourceId { get; set; }
    public string? StaticOptionsJson { get; set; }
    public bool IsCascading { get; set; }
    public int? ParentFieldId { get; set; }
    public bool IsMultiSelect { get; set; }
    public string? VisibilityConditionJson { get; set; }
    public string? ValidationJson { get; set; }
    public string? ExtraConfigJson { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
}