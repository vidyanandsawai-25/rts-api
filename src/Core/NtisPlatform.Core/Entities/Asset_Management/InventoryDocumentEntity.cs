using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Inventory document business table (AMS.InventoryDocument).
/// </summary>
public class InventoryDocumentEntity : BaseEntity, IHardDeletable
{
    protected InventoryDocumentEntity() { }

    internal InventoryDocumentEntity(
        int inventoryBatchId,
        int documentTypeId,
        int? documentBindingId = null,
        bool isLatest = true,
        int? displayOrder = null,
        string? remarks = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null)
    {
        InventoryBatchId = inventoryBatchId;
        DocumentTypeId = documentTypeId;
        DocumentBindingId = documentBindingId;
        IsLatest = isLatest;
        DisplayOrder = displayOrder;
        Remarks = remarks;
        _markedForDeletion = markedForDeletion;
        _markedForDeletionDate = markedForDeletionDate;
    }

    public static InventoryDocumentEntity Create(
        int inventoryBatchId,
        int documentTypeId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(inventoryBatchId, documentTypeId);
        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new InventoryDocumentEntity
        {
            InventoryBatchId = inventoryBatchId,
            DocumentTypeId = documentTypeId,
            DocumentBindingId = null,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    public static InventoryDocumentEntity CreateWithDocument(
        int inventoryBatchId,
        int documentTypeId,
        int documentBindingId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(inventoryBatchId, documentTypeId);

        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new InventoryDocumentEntity
        {
            InventoryBatchId = inventoryBatchId,
            DocumentTypeId = documentTypeId,
            DocumentBindingId = documentBindingId,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    public int InventoryBatchId { get; private set; }
    public int DocumentTypeId { get; private set; }
    public int? DocumentBindingId { get; private set; }
    public bool IsLatest { get; private set; } = true;
    public int? DisplayOrder { get; private set; }
    public string? Remarks { get; private set; }

    private bool _markedForDeletion = false;
    private DateTime? _markedForDeletionDate;

    public bool MarkedForDeletion => _markedForDeletion;
    public DateTime? MarkedForDeletionDate => _markedForDeletionDate;

    bool IHardDeletable.MarkedForDeletion
    {
        get => _markedForDeletion;
        set => _markedForDeletion = value;
    }

    DateTime? IHardDeletable.MarkedForDeletionDate
    {
        get => _markedForDeletionDate;
        set => _markedForDeletionDate = value;
    }

    public InventoryDocumentTypeEntity? DocumentType { get; private set; }
    public DocumentBindingEntity? DocumentBinding { get; private set; }

    public void LinkDocumentBinding(int documentBindingId)
    {
        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        if (_markedForDeletion)
            throw new InvalidOperationException("Cannot link document to a record marked for deletion.");

        DocumentBindingId = documentBindingId;
    }

    public void UnlinkDocumentBinding()
    {
        DocumentBindingId = null;
    }

    public void SetDisplayOrder(int? displayOrder)
    {
        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        DisplayOrder = displayOrder;
    }

    public void SetRemarks(string? remarks)
    {
        ValidateRemarks(remarks);
        Remarks = remarks;
    }

    public void MarkAsSuperseded()
    {
        IsLatest = false;
    }

    public void MarkForDeletion()
    {
        if (_markedForDeletion)
            throw new InvalidOperationException("Record is already marked for deletion.");

        _markedForDeletion = true;
        _markedForDeletionDate = DateTime.Now;
        IsActive = false;
        IsLatest = false;
    }

    public void RestoreFromDeletion()
    {
        if (!_markedForDeletion)
            throw new InvalidOperationException("Record is not marked for deletion.");

        _markedForDeletion = false;
        _markedForDeletionDate = null;
        IsActive = true;
    }

    public bool HasDocument()
    {
        return DocumentBindingId.HasValue && DocumentBindingId.Value > 0;
    }

    private static void ValidateRequiredIds(int inventoryBatchId, int documentTypeId)
    {
        if (inventoryBatchId <= 0)
            throw new ArgumentException("Inventory batch ID must be greater than zero.", nameof(inventoryBatchId));

        if (documentTypeId <= 0)
            throw new ArgumentException("Document type ID must be greater than zero.", nameof(documentTypeId));
    }

    private static void ValidateRemarks(string? remarks)
    {
        if (!string.IsNullOrWhiteSpace(remarks) && remarks.Length > 500)
            throw new ArgumentException("Remarks cannot exceed 500 characters.", nameof(remarks));
    }
}
