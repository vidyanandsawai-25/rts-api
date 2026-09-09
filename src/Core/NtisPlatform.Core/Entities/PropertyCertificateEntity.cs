using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Property certificate business table (PTIS.PropertyCertificates)
/// Stores certificate records for properties with link to document storage
/// Rich domain model with validation and business logic
/// </summary>
public class PropertyCertificateEntity : BaseEntity, IHardDeletable
{
    /// <summary>
    /// Protected constructor for EF Core
    /// </summary>
    protected PropertyCertificateEntity() { }

    /// <summary>
    /// Internal constructor for testing purposes only
    /// </summary>
    internal PropertyCertificateEntity(
        int? propertyId,
        int certificateTypeId,
        string? certificateNo = null,
        DateTime? issueDate = null,
        int? documentBindingId = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null,
        int? propertyDetailsId = null,
        string entityType = "P",
        int? societyDetailId = null,
        int? wingDetailId = null)
    {
        PropertyId = propertyId;
        CertificateTypeId = certificateTypeId;
        CertificateNo = certificateNo;
        IssueDate = issueDate;
        DocumentBindingId = documentBindingId;
        _markedForDeletion = markedForDeletion;
        _markedForDeletionDate = markedForDeletionDate;
        PropertyDetailsId = propertyDetailsId;
        EntityType = entityType;
        SocietyDetailId = societyDetailId;
        WingDetailId = wingDetailId;
    }

    /// <summary>
    /// Factory method to create a new property certificate without document binding.
    /// Use this when you need to create the certificate before the DocumentBinding exists.
    /// </summary>
    public static PropertyCertificateEntity Create(
        int? propertyId,
        int certificateTypeId,
        string? certificateNo = null,
        DateTime? issueDate = null,
        int? propertyDetailsId = null,
        string entityType = "P",
        int? societyDetailId = null,
        int? wingDetailId = null)
    {
        if (certificateTypeId <= 0)
            throw new ArgumentException("Certificate type ID must be greater than zero.", nameof(certificateTypeId));

        if (!string.IsNullOrWhiteSpace(certificateNo) && certificateNo.Length > 100)
            throw new ArgumentException("Certificate number cannot exceed 100 characters.", nameof(certificateNo));

        if (issueDate.HasValue && issueDate.Value > DateTime.Now)
            throw new ArgumentException("Issue date cannot be in the future.", nameof(issueDate));

        ValidateEntityScope(entityType, propertyId, societyDetailId, wingDetailId);

        var certificate = new PropertyCertificateEntity
        {
            PropertyId = propertyId,
            CertificateTypeId = certificateTypeId,
            CertificateNo = certificateNo,
            IssueDate = issueDate,
            DocumentBindingId = null,
            PropertyDetailsId = propertyDetailsId,
            IsActive = true,
            _markedForDeletion = false,
            EntityType = entityType,
            SocietyDetailId = societyDetailId,
            WingDetailId = wingDetailId
        };

        return certificate;
    }

    /// <summary>
    /// Validates EntityType against the required companion ids, mirroring
    /// CK_PropertyCertificates_EntityScope exactly: 'S' requires SocietyDetailId and forbids
    /// WingDetailId/PropertyId; 'W' requires both SocietyDetailId and WingDetailId and forbids
    /// PropertyId; 'P' requires PropertyId (SocietyDetailId/WingDetailId may be set alongside it,
    /// e.g. a specific unit certificate created under a wing/society context -- the DB constraint
    /// places no restriction on them for 'P').
    /// </summary>
    private static void ValidateEntityScope(string entityType, int? propertyId, int? societyDetailId, int? wingDetailId)
    {
        switch (entityType)
        {
            case "S":
                if (!societyDetailId.HasValue)
                    throw new ArgumentException("SocietyDetailId is required when EntityType is 'S'.", nameof(societyDetailId));
                if (wingDetailId.HasValue)
                    throw new ArgumentException("WingDetailId must be null when EntityType is 'S'.", nameof(wingDetailId));
                if (propertyId.HasValue)
                    throw new ArgumentException("PropertyId must be null when EntityType is 'S'.", nameof(propertyId));
                break;
            case "W":
                if (!societyDetailId.HasValue)
                    throw new ArgumentException("SocietyDetailId is required when EntityType is 'W'.", nameof(societyDetailId));
                if (!wingDetailId.HasValue)
                    throw new ArgumentException("WingDetailId is required when EntityType is 'W'.", nameof(wingDetailId));
                if (propertyId.HasValue)
                    throw new ArgumentException("PropertyId must be null when EntityType is 'W'.", nameof(propertyId));
                break;
            case "P":
                if (!propertyId.HasValue || propertyId.Value <= 0)
                    throw new ArgumentException("PropertyId is required when EntityType is 'P'.", nameof(propertyId));
                break;
            default:
                throw new ArgumentException($"EntityType must be 'S', 'W', or 'P'. Got '{entityType}'.", nameof(entityType));
        }
    }

