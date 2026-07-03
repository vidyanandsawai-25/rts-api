using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class TaxPendingDetailsRetroEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }

    /// <summary>
    /// Foreign key to YearMaster - represents the pending (finance) year for this retrospective tax.
    /// </summary>
    public int PendingYearId { get; set; }

    /// <summary>
    /// Foreign key to TaxMaster - represents the type of tax.
    /// </summary>
    public int TaxId { get; set; }

    /// <summary>
    /// The retrospective pending tax amount for this property, year, and tax type combination.
    /// </summary>
    public decimal? PendingAmount { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public virtual PropertyEntity? PropertyMast { get; set; }

    public virtual YearMasterEntity? PendingYear { get; set; }

    public virtual TaxMasterEntity? Tax { get; set; }
}
