using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class InventoryBatchEntity : BaseEntity, IHardDeletable
{
    public int ParentAssetId { get; set; }
    public int OwningDepartmentId { get; set; }
    // FK references to the inventory master tables (replaces the former denormalized
    // InventoryType / ItemName / ModelBrand / Condition / OwningDepartment string columns).
    public int InventoryItemCategoryId { get; set; }
    public int InventoryItemNameId { get; set; }
    public int InventoryItemModelId { get; set; }
    public int ConditionId { get; set; }
    public string? Specifications { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalBatchValue { get; set; }
    public decimal? TotalBatchCV { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    // Maps to schema columns InvoiceDocumentId / PhotoDocumentId (see ApplicationDbContext).
    public string? InvoiceFileName { get; set; }
    public string? PhotoFileName { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public AssetMasterEntity? ParentAsset { get; set; }
    public ICollection<InventoryAssetDetailEntity> Units { get; set; } = new List<InventoryAssetDetailEntity>();
}
