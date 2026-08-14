using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Master-based tax mapping (PTIS.TaxMasterMapping).
/// One row per master key (e.g. a Property Type / Owner Type / Type-of-Use id) per
/// assessment-year range for a MASTER_BASED tax, giving the result to apply.
/// </summary>
[Table("TaxMasterMapping", Schema = "PTIS")]
public class TaxMasterMappingEntity : BaseEntity
{
    /// <summary>FK → PTIS.TaxMaster.</summary>
    [Required]
    public int TaxId { get; set; }

    /// <summary>Master record key this row maps (property-type id, owner-type value, type-of-use code, …).</summary>
    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public string MasterKey { get; set; } = null!;

    /// <summary>Human-readable label for the key (denormalized for display).</summary>
    [Column(TypeName = "nvarchar(200)")]
    public string? DisplayValue { get; set; }

    /// <summary>FK → PTIS.AssessmentYearRange.</summary>
    [Required]
    public int AssessmentYearRangeId { get; set; }

    /// <summary>FIXED | PERCENT.</summary>
    [Required]
    [Column(TypeName = "nvarchar(10)")]
    public string ResultMode { get; set; } = "FIXED";

    /// <summary>NONE | RV | ALV (base a PERCENT result is applied to).</summary>
    [Required]
    [Column(TypeName = "nvarchar(10)")]
    public string ResultBase { get; set; } = "NONE";

    /// <summary>Fixed amount or percentage, per <see cref="ResultMode"/>.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ResultValue { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────────
    public TaxMasterEntity? Tax { get; set; }
}
