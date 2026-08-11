namespace NtisPlatform.Core.Entities.Asset_Management;

public class InventoryAssetDetailEntity : BaseEntity
{
    public int AssetId { get; set; }
    public int BatchId { get; set; }
    public int UnitNumber { get; set; }
    // FK references to the inventory master tables (replaces the former denormalized
    // InventoryType / ItemName / ModelBrand / Condition / OwningDepartment string columns).
    public int? InventoryItemCategoryId { get; set; }
    public int? InventoryItemNameId { get; set; }
    public int? InventoryItemModelId { get; set; }
    public int? InventoryItemConditionId { get; set; }
    public int? OwningDepartmentId { get; set; }
    public string? Specifications { get; set; }
    public string? PhotoFileId { get; set; }
    public decimal UnitPurchaseValue { get; set; }
    public decimal? UnitCapitalValue { get; set; }

    // Navigation properties
    public AssetMasterEntity? AssetMaster { get; set; }
    public InventoryBatchEntity? Batch { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
