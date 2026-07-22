using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents historical tax pending details in the PTIS system (TaxPendingDetailsOld table).
/// Stores year-wise, tax-wise pending amounts for a property's old/historical (PropertyMastOld) record.
/// </summary>
[Table("TaxPendingDetailsOld", Schema = "PTIS")]
public class TaxPendingDetailsOldEntity : BaseEntity
{
    /// <summary>
    /// Foreign Key to PropertyMastOld.Id
    /// </summary>
    public int PropertyMastOldId { get; set; }

    /// <summary>
    /// Foreign key to YearMaster - represents the pending year for this tax.
    /// </summary>
    public int PendingYearId { get; set; }

    /// <summary>
    /// Foreign key to TaxMaster - represents the type of tax.
    /// </summary>
    public int TaxId { get; set; }

    /// <summary>
    /// The pending tax amount for this property, year, and tax type combination.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PendingAmount { get; set; }

    /// <summary>
    /// When true, this pending tax record is skipped during tax calculation.
    /// </summary>
    public bool PendingFixed { get; set; } = false;

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}
