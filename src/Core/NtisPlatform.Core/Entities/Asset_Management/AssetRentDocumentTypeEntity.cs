using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Represents asset rent document type master data in the AMS system
/// </summary>
public class AssetRentDocumentTypeEntity : BaseEntity, IHardDeletable
{
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsRequired { get; set; } = false;

    // IHardDeletable properties
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
}
