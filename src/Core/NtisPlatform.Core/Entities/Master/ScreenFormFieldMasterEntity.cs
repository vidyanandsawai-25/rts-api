namespace NtisPlatform.Core.Entities.Master;

public class ScreenFormFieldMasterEntity : BaseEntity
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