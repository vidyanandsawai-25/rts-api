using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Property photo business table (PTIS.PropertyPhoto).
/// Stores photo records for Property ('P'), Society ('S'), and Wing ('W') with a link to document storage.
/// Supports versioning via <see cref="IsLatest"/> (1 = current, 0 = superseded).
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
        int? propertyId,
        int photoTypeId,
        int? documentBindingId = null,
        bool isLatest = true,
        int? displayOrder = null,
        string? remarks = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null,
        string entityType = "P",
        int? societyDetailId = null,
        int? wingDetailId = null,
        string? type = null)
    {
        PropertyId = propertyId;
        PhotoTypeId = photoTypeId;
        DocumentBindingId = documentBindingId;
        IsLatest = isLatest;
        DisplayOrder = displayOrder;
        Remarks = remarks;
        _markedForDeletion = markedForDeletion;
        _markedForDeletionDate = markedForDeletionDate;
        EntityType = entityType;
        SocietyDetailId = societyDetailId;
        WingDetailId = wingDetailId;
        Type = type;
    }

    /// <summary>
    /// Factory method to create a new Property photo ('P') without document binding.
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
            EntityType = "P",
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
            EntityType = "P",
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
    /// Factory method to create a new photo with all details including entityType, societyDetailId, and wingDetailId.
    /// </summary>
    public static PropertyPhotoEntity CreateWithDetails(
        int? propertyId,
        int photoTypeId,
        string entityType,
        int? societyDetailId,
        int? wingDetailId,
        int? documentBindingId = null,
        int? displayOrder = null,
        string? remarks = null,
        string? type = null)
    {
        if (photoTypeId <= 0)
            throw new ArgumentException("Photo type ID must be greater than zero.", nameof(photoTypeId));

        if (propertyId.HasValue && propertyId.Value <= 0)
            throw new ArgumentException("Property ID must be greater than zero.", nameof(propertyId));

        if (documentBindingId.HasValue && documentBindingId.Value <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new PropertyPhotoEntity
        {
            EntityType = entityType,
            PropertyId = propertyId,
            PhotoTypeId = photoTypeId,
            DocumentBindingId = documentBindingId,
            SocietyDetailId = societyDetailId,
            WingDetailId = wingDetailId,
            Type = type,
            IsLatest = true,
            DisplayOrder = displayOrder,
            Remarks = remarks,
            IsActive = true,
            _markedForDeletion = false
        };
    }

    /// <summary>
    /// Factory method to create a new Society photo ('S') without document binding.
    /// </summary>
    public static PropertyPhotoEntity CreateForSociety(
        int societyDetailId,
        int photoTypeId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(societyDetailId, photoTypeId);
        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new PropertyPhotoEntity
        {
            EntityType = "S",
            SocietyDetailId = societyDetailId,
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
    /// Factory method to create a new Wing photo ('W') without document binding.
    /// </summary>
    public static PropertyPhotoEntity CreateForWing(
        int wingDetailId,
        int photoTypeId,
        int? displayOrder = null,
        string? remarks = null)
    {
        ValidateRequiredIds(wingDetailId, photoTypeId);
        ValidateRemarks(remarks);

        if (displayOrder.HasValue && displayOrder.Value < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(displayOrder));

        return new PropertyPhotoEntity
        {
            EntityType = "W",
            WingDetailId = wingDetailId,
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
    /// Discriminator flag: 'P' = Property, 'S' = Society, 'W' = Wing
    /// </summary>
    public string EntityType { get; private set; } = "P";

    /// <summary>
    /// Society Detail ID when EntityType is 'S' (FK to PTIS.SocietyDetails)
    /// </summary>
    public int? SocietyDetailId { get; private set; }

    /// <summary>
    /// Wing Detail ID when EntityType is 'W' (FK to PTIS.SocietyWingDetails)
    /// </summary>
    public int? WingDetailId { get; private set; }

    /// <summary>
    /// Property ID this photo belongs to when EntityType is 'P' (FK to PTIS.PropertyMast)
    /// </summary>
    public int? PropertyId { get; private set; }

    /// <summary>
    /// Mirrors PTIS.PropertyMast.Type. Set only for the shared PROPERTY_PLAN photo of a
    /// non-Amenity apartment unit (PropertyId is null in that case) -- lets every unit of the
    /// same Type within a society resolve the same plan image instead of one row per unit.
    /// </summary>
    public string? Type { get; private set; }

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
    /// Gallery sort order within category
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

    [ForeignKey(nameof(WingDetailId))]
    public virtual WingDetailsMastEntity? WingDetail { get; private set; }

    // ========== Domain Methods ==========

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

    public void UpdateDetails(string entityType, int? societyDetailId, int? wingDetailId,int? propertyId)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be null or empty.", nameof(entityType));

        EntityType = entityType;
        SocietyDetailId = societyDetailId;
        WingDetailId = wingDetailId;
        PropertyId = propertyId;
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

    public void RestoreFromSuperseding()
    {
        IsLatest = true;
    }

    public void MarkForDeletion()
    {
        if (_markedForDeletion)
            throw new InvalidOperationException("Photo is already marked for deletion.");

        _markedForDeletion = true;
        _markedForDeletionDate = DateTime.Now;
        IsActive = false;
        IsLatest = false;
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

    private static void ValidateRequiredIds(int entityId, int photoTypeId)
    {
        if (entityId <= 0)
            throw new ArgumentException("Entity ID must be greater than zero.", nameof(entityId));

        if (photoTypeId <= 0)
            throw new ArgumentException("Photo type ID must be greater than zero.", nameof(photoTypeId));
    }

    private static void ValidateRemarks(string? remarks)
    {
        if (!string.IsNullOrWhiteSpace(remarks) && remarks.Length > 500)
            throw new ArgumentException("Remarks cannot exceed 500 characters.", nameof(remarks));
    }

}
