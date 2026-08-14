using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// A single uploaded ULB document instance (e.g. the currently-certified Tax Zoning List/Map).
/// Replaces the old, module-specific <c>TaxZoningCertificateDocument</c> table with a generic
/// <c>PTIS.ULBDocument</c> row keyed by <see cref="ULBDocumentTypeId"/>. Older uploads for the same
/// type are kept (not soft-deleted) with <see cref="IsLatest"/> flipped to false, rather than
/// erased, so history isn't lost.
/// </summary>
public class ULBDocumentEntity : BaseEntity, IHardDeletable
{
    public int ULBDocumentTypeId { get; set; }

    /// <summary>FK to CORE.DocumentBinding(Id). NULL until the binding handler links it post-upload.</summary>
    public int? DocumentBindingId { get; set; }

    public string? DocumentTitle { get; set; }

    public string? Remark { get; set; }

    /// <summary>True for the current/active upload of this document type; older uploads are kept with this false.</summary>
    public bool IsLatest { get; set; } = true;

    // IHardDeletable
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual ULBDocumentTypeEntity? ULBDocumentType { get; set; }
    public virtual DocumentBindingEntity? DocumentBinding { get; set; }
}
