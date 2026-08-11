using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class RTSFieldDefinitionEntity : BaseEntity, IHardDeletable
{
    
    public int DepartmentId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>
    /// Unique identifier for the field (camelCase, e.g., "firstName", "mobileNumber").
    /// Doubles as both the code and name — FieldName column was removed as it was always identical to FieldCode.
    /// </summary>
    public string FieldCode { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? FieldLabelLocal { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public string? FieldGroup { get; set; }
    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? MaxLength { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
    public virtual List<RTSFieldValueEntity> FieldValues { get; set; } = new List<RTSFieldValueEntity>();
}