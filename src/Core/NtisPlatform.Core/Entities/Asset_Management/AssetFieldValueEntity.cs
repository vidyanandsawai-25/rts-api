namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetFieldValueEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to AssetMaster.
    /// </summary>
    public int AssetId { get; set; }
    public int? FieldDefinitionId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    // Schema collapses the former typed columns (TextValue/NumberValue/DateValue/BooleanValue)
    // into a single string column [FieldValue].
    public string? FieldValue { get; set; }

    // Soft delete fields
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }

    public AssetMasterEntity Asset { get; set; } = null!;
}
