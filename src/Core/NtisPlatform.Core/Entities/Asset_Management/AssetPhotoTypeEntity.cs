using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Asset photo type master data in the AMS system (AMS.AssetPhotoType).
/// </summary>
public class AssetPhotoTypeEntity : BaseEntity, IHardDeletable
{
    public string PhotoTypeCode { get; set; } = string.Empty;
    public string PhotoTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsSubUnit { get; set; } = false;

    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public AssetCategoryEntity? AssetCategory { get; set; }
    public AssetTypeEntity? AssetType { get; set; }
}
