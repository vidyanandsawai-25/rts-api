using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Asset photo business table (AMS.AssetPhoto).
/// Stores photo records for assets with a link to document storage.
/// Supports versioning via IsLatest (1 = current, 0 = superseded).
/// </summary>
public class AssetPhotoEntity : BaseEntity, IHardDeletable
{
    protected AssetPhotoEntity() { }

    internal AssetPhotoEntity(
        int assetId,
        int photoTypeId,
        int? documentBindingId = null,
        bool isLatest = true,
        int? displayOrder = null,
        string? remarks = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null,
        int? subUnitDetailsId = null)
    {
        AssetId = assetId;
        PhotoTypeId = photoTypeId;
        DocumentBindingId = documentBindingId;
        SubUnitsDetailsId = subUnitDetailsId;
        IsLatest = isLatest;
        DisplayOrder = displayOrder;
        Remarks = remarks;
        _markedForDeletion = markedForDeletion;
        _markedForDeletionDate = markedForDeletionDate;
    }

    public static AssetPhotoEntity Create(
        int assetId,
        int photoTypeId,
        int? subUnitDetailsId = null,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(assetId, photoTypeId);
        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new AssetPhotoEntity
        {
            AssetId = assetId,
            PhotoTypeId = photoTypeId,
            SubUnitsDetailsId = subUnitDetailsId,
            DocumentBindingId = null,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    public static AssetPhotoEntity CreateWithDocument(
        int assetId,
        int photoTypeId,
        int documentBindingId,
        int? subUnitDetailsId = null,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(assetId, photoTypeId);

        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new AssetPhotoEntity
        {
            AssetId = assetId,
            PhotoTypeId = photoTypeId,
            SubUnitsDetailsId = subUnitDetailsId,
            DocumentBindingId = documentBindingId,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    public int AssetId { get; private set; }

    /// <summary>
    /// Optional FK to AMS.SubUnitsDetails.
    /// When set, this photo belongs to a sub-unit row; when null it belongs directly to AssetMaster.
    /// </summary>
    public int? SubUnitsDetailsId { get; private set; }

    public int PhotoTypeId { get; private set; }
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

    public AssetPhotoTypeEntity? PhotoType { get; private set; }
    public DocumentBindingEntity? DocumentBinding { get; private set; }

    public void LinkDocumentBinding(int documentBindingId)
    {
        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        if (_markedForDeletion)
            throw new InvalidOperationException("Cannot link document to a photo marked for deletion.");

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
            throw new InvalidOperationException("Photo is already marked for deletion.");

        _markedForDeletion = true;
        _markedForDeletionDate = DateTime.Now;
        IsActive = false;
    }

    public void RestoreFromDeletion()
    {
        if (!_markedForDeletion)
            throw new InvalidOperationException("Photo is not marked for deletion.");

        _markedForDeletion = false;
        _markedForDeletionDate = null;
        IsActive = true;
    }

    public bool HasDocument()
    {
        return DocumentBindingId.HasValue && DocumentBindingId.Value > 0;
    }

    private static void ValidateRequiredIds(int assetId, int photoTypeId)
    {
        if (assetId <= 0)
            throw new ArgumentException("Asset ID must be greater than zero.", nameof(assetId));

        if (photoTypeId <= 0)
            throw new ArgumentException("Photo type ID must be greater than zero.", nameof(photoTypeId));
    }

    private static void ValidateRemarks(string? remarks)
    {
        if (!string.IsNullOrWhiteSpace(remarks) && remarks.Length > 500)
            throw new ArgumentException("Remarks cannot exceed 500 characters.", nameof(remarks));
    }
}
