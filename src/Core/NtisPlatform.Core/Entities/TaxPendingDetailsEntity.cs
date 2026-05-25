using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents tax pending details in the PTIS system
/// </summary>
public class TaxPendingDetailsEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }

    public decimal? PendingAmount { get; set; }

    // Navigation property
    public virtual PropertyEntity? PropertyMast { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion.
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when the entity was marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }
}
