using NtisPlatform.Core.Constants;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Polymorphic association between documents and entities (CORE.DocumentBinding)
/// Links documents to any entity in any module
/// Rich domain model with validation and business logic
/// </summary>
public class DocumentBindingEntity : BaseEntity
{
    // Private backing fields for encapsulation
    private string _moduleCode = string.Empty;
    private string _referenceTableName = string.Empty;

    /// <summary>
    /// Protected constructor for EF Core
    /// </summary>
    protected DocumentBindingEntity() { }

    /// <summary>
    /// Internal constructor for testing purposes only - provides full control over entity state
    /// </summary>
    internal DocumentBindingEntity(
        int documentId,
        string moduleCode,
        string referenceTableName,
        int? referenceTableId = null,
        Guid? referenceTableIdGuid = null,
        string? bindingPurpose = null,
        bool isPrimaryDocument = false)
    {
        DocumentId = documentId;
        _moduleCode = moduleCode.ToUpperInvariant();
        _referenceTableName = referenceTableName;
        ReferenceTableId = referenceTableId;
        ReferenceTableIdGuid = referenceTableIdGuid;
        BindingPurpose = bindingPurpose;
        IsPrimaryDocument = isPrimaryDocument;
        IsReferenceValid = true;
        IsActive = true;
    }

    /// <summary>
    /// Factory method to create a document binding with INT reference ID
    /// </summary>
    public static DocumentBindingEntity CreateWithIntReference(
        int documentId,
        string moduleCode,
        string referenceTableName,
        int referenceTableId,
        string? bindingPurpose = null)
    {
        if (documentId <= 0)
            throw new ArgumentException("Document ID must be greater than zero.", nameof(documentId));

        if (string.IsNullOrWhiteSpace(moduleCode))
            throw new ArgumentException("Module code cannot be empty.", nameof(moduleCode));

        if (string.IsNullOrWhiteSpace(referenceTableName))
            throw new ArgumentException("Reference table name cannot be empty.", nameof(referenceTableName));

        if (referenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(referenceTableId));

        var binding = new DocumentBindingEntity
        {
            DocumentId = documentId,
            _moduleCode = moduleCode.ToUpperInvariant(),
            _referenceTableName = referenceTableName,
            ReferenceTableId = referenceTableId,
            ReferenceTableIdGuid = null,
            IsPrimaryDocument = false,
            IsReferenceValid = true,
            IsActive = true
        };

        if (!string.IsNullOrWhiteSpace(bindingPurpose))
        {
            binding.SetBindingPurpose(bindingPurpose);
        }

        return binding;
    }

    /// <summary>
    /// Factory method to create a document binding with GUID reference ID
    /// </summary>
    public static DocumentBindingEntity CreateWithGuidReference(
        int documentId,
        string moduleCode,
        string referenceTableName,
        Guid referenceTableIdGuid,
        string? bindingPurpose = null)
    {
        if (documentId <= 0)
            throw new ArgumentException("Document ID must be greater than zero.", nameof(documentId));

        if (string.IsNullOrWhiteSpace(moduleCode))
            throw new ArgumentException("Module code cannot be empty.", nameof(moduleCode));

        if (string.IsNullOrWhiteSpace(referenceTableName))
            throw new ArgumentException("Reference table name cannot be empty.", nameof(referenceTableName));

        if (referenceTableIdGuid == Guid.Empty)
            throw new ArgumentException("Reference table GUID cannot be empty.", nameof(referenceTableIdGuid));

        var binding = new DocumentBindingEntity
        {
            DocumentId = documentId,
            _moduleCode = moduleCode.ToUpperInvariant(),
            _referenceTableName = referenceTableName,
            ReferenceTableId = null,
            ReferenceTableIdGuid = referenceTableIdGuid,
            IsPrimaryDocument = false,
            IsReferenceValid = true,
            IsActive = true
        };

        if (!string.IsNullOrWhiteSpace(bindingPurpose))
        {
            binding.SetBindingPurpose(bindingPurpose);
        }

        return binding;
    }

    public int DocumentId { get; private set; }