    /// <summary>
    /// Factory method to create a new property certificate with document binding.
    /// Optimized to eliminate the need for a separate update operation.
    /// </summary>
    public static PropertyCertificateEntity CreateWithDocument(
        int? propertyId,
        int certificateTypeId,
        int documentBindingId,
        string? certificateNo = null,
        DateTime? issueDate = null,
        int? propertyDetailsId = null,
        string entityType = "P",
        int? societyDetailId = null,
        int? wingDetailId = null)
    {
        if (certificateTypeId <= 0)
            throw new ArgumentException("Certificate type ID must be greater than zero.", nameof(certificateTypeId));

        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        if (!string.IsNullOrWhiteSpace(certificateNo) && certificateNo.Length > 100)
            throw new ArgumentException("Certificate number cannot exceed 100 characters.", nameof(certificateNo));

        if (issueDate.HasValue && issueDate.Value > DateTime.Now)
            throw new ArgumentException("Issue date cannot be in the future.", nameof(issueDate));

        ValidateEntityScope(entityType, propertyId, societyDetailId, wingDetailId);

        var certificate = new PropertyCertificateEntity
        {
            PropertyId = propertyId,
            CertificateTypeId = certificateTypeId,
            CertificateNo = certificateNo,
            IssueDate = issueDate,
            DocumentBindingId = documentBindingId,
            PropertyDetailsId = propertyDetailsId,
            IsActive = true,
            _markedForDeletion = false,
            EntityType = entityType,
            SocietyDetailId = societyDetailId,
            WingDetailId = wingDetailId
        };

        return certificate;
    }

    /// <summary>
    /// Property ID this certificate belongs to. Null when EntityType is 'S' (Society) or 'W'
    /// (Wing) -- those scopes apply to every property under the society/wing, not one specific
    /// property (see CK_PropertyCertificates_EntityScope). Always set when EntityType is 'P'.
    /// </summary>
    public int? PropertyId { get; private set; }

    /// <summary>
    /// FK to PropertyCertificateTypeMaster
    /// </summary>
    public int CertificateTypeId { get; private set; }

    /// <summary>
    /// Certificate number
    /// </summary>
    public string? CertificateNo { get; private set; }

    /// <summary>
    /// Certificate issue date
    /// </summary>
    public DateTime? IssueDate { get; private set; }

    /// <summary>
    /// FK to DocumentBinding - links to the uploaded document
    /// </summary>
    public int? DocumentBindingId { get; private set; }

    /// <summary>
    /// FK to PropertyDetails - NULL = property-level (Building Permission screen),
    /// set = floor-level (Floor screen). Enables floor-wise taxation.
    /// FK to PropertyDetails - links to specific floor/unit
    /// </summary>
    public int? PropertyDetailsId { get; private set; }

    /// <summary>
    /// Entity type (P for Property, S for Society, W for Wing)
    /// </summary>
    public string EntityType { get; private set; } = "P";

    /// <summary>
    /// Society Detail ID when EntityType is 'S' (FK to PTIS.SocietyDetailsMast)
    /// </summary>
    public int? SocietyDetailId { get; private set; }

    /// <summary>
    /// Wing Detail ID when EntityType is 'W' (FK to PTIS.WingDetailsMast)
    /// </summary>
    public int? WingDetailId { get; private set; }

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

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// Automatically updated by EF Core on each save.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// True once this certificate's date has been factored into an Occupation Tax
    /// computation (PolicyTaxDetails), so the tax engine can tell already-applied
    /// certificates apart from freshly saved ones.
    /// </summary>
    public bool TaxApplied { get; private set; }
    public DateTime? TaxAppliedDate { get; private set; }

