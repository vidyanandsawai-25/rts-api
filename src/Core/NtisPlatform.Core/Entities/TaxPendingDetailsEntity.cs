using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents tax pending details in the PTIS system.
/// Stores year-wise, tax-wise pending amounts with ability to skip during tax calculation.
/// </summary>
public class TaxPendingDetailsEntity : BaseEntity, IHardDeletable
{
    /// <summary>
    /// The property this pending tax belongs to
    /// </summary>
    public int PropertyId { get; set; }

    /// <summary>
    /// Foreign key to YearMaster - represents the pending year for this tax
    /// </summary>
    public int PendingYearId { get; set; }

    /// <summary>
    /// Foreign key to TaxMaster - represents the type of tax
    /// </summary>
    public int TaxId { get; set; }

    /// <summary>
    /// The pending tax amount for this property, year, and tax type combination
    /// </summary>
    public decimal? PendingAmount { get; set; }

    /// <summary>
    /// When true, this pending tax record will be skipped during tax calculation.
    /// Used after property combine to prevent double-counting.
    /// </summary>
    public bool PendingFixed { get; set; } = false;

    /// <summary>
    /// Indicates whether the entity is marked for deletion.
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when the entity was marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the associated Property
    /// </summary>
    public virtual PropertyEntity? PropertyMast { get; set; }

    /// <summary>
    /// Navigation property to the Year Master (pending year)
    /// </summary>
    public virtual YearMasterEntity? PendingYear { get; set; }

    /// <summary>
    /// Navigation property to the Tax Master (type of tax)
    /// </summary>
    public virtual TaxMasterEntity? Tax { get; set; }
}
