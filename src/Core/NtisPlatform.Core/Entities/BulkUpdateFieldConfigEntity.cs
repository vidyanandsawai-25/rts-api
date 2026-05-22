namespace NtisPlatform.Core.Entities;

public class BulkUpdateFieldConfigEntity : BaseEntity
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
    public BulkUpdateMasterEntity? Master { get; set; }
}