    // Polymorphic Reference
    /// <summary>
    /// Module code: PROPERTY, WATER_TAX, etc.
    /// </summary>
    public string ModuleCode
    {
        get => _moduleCode;
        private set => _moduleCode = value?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(ModuleCode));
    }

    /// <summary>
    /// Table name: PropertyCertificate, WaterConnection, etc.
    /// </summary>
    public string ReferenceTableName
    {
        get => _referenceTableName;
        private set => _referenceTableName = value ?? throw new ArgumentNullException(nameof(ReferenceTableName));
    }

    /// <summary>
    /// Reference ID for INT primary keys
    /// Either ReferenceTableId OR ReferenceTableIdGuid must be provided (not both)
    /// </summary>
    public int? ReferenceTableId { get; private set; }

    /// <summary>
    /// Reference ID for GUID primary keys
    /// Either ReferenceTableId OR ReferenceTableIdGuid must be provided (not both)
    /// </summary>
    public Guid? ReferenceTableIdGuid { get; private set; }

    // Binding Metadata
    /// <summary>
    /// Purpose: MainCertificate, SupportingDocument, etc.
    /// </summary>
    public string? BindingPurpose { get; private set; }

    public int? DisplayOrder { get; private set; }

    public bool IsPrimaryDocument { get; private set; } = false;

    public string? Notes { get; private set; }

    // Access Control
    public string? AccessPermission { get; private set; }

    public DateTime? ExpiryDate { get; private set; }

    // Auth Reference
    /// <summary>
    /// Authorization module code (e.g., PROPERTY)
    /// Used for permission check - check if user can access this module's entity
    /// </summary>
    public string? AuthModuleCode { get; private set; }

    /// <summary>
    /// Authorization reference ID (e.g., PropertyId)
    /// Used for permission check - check if user can access entity with this ID
    /// </summary>
    public int? AuthReferenceId { get; private set; }

    // Validation
    public bool IsReferenceValid { get; private set; } = true;

    public DateTime? LastValidatedDate { get; private set; }

    public string? ValidationError { get; private set; }

    public byte[]? RowVersion { get; set; }

    // Navigation Properties
    public DocumentEntity Document { get; private set; } = null!;

    // ========== Domain Methods ==========

    /// <summary>
    /// Validate that binding has proper reference (either INT or GUID, not both)
    /// </summary>
    public bool ValidateBinding()
    {
        // Must have exactly one reference type
        var hasIntReference = ReferenceTableId.HasValue && ReferenceTableId.Value > 0;
        var hasGuidReference = ReferenceTableIdGuid.HasValue && ReferenceTableIdGuid.Value != Guid.Empty;

        if (!hasIntReference && !hasGuidReference)
        {
            ValidationError = "Binding must have either ReferenceTableId or ReferenceTableIdGuid.";
            IsReferenceValid = false;
            return false;
        }

        if (hasIntReference && hasGuidReference)
        {
            ValidationError = "Binding cannot have both ReferenceTableId and ReferenceTableIdGuid.";
            IsReferenceValid = false;
            return false;
        }

        // Check expiry
        if (ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Now)
        {
            ValidationError = "Document binding has expired.";
            IsReferenceValid = false;
            return false;
        }

        ValidationError = null;
        IsReferenceValid = true;
        LastValidatedDate = DateTime.Now;
        return true;
    }

    /// <summary>
    /// Mark reference as invalid with reason
    /// </summary>
    public void MarkAsInvalid(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty.", nameof(reason));

        IsReferenceValid = false;
        ValidationError = reason;
        LastValidatedDate = DateTime.Now;
    }

    /// <summary>
    /// Mark reference as valid
    /// </summary>
    public void MarkAsValid()
    {
        IsReferenceValid = true;
        ValidationError = null;
        LastValidatedDate = DateTime.Now;
    }

    /// <summary>
    /// Set binding purpose with validation
    /// </summary>
    public void SetBindingPurpose(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("Binding purpose cannot be empty.", nameof(purpose));

        if (purpose.Length > 200)
            throw new ArgumentException("Binding purpose cannot exceed 200 characters.", nameof(purpose));

        BindingPurpose = purpose;
    }

    /// <summary>
    /// Mark as primary document (only one per reference should be primary)
    /// </summary>
    public void MarkAsPrimary()
    {
        IsPrimaryDocument = true;
        DisplayOrder = 0; // Primary documents shown first
    }

    /// <summary>
    /// Remove primary document flag
    /// </summary>
    public void UnmarkAsPrimary()
    {
        IsPrimaryDocument = false;
    }

    /// <summary>
    /// Set display order
    /// </summary>
    public void SetDisplayOrder(int order)
    {
        if (order < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(order));

        DisplayOrder = order;
    }

    /// <summary>
    /// Add notes to the binding
    /// </summary>
    public void AddNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException("Notes cannot be empty.", nameof(notes));

        if (notes.Length > 1000)
            throw new ArgumentException("Notes cannot exceed 1000 characters.", nameof(notes));

        Notes = notes;
    }

    /// <summary>
    /// Set expiry date for the binding
    /// </summary>
    public void SetExpiryDate(DateTime expiryDate)
    {
        if (expiryDate <= DateTime.Now)
            throw new ArgumentException("Expiry date must be in the future.", nameof(expiryDate));

        ExpiryDate = expiryDate;
    }

    /// <summary>
    /// Check if binding is expired
    /// </summary>
    public bool IsExpired()
    {
        return ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Now;
    }

    /// <summary>
    /// Set authorization context for permission checks
    /// </summary>
    public void SetAuthorizationContext(string authModuleCode, int authReferenceId)
    {
        if (string.IsNullOrWhiteSpace(authModuleCode))
            throw new ArgumentException("Authorization module code cannot be empty.", nameof(authModuleCode));

        if (authReferenceId <= 0)
            throw new ArgumentException("Authorization reference ID must be greater than zero.", nameof(authReferenceId));

        AuthModuleCode = authModuleCode.ToUpperInvariant();
        AuthReferenceId = authReferenceId;
    }

    /// <summary>
    /// Check if binding is active and valid
    /// </summary>
    public bool IsActiveAndValid()
    {
        return IsActive && IsReferenceValid && !IsExpired();
    }

    /// <summary>
    /// Update the reference table ID for int-based references
    /// </summary>
    public void UpdateReferenceTableId(int newReferenceTableId)
    {
        if (newReferenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(newReferenceTableId));

        if (!ReferenceTableId.HasValue)
            throw new InvalidOperationException("Cannot update reference table ID for a GUID-based binding.");

        ReferenceTableId = newReferenceTableId;
    }

    /// <summary>
    /// Update the reference table GUID for guid-based references
    /// </summary>
    public void UpdateReferenceTableIdGuid(Guid newReferenceTableIdGuid)
    {
        if (newReferenceTableIdGuid == Guid.Empty)
            throw new ArgumentException("Reference table GUID cannot be empty.", nameof(newReferenceTableIdGuid));

        if (!ReferenceTableIdGuid.HasValue)
            throw new InvalidOperationException("Cannot update reference table GUID for an INT-based binding.");

        ReferenceTableIdGuid = newReferenceTableIdGuid;
    }
}
