using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Polymorphic association between documents and entities (CORE.DocumentBinding)
/// Links documents to any entity in any module
/// Rich domain model with validation and business logic
/// </summary>
public class DocumentBindingEntity : BaseEntity, IHardDeletable
{
    // Private backing fields for encapsulation
    private string _referenceTableName = string.Empty;
    private string _referencePropertyName = string.Empty;

    /// <summary>
    /// Protected constructor for EF Core
    /// </summary>
    protected DocumentBindingEntity() { }

    /// <summary>
    /// Internal constructor for testing purposes only - provides full control over entity state
    /// </summary>
    internal DocumentBindingEntity(
        int documentId,
        int departmentId,
        int moduleId,
        string referenceTableName,
        string referencePropertyName,
        int? referenceTableId = null,
        Guid? referenceTableIdGuid = null,
        string? bindingPurpose = null,
        bool isPrimaryDocument = false)
    {
        DocumentId = documentId;
        DepartmentId = departmentId;
        ModuleId = moduleId;
        _referenceTableName = referenceTableName;
        _referencePropertyName = referencePropertyName;
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
        int departmentId,
        int moduleId,
        string referenceTableName,
        int referenceTableId,
        string referencePropertyName,
        string? bindingPurpose = null)
    {
        if (documentId <= 0)
            throw new ArgumentException("Document ID must be greater than zero.", nameof(documentId));

        if (departmentId <= 0)
            throw new ArgumentException("Department ID must be greater than zero.", nameof(departmentId));

        if (moduleId <= 0)
            throw new ArgumentException("Module ID must be greater than zero.", nameof(moduleId));

        if (string.IsNullOrWhiteSpace(referenceTableName))
            throw new ArgumentException("Reference table name cannot be empty.", nameof(referenceTableName));

        if (string.IsNullOrWhiteSpace(referencePropertyName))
            throw new ArgumentException("Reference property name cannot be empty.", nameof(referencePropertyName));

        if (referenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(referenceTableId));

        var binding = new DocumentBindingEntity
        {
            DocumentId = documentId,
            DepartmentId = departmentId,
            ModuleId = moduleId,
            _referenceTableName = referenceTableName,
            _referencePropertyName = referencePropertyName,
            ReferenceTableId = referenceTableId,
            ReferenceTableIdGuid = null,
            IsPrimaryDocument = false,
            IsReferenceValid = true,
            IsActive = true,
            _markedForDeletion = false
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
        int departmentId,
        int moduleId,
        string referenceTableName,
        Guid referenceTableIdGuid,
        string referencePropertyName,
        string? bindingPurpose = null)
    {
        if (documentId <= 0)
            throw new ArgumentException("Document ID must be greater than zero.", nameof(documentId));

        if (departmentId <= 0)
            throw new ArgumentException("Department ID must be greater than zero.", nameof(departmentId));

        if (moduleId <= 0)
            throw new ArgumentException("Module ID must be greater than zero.", nameof(moduleId));

        if (string.IsNullOrWhiteSpace(referenceTableName))
            throw new ArgumentException("Reference table name cannot be empty.", nameof(referenceTableName));

        if (string.IsNullOrWhiteSpace(referencePropertyName))
            throw new ArgumentException("Reference property name cannot be empty.", nameof(referencePropertyName));

        if (referenceTableIdGuid == Guid.Empty)
            throw new ArgumentException("Reference table GUID cannot be empty.", nameof(referenceTableIdGuid));

        var binding = new DocumentBindingEntity
        {
            DocumentId = documentId,
            DepartmentId = departmentId,
            ModuleId = moduleId,
            _referenceTableName = referenceTableName,
            _referencePropertyName = referencePropertyName,
            ReferenceTableId = null,
            ReferenceTableIdGuid = referenceTableIdGuid,
            IsPrimaryDocument = false,
            IsReferenceValid = true,
            IsActive = true,
            _markedForDeletion = false
        };

        if (!string.IsNullOrWhiteSpace(bindingPurpose))
        {
            binding.SetBindingPurpose(bindingPurpose);
        }

        return binding;
    }

    public int DocumentId { get; private set; }

    /// <summary>
    /// FK to CORE.DepartmentMaster(Id). Top-level grouping (e.g. PTIS, Water).
    /// Example: 3 (DepartmentMaster.Id for 'PTIS')
    /// </summary>
    public int DepartmentId { get; private set; }

    /// <summary>
    /// FK to CORE.ModuleMaster(Id). Specific module under the department.
    /// Example: 12 (ModuleMaster.Id for 'PropertyCertificate')
    /// </summary>
    public int ModuleId { get; private set; }

    /// <summary>
    /// Table name: PropertyCertificates, WaterConnection, etc.
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

    /// <summary>
    /// Name of the column in [ReferenceTableName] that holds the PK value.
    /// Lets generic code build joins/links without hardcoding per module.
    /// Example: 'Id', 'PropertyCertificateId', 'WaterConnectionId'
    /// </summary>
    public string ReferencePropertyName
    {
        get => _referencePropertyName;
        private set => _referencePropertyName = value ?? throw new ArgumentNullException(nameof(ReferencePropertyName));
    }

    // Binding Metadata
    /// <summary>
    /// Purpose: MainCertificate, SupportingDocument, etc.
    /// </summary>
    public string? BindingPurpose { get; private set; }

    public bool IsPrimaryDocument { get; private set; } = false;

    public string? Notes { get; private set; }

    // Access Control
    public string? AccessPermission { get; private set; }

    // Auth Reference
    /// <summary>
    /// FK to CORE.DepartmentMaster(Id). Department against which authorization
    /// is resolved (parent-entity authorization model).
    /// Example: 3 (DepartmentMaster.Id for 'PTIS')
    /// </summary>
    public int? AuthDepartmentId { get; private set; }

    /// <summary>
    /// PK of the row in the auth module that grants access.
    /// Together with AuthDepartmentId, answers "who owns this for permission checks".
    /// Example: 1001 (Property.Id used for property-level auth)
    /// </summary>
    public int? AuthReferenceId { get; private set; }

    // Validation
    public bool IsReferenceValid { get; private set; } = true;

    // IHardDeletable (soft delete) - Explicit interface implementation
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

    public byte[]? RowVersion { get; set; }

    // Navigation Properties
    public DocumentEntity? Document { get; private set; }

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
            IsReferenceValid = false;
            return false;
        }

