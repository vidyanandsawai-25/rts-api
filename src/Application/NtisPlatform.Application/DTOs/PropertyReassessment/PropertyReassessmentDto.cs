namespace NtisPlatform.Application.DTOs.PropertyReassessment;

/// <summary>
/// The complete "Property Re-Assessment" screen payload for a single property: the resolved ids,
/// old/new photos, old/new floor details, and the old-vs-new tax-head comparison. Assembled from the
/// four steps of the legacy re-assessment SQL, re-implemented in EF Core LINQ.
/// </summary>
public class PropertyReassessmentDto
{
    /// <summary>Current (new-survey) property id — PTIS.PropertyMast.Id.</summary>
    public int PropertyId { get; set; }

    /// <summary>Historical property id — PTIS.PropertyMast.PropertyMastOldId (null when no old record is linked).</summary>
    public int? PropertyOldId { get; set; }

    /// <summary>Old/new plan &amp; property photos (STEP 2).</summary>
    public List<ReassessmentPhotoDto> Photos { get; set; } = [];

    /// <summary>New Survey floor-wise details (STEP 3).</summary>
    public List<ReassessmentFloorDto> NewFloorDetails { get; set; } = [];

    /// <summary>Municipal Corp. Registration (old) floor-wise details (STEP 3).</summary>
    public List<ReassessmentFloorDto> OldFloorDetails { get; set; } = [];

    /// <summary>Per-tax-head old-vs-new amounts (STEP 4).</summary>
    public List<ReassessmentTaxHeadDto> TaxSummary { get; set; } = [];
}
