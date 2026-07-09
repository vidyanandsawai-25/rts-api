using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Polymorphic association between documents and entities (CORE.DocumentBinding).
/// Implements IHardDeletable to support background cleanup service for orphaned bindings.
/// </summary>
public class DocumentBindingEntity : BaseEntity, IHardDeletable
{
    public static DocumentBindingEntity CreateWithIntReference(
        int documentId,
        int departmentId,
        int moduleId,
        string referenceTableName,
        int referenceTableId,
        string referencePropertyName,
        string? bindingPurpose = null)
    {
        if (referenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(referenceTableId));
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

        var binding = new DocumentBindingEntity
        {
            DocumentId = documentId,
            DepartmentId = departmentId,
            ModuleId = moduleId,
            ReferenceTableName = referenceTableName,
            ReferenceTableId = referenceTableId,
            ReferencePropertyName = referencePropertyName,
            BindingPurpose = bindingPurpose,
            IsReferenceValid = true,
            IsActive = true
        };
        if (!binding.ValidateBinding())
            throw new InvalidOperationException("Failed to validate binding: XOR constraint violation (both or neither reference types provided).");
        return binding;
    }

    public static DocumentBindingEntity CreateWithGuidReference(
        int documentId,
        int departmentId,
        int moduleId,
        string referenceTableName,
        Guid referenceTableIdGuid,
        string referencePropertyName,
        string? bindingPurpose = null)
    {
        if (referenceTableIdGuid == Guid.Empty)
            throw new ArgumentException("Reference table GUID cannot be empty.", nameof(referenceTableIdGuid));
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

        var binding = new DocumentBindingEntity
        {
            DocumentId = documentId,
            DepartmentId = departmentId,
            ModuleId = moduleId,
            ReferenceTableName = referenceTableName,
            ReferenceTableIdGuid = referenceTableIdGuid,
            ReferencePropertyName = referencePropertyName,
            BindingPurpose = bindingPurpose,
            IsReferenceValid = true,
            IsActive = true
        };
        if (!binding.ValidateBinding())
            throw new InvalidOperationException("Failed to validate binding: XOR constraint violation (both or neither reference types provided).");
        return binding;
    }

    public int DocumentId { get; set; }
    public int DepartmentId { get; set; }
    public int ModuleId { get; set; }
    public string ReferenceTableName { get; set; } = string.Empty;
    public int? ReferenceTableId { get; set; }
    public Guid? ReferenceTableIdGuid { get; set; }
    public string ReferencePropertyName { get; set; } = string.Empty;
    public string? BindingPurpose { get; set; }
    public bool IsPrimaryDocument { get; set; } = false;
    public string? Notes { get; set; }
    public string? AccessPermission { get; set; }
    public int? AuthDepartmentId { get; set; }
    public int? AuthReferenceId { get; set; }
    public bool IsReferenceValid { get; set; } = true;
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
    public byte[]? RowVersion { get; set; }

    public DocumentEntity? Document { get; set; }

    public void SetAuthorizationContext(int authDepartmentId, int authReferenceId)
    {
        if (authDepartmentId <= 0)
            throw new ArgumentException("Authorization department ID must be greater than zero.", nameof(authDepartmentId));
        if (authReferenceId <= 0)
            throw new ArgumentException("Authorization reference ID must be greater than zero.", nameof(authReferenceId));
        AuthDepartmentId = authDepartmentId;
        AuthReferenceId = authReferenceId;
    }

    public bool ValidateBinding()
    {
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

    public void MarkForDeletion()
    {
        if (MarkedForDeletion)
            return;
        MarkedForDeletion = true;
        MarkedForDeletionDate = DateTime.Now;
        IsActive = false;
    }

    public void RestoreFromDeletion()
    {
        if (!MarkedForDeletion)
            throw new InvalidOperationException("Binding is not marked for deletion.");
        MarkedForDeletion = false;
        MarkedForDeletionDate = null;
        IsActive = true;
    }

    public void SetBindingPurpose(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose) || purpose.Length > 200)
            throw new ArgumentException("Binding purpose must be between 1 and 200 characters.", nameof(purpose));
        BindingPurpose = purpose;
    }

    public void AddNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes) || notes.Length > 1000)
            throw new ArgumentException("Notes must be between 1 and 1000 characters.", nameof(notes));
        Notes = notes;
    }

    public void MarkAsPrimary()
    {
        IsPrimaryDocument = true;
    }

    public void UnmarkAsPrimary()
    {
        IsPrimaryDocument = false;
    }

    public void UpdateReferenceTableId(int newReferenceTableId)
    {
        if (newReferenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(newReferenceTableId));
        if (!ReferenceTableId.HasValue)
            throw new InvalidOperationException("Cannot update reference table ID for a GUID-based binding.");
        ReferenceTableId = newReferenceTableId;
    }

    public void UpdateReferenceTableIdGuid(Guid newReferenceTableIdGuid)
    {
        if (newReferenceTableIdGuid == Guid.Empty)
            throw new ArgumentException("Reference table GUID cannot be empty.", nameof(newReferenceTableIdGuid));
        if (!ReferenceTableIdGuid.HasValue)
            throw new InvalidOperationException("Cannot update reference table GUID for an INT-based binding.");
        ReferenceTableIdGuid = newReferenceTableIdGuid;
    }

    public void ConvertGuidToIntReference(int newReferenceTableId)
    {
        if (newReferenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(newReferenceTableId));
        if (!ReferenceTableIdGuid.HasValue)
            throw new InvalidOperationException("Cannot convert reference type: binding is not GUID-based.");

        ReferenceTableIdGuid = null;
        ReferenceTableId = newReferenceTableId;
        IsReferenceValid = true;
    }

    public bool IsActiveAndValid()
    {
        return IsActive && IsReferenceValid && !MarkedForDeletion;
    }
}
