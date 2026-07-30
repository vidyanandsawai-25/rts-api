using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Asset document business table (AMS.AssetDocument).
/// Stores document records for assets with a link to core document storage binding.
/// Supports versioning via IsLatest (1 = current, 0 = superseded).
/// </summary>
public class AssetDocumentEntity : BaseEntity, IHardDeletable
{
    protected AssetDocumentEntity() { }

    internal AssetDocumentEntity(
        int assetId,
        int documentDefinitionId,
        int? documentBindingId = null,
        bool isLatest = true,
        int? displayOrder = null,
        string? remarks = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null)
    {
        AssetId = assetId;
        DocumentDefinitionId = documentDefinitionId;
        DocumentBindingId = documentBindingId;
        IsLatest = isLatest;
        DisplayOrder = displayOrder;
        Remarks = remarks;
        _markedForDeletion = markedForDeletion;
        _markedForDeletionDate = markedForDeletionDate;
    }

    public static AssetDocumentEntity Create(
        int assetId,
        int documentDefinitionId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(assetId, documentDefinitionId);
        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new AssetDocumentEntity
        {
            AssetId = assetId,
            DocumentDefinitionId = documentDefinitionId,
            DocumentBindingId = null,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    public static AssetDocumentEntity CreateWithDocument(
        int assetId,
        int documentDefinitionId,
        int documentBindingId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(assetId, documentDefinitionId);

        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new AssetDocumentEntity
        {
            AssetId = assetId,
            DocumentDefinitionId = documentDefinitionId,
            DocumentBindingId = documentBindingId,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    public int AssetId { get; private set; }
    public int DocumentDefinitionId { get; private set; }
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

    public AssetDocumentDefinitionEntity? DocumentDefinition { get; private set; }
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
            throw new InvalidOperationException("Document record is already marked for deletion.");

        _markedForDeletion = true;
        _markedForDeletionDate = DateTime.Now;
        IsActive = false;
    }

    public void RestoreFromDeletion()
    {
        if (!_markedForDeletion)
            throw new InvalidOperationException("Document record is not marked for deletion.");

        _markedForDeletion = false;
        _markedForDeletionDate = null;
        IsActive = true;
    }

    public bool HasDocument()
    {
        return DocumentBindingId.HasValue && DocumentBindingId.Value > 0;
    }

    private static void ValidateRequiredIds(int assetId, int documentDefinitionId)
    {
        if (assetId <= 0)
            throw new ArgumentException("Asset ID must be greater than zero.", nameof(assetId));

        if (documentDefinitionId <= 0)
            throw new ArgumentException("Document definition ID must be greater than zero.", nameof(documentDefinitionId));
    }

    private static void ValidateRemarks(string? remarks)
    {
        if (!string.IsNullOrWhiteSpace(remarks) && remarks.Length > 500)
            throw new ArgumentException("Remarks cannot exceed 500 characters.", nameof(remarks));
    }
}
