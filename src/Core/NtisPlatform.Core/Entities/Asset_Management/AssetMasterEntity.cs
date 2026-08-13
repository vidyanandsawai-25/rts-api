using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Asset Master entity for the Asset Management System.
/// Mirrors AMS.AssetMaster: only identification, category, hierarchy, owning department,
/// ownership/occupancy and condition live here. All location, plot and KYC
/// detail now lives on AMS.AssetDetails (1:1, keyed by AssetId).
/// </summary>
public class AssetMasterEntity : BaseEntity, IHardDeletable
{
    // Identification / Category
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? AssetRegionalName { get; set; }
    public int AssetCategoryId { get; set; }
    public int AssetTypeId { get; set; }
    public int? ParentAssetId { get; set; }

    // Hierarchy
    public int HierarchyLevel { get; set; }
    public string? HierarchyPath { get; set; }

    // Owning department (AMS.OwningDepartmentMaster)
    public int? DepartmentId { get; set; }

    // Legal / Acquisition
    public string? OwnershipType { get; set; }
    public string? OccupancyStatus { get; set; }

    // Condition
    public int? AssetConditionId { get; set; }

    // Soft delete properties
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // ---------------------------------------------------------------------
    // Compatibility shims: these columns were dropped from AMS.AssetMaster
    // (moved to AMS.AssetDetails, or removed entirely). They are kept so
    // legacy code that still references them compiles, but are excluded from
    // the EF model via Fluent Ignore() in ApplicationDbContext (Core entities
    // stay pure POCOs — no DataAnnotations.Schema). Never persisted or read
    // from AssetMaster; new code must not use them in EF queries.
    // ---------------------------------------------------------------------
    public int AssetLocationDetailsId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? UpicId { get; set; }
    public string? PlotNo { get; set; }
    public decimal? PurchaseValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int? DepreciationId { get; set; }
    public int? InventoryBatchId { get; set; }

    // Navigation properties
    public AssetCategoryEntity? AssetCategory { get; set; }
    public AssetTypeEntity? AssetType { get; set; }
    public AssetMasterEntity? ParentAsset { get; set; }
    public AssetDetailsEntity? Details { get; set; }
    public ICollection<AssetFieldValueEntity>? FieldValues { get; set; }

    // Excluded from the EF model via Fluent Ignore() (see ApplicationDbContext).
    public InventoryBatchEntity? InventoryBatch { get; set; }
    public ICollection<SubUnitsDetailsEntity>? SubUnitsDetails { get; set; }
}
