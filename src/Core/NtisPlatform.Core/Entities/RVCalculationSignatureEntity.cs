using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Tracks the input signature (hash of the property-owned inputs that fed the last RV
/// calculation -- property/details/renter/exemption/certificate/social-detail data) for a
/// property. <see cref="Services.TaxEngine.RateableValueService"/> compares the current
/// signature against this row before recalculating; when they match, nothing relevant has
/// changed since the last run and the previously persisted results are returned as-is.
/// Holds current state only (one row per property), matching how RVCalculationResults and
/// PolicyTaxDetails already behave in this system.
/// </summary>
[Table("RVCalculationSignature", Schema = "PTIS")]
public class RVCalculationSignatureEntity : BaseEntity
{
    public int PropertyId { get; set; }

    [Column(TypeName = "varchar(64)")]
    public string SignatureHash { get; set; } = string.Empty;

    public DateTime CalculatedAt { get; set; }
}
