using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Property photo business table (PTIS.PropertyPhoto).
/// Stores photo records for properties with a link to document storage.
/// Supports versioning via <see cref="IsLatest"/> (1 = current, 0 = superseded) so a photo
/// can be replaced while retaining prior versions for audit. Multiple current photos may exist
/// per (PropertyId, PhotoTypeId) (e.g., several gallery images for the same slot/type).
/// Rich domain model with validation and business logic.
/// </summary>
public class PropertyPhotoEntity : BaseEntity, IHardDeletable
{
    /// <summary>
    /// Protected constructor for EF Core
    /// </summary>
    protected PropertyPhotoEntity() { }

    /// <summary>
    /// Internal constructor for testing purposes only - provides full control over entity state
    /// </summary>
    internal PropertyPhotoEntity(
        int propertyId,
        int photoTypeId,
        int? documentBindingId = null,
        bool isLatest = true,
        int? displayOrder = null,
        string? remarks = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null)
    {
        PropertyId = propertyId;
        PhotoTypeId = photoTypeId;
        DocumentBindingId = documentBindingId;
        IsLatest = isLatest;
        DisplayOrder = displayOrder;
        Remarks = remarks;
        _markedForDeletion = markedForDeletion;
        _markedForDeletionDate = markedForDeletionDate;
    }

    /// <summary>
    /// Factory method to create a new property photo without document binding.
    /// Use this when you need to create the photo row before the DocumentBinding exists.
    /// </summary>
    public static PropertyPhotoEntity Create(
        int propertyId,
        int photoTypeId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(propertyId, photoTypeId);
        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new PropertyPhotoEntity
        {
            PropertyId = propertyId,
            PhotoTypeId = photoTypeId,
            DocumentBindingId = null,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    /// <summary>
    /// Factory method to create a new property photo with document binding.
    /// Optimized to eliminate the need for a separate update operation.
    /// </summary>
    public static PropertyPhotoEntity CreateWithDocument(
        int propertyId,
        int photoTypeId,
        int documentBindingId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(propertyId, photoTypeId);

        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new PropertyPhotoEntity
        {
            PropertyId = propertyId,
            PhotoTypeId = photoTypeId,
            DocumentBindingId = documentBindingId,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    /// <summary>
    /// Property ID this photo belongs to (FK to PTIS.PropertyMast)
    /// </summary>
    public int PropertyId { get; private set; }

    /// <summary>
    /// FK to PTIS.PropertyPhotoType - the slot/category this photo fills (e.g. Front Elevation)
    /// </summary>
    public int PhotoTypeId { get; private set; }

    /// <summary>
    /// FK to CORE.DocumentBinding - links to the uploaded image document
    /// </summary>
    public int? DocumentBindingId { get; private set; }

    /// <summary>
    /// 1 = current photo, 0 = superseded by a newer version (kept for audit history)
    /// </summary>
    public bool IsLatest { get; private set; } = true;

    /// <summary>
    /// Gallery sort order within (PropertyId, PhotoTypeId)
    /// </summary>
    public int? DisplayOrder { get; private set; }

    /// <summary>
    /// Surveyor notes / caption for the photo
    /// </summary>
    public string? Remarks { get; private set; }

    // IHardDeletable - Explicit interface implementation
    private bool _markedForDeletion = false;
    private DateTime? _markedForDeletionDate;

    public bool MarkedForDeletion => _markedForDeletion;
    public DateTime? MarkedForDeletionDate => _markedForDeletionDate;

    // Explicit interface implementation for setters
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

    // Navigation Properties
    public Master.PropertyPhotoTypeEntity? PhotoType { get; private set; }

    public DocumentBindingEntity? DocumentBinding { get; private set; }

    // ========== Domain Methods ==========

    /// <summary>
    /// Link document binding to this photo
    /// </summary>
    public void LinkDocumentBinding(int documentBindingId)
    {
        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        if (_markedForDeletion)
            throw new InvalidOperationException("Cannot link document to a photo marked for deletion.");

        DocumentBindingId = documentBindingId;
    }

    /// <summary>
    /// Remove document binding link
    /// </summary>
    public void UnlinkDocumentBinding()
    {
        DocumentBindingId = null;
    }

    /// <summary>
    /// Set the gallery display order
    /// </summary>
    public void SetDisplayOrder(int? displayOrder)
    {
        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Set the remarks / caption with validation
    /// </summary>
    public void SetRemarks(string? remarks)
    {
        ValidateRemarks(remarks);
        Remarks = remarks;
    }

    /// <summary>
    /// Mark this photo as superseded (no longer the latest version).
    /// Called when a newer version is uploaded via replace; the row is retained for audit.
    /// </summary>
    public void MarkAsSuperseded()
    {
        IsLatest = false;
    }

    /// <summary>
    /// Mark photo for soft deletion. Frees the latest-per-type slot so a new photo
    /// can be uploaded for the same (PropertyId, PhotoTypeId).
    /// </summary>
    public void MarkForDeletion()
    {
        if (_markedForDeletion)
            throw new InvalidOperationException("Photo is already marked for deletion.");

        _markedForDeletion = true;
        _markedForDeletionDate = DateTime.Now;
        IsActive = false;
        IsLatest = false;
    }

    /// <summary>
    /// Restore photo from soft deletion
    /// </summary>
    public void RestoreFromDeletion()
    {
        if (!_markedForDeletion)
            throw new InvalidOperationException("Photo is not marked for deletion.");

        _markedForDeletion = false;
        _markedForDeletionDate = null;
        IsActive = true;
    }

    /// <summary>
    /// Check if photo has an attached document
    /// </summary>
    public bool HasDocument()
    {
        return DocumentBindingId.HasValue && DocumentBindingId.Value > 0;
    }

    private static void ValidateRequiredIds(int propertyId, int photoTypeId)
    {
        if (propertyId <= 0)
            throw new ArgumentException("Property ID must be greater than zero.", nameof(propertyId));

        if (photoTypeId <= 0)
            throw new ArgumentException("Photo type ID must be greater than zero.", nameof(photoTypeId));
    }

    private static void ValidateRemarks(string? remarks)
    {
        if (!string.IsNullOrWhiteSpace(remarks) && remarks.Length > 500)
            throw new ArgumentException("Remarks cannot exceed 500 characters.", nameof(remarks));
    }
}