        if (hasIntReference && hasGuidReference)
        {
            IsReferenceValid = false;
            return false;
        }

        IsReferenceValid = true;
        return true;
    }

    /// <summary>
    /// Mark reference as invalid
    /// </summary>
    public void MarkAsInvalid()
    {
        IsReferenceValid = false;
    }

    /// <summary>
    /// Mark reference as valid
    /// </summary>
    public void MarkAsValid()
    {
        IsReferenceValid = true;
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
    }

    /// <summary>
    /// Remove primary document flag
    /// </summary>
    public void UnmarkAsPrimary()
    {
        IsPrimaryDocument = false;
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
    /// Set authorization context for permission checks
    /// </summary>
    public void SetAuthorizationContext(int authDepartmentId, int authReferenceId)
    {
        if (authDepartmentId <= 0)
            throw new ArgumentException("Authorization department ID must be greater than zero.", nameof(authDepartmentId));

        if (authReferenceId <= 0)
            throw new ArgumentException("Authorization reference ID must be greater than zero.", nameof(authReferenceId));

        AuthDepartmentId = authDepartmentId;
        AuthReferenceId = authReferenceId;
    }

    /// <summary>
    /// Check if binding is active and valid
    /// </summary>
    public bool IsActiveAndValid()
    {
        return IsActive && IsReferenceValid && !_markedForDeletion;
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

    /// <summary>
    /// Mark binding for soft deletion
    /// </summary>
    public void MarkForDeletion()
    {
        if (_markedForDeletion)
            throw new InvalidOperationException("Binding is already marked for deletion.");

        _markedForDeletion = true;
        _markedForDeletionDate = DateTime.Now;
        IsActive = false;
    }

    /// <summary>
    /// Restore binding from soft deletion
    /// </summary>
    public void RestoreFromDeletion()
    {
        if (!_markedForDeletion)
            throw new InvalidOperationException("Binding is not marked for deletion.");

        _markedForDeletion = false;
        _markedForDeletionDate = null;
        IsActive = true;
    }
}
