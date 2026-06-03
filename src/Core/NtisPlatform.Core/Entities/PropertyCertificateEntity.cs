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
        int propertyId,
        int certificateTypeId,
        string? certificateNo = null,
        DateTime? issueDate = null,
        int? documentBindingId = null,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null)
    {
        PropertyId = propertyId;
        CertificateTypeId = certificateTypeId;
        CertificateNo = certificateNo;
        IssueDate = issueDate;
        DocumentBindingId = documentBindingId;
        _markedForDeletion = markedForDeletion;
        _markedForDeletionDate = markedForDeletionDate;
    }

    /// <summary>
    /// Factory method to create a new property certificate without document binding.
    /// Use this when you need to create the certificate before the DocumentBinding exists.
    /// </summary>
    public static PropertyCertificateEntity Create(
        int propertyId,
        int certificateTypeId,
        string? certificateNo = null,
        DateTime? issueDate = null)
    {
        if (propertyId <= 0)
            throw new ArgumentException("Property ID must be greater than zero.", nameof(propertyId));

        if (certificateTypeId <= 0)
            throw new ArgumentException("Certificate type ID must be greater than zero.", nameof(certificateTypeId));

        if (!string.IsNullOrWhiteSpace(certificateNo) && certificateNo.Length > 100)
            throw new ArgumentException("Certificate number cannot exceed 100 characters.", nameof(certificateNo));

        if (issueDate.HasValue && issueDate.Value > DateTime.Now)
            throw new ArgumentException("Issue date cannot be in the future.", nameof(issueDate));

        var certificate = new PropertyCertificateEntity
        {
            PropertyId = propertyId,
            CertificateTypeId = certificateTypeId,
            CertificateNo = certificateNo,
            IssueDate = issueDate,
            DocumentBindingId = null,
            IsActive = true,
            _markedForDeletion = false
        };

        return certificate;
    }

    /// <summary>
    /// Factory method to create a new property certificate with document binding.
    /// Optimized to eliminate the need for a separate update operation.
    /// </summary>
    public static PropertyCertificateEntity CreateWithDocument(
        int propertyId,
        int certificateTypeId,
        int documentBindingId,
        string? certificateNo = null,
        DateTime? issueDate = null)
    {
        if (propertyId <= 0)
            throw new ArgumentException("Property ID must be greater than zero.", nameof(propertyId));

        if (certificateTypeId <= 0)
            throw new ArgumentException("Certificate type ID must be greater than zero.", nameof(certificateTypeId));

        if (documentBindingId <= 0)
            throw new ArgumentException("Document binding ID must be greater than zero.", nameof(documentBindingId));

        if (!string.IsNullOrWhiteSpace(certificateNo) && certificateNo.Length > 100)
            throw new ArgumentException("Certificate number cannot exceed 100 characters.", nameof(certificateNo));

        if (issueDate.HasValue && issueDate.Value > DateTime.Now)
            throw new ArgumentException("Issue date cannot be in the future.", nameof(issueDate));

        var certificate = new PropertyCertificateEntity
        {
            PropertyId = propertyId,
            CertificateTypeId = certificateTypeId,
            CertificateNo = certificateNo,
            IssueDate = issueDate,
            DocumentBindingId = documentBindingId,
            IsActive = true,
            _markedForDeletion = false
        };

        return certificate;
    }

    /// <summary>
    /// Property ID this certificate belongs to
    /// </summary>
    public int PropertyId { get; private set; }

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
               && PropertyId > 0
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