    /// <summary>Marks this certificate as having been applied to a tax computation.</summary>
    public void MarkTaxApplied(DateTime appliedAt)
    {
        TaxApplied = true;
        TaxAppliedDate = appliedAt;
    }

    // Navigation Properties
    public Master.PropertyCertificateTypeMasterEntity? CertificateType { get; private set; }

    public DocumentBindingEntity? DocumentBinding { get; private set; }

    // ========== Domain Methods ==========

    /// <summary>
    /// Set certificate number with validation
    /// </summary>
    public void SetCertificateNumber(string certificateNo)
    {
        if (string.IsNullOrWhiteSpace(certificateNo))
            throw new ArgumentException("Certificate number cannot be empty.", nameof(certificateNo));

        if (certificateNo.Length > 100)
            throw new ArgumentException("Certificate number cannot exceed 100 characters.", nameof(certificateNo));

        CertificateNo = certificateNo.Trim();
    }

    /// <summary>
    /// Set issue date with validation
    /// </summary>
    public void SetIssueDate(DateTime issueDate)
    {
        if (issueDate > DateTime.Now)
            throw new ArgumentException("Issue date cannot be in the future.", nameof(issueDate));

        IssueDate = issueDate;
    }

    /// <summary>
    /// Link document binding to this certificate
    /// </summary>
    public void LinkDocumentBinding(int documentBindingId)
    {
        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        if (_markedForDeletion)
            throw new InvalidOperationException("Cannot link document to a certificate marked for deletion.");

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
    /// Enable the certificate (sets IsActive to true)
    /// </summary>
    public void Enable()
    {
        if (_markedForDeletion)
            throw new InvalidOperationException("Cannot enable a certificate marked for deletion.");

        if (!IssueDate.HasValue)
            throw new InvalidOperationException("Cannot enable certificate without an issue date.");

        if (string.IsNullOrWhiteSpace(CertificateNo))
            throw new InvalidOperationException("Cannot enable certificate without a certificate number.");

        IsActive = true;
    }

    /// <summary>
    /// Disable the certificate (sets IsActive to false)
    /// </summary>
    public void Disable()
    {
        IsActive = false;
    }

    /// <summary>
    /// Mark certificate for soft deletion
    /// </summary>
    public void MarkForDeletion()
    {
        if (_markedForDeletion)
            throw new InvalidOperationException("Certificate is already marked for deletion.");

        _markedForDeletion = true;
        _markedForDeletionDate = DateTime.Now;
        IsActive = false;
    }

    /// <summary>
    /// Restore certificate from soft deletion
    /// </summary>
    public void RestoreFromDeletion()
    {
        if (!_markedForDeletion)
            throw new InvalidOperationException("Certificate is not marked for deletion.");

        _markedForDeletion = false;
        _markedForDeletionDate = null;
        IsActive = true;
        // Note: IsEnabled remains false - must be explicitly enabled after restore
    }

    /// <summary>
    /// Validate certificate completeness
    /// </summary>
    public bool IsComplete()
    {
        return !string.IsNullOrWhiteSpace(CertificateNo)
               && IssueDate.HasValue
               && (EntityType != "P" || (PropertyId.HasValue && PropertyId.Value > 0))
               && CertificateTypeId > 0
               && !_markedForDeletion;
    }

    /// <summary>
    /// Validate if certificate can be enabled
    /// </summary>
    public bool CanBeEnabled()
    {
        return IsComplete()
               && IsActive
               && !_markedForDeletion;
    }

    /// <summary>
    /// Check if certificate has an attached document
    /// </summary>
    public bool HasDocument()
    {
        return DocumentBindingId.HasValue && DocumentBindingId.Value > 0;
    }

    /// <summary>
    /// Update certificate details
    /// </summary>
    public void UpdateDetails(string? certificateNo = null, DateTime? issueDate = null)
    {
        if (!string.IsNullOrWhiteSpace(certificateNo))
        {
            SetCertificateNumber(certificateNo);
        }

        if (issueDate.HasValue)
        {
            SetIssueDate(issueDate.Value);
        }
    }
}
