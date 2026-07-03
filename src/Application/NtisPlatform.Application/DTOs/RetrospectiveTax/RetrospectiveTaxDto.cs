namespace NtisPlatform.Application.DTOs.RetrospectiveTax;

/// <summary>
/// The "Retrospective Tax Details" screen payload for a single property: the active tax heads
/// (pivot column headers, ordered by PTIS.TaxMaster.DisplayOrder) and one row per finance year with
/// pending amounts per head. Assembled from PTIS.TaxPendingDetailsRetro, re-implemented in EF Core
/// LINQ in place of the legacy dynamic-PIVOT SQL script.
/// </summary>
public class RetrospectiveTaxDto
{
    /// <summary>PTIS.PropertyMast.Id.</summary>
    public int PropertyId { get; set; }

    /// <summary>Active tax head names, ordered by TaxMaster.DisplayOrder — the pivot's column headers.</summary>
    public List<string> TaxHeadNames { get; set; } = [];

    /// <summary>One row per finance year with pending amounts, ordered by YearMaster.Year ascending.</summary>
    public List<RetrospectiveTaxYearRowDto> Years { get; set; } = [];
}
