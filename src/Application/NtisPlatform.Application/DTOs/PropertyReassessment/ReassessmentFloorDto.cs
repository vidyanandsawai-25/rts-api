namespace NtisPlatform.Application.DTOs.PropertyReassessment;

/// <summary>
/// One floor-wise row for the re-assessment screen (STEP 3 of the SQL). The same shape is used for
/// both the "New Floor Details" (from PTIS.PropertyDetails, with renter + RV-tax figures) and the
/// "Old Floor Details" (from PTIS.PropertyDetailsOld, where the renter/RV-only fields stay null/0).
/// </summary>
public class ReassessmentFloorDto
{
    /// <summary>"NEW" (New Survey) or "OLD" (Municipal Corp. Registration).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>"Unchanged" (same floor in both), "Added" (new survey only), or "Removed" (old registration only).</summary>
    public string ChangeStatus { get; set; } = string.Empty;

    /// <summary>
    /// For OLD rows only: the source old property ID (populated for MERGE scenarios to allow grouping
    /// floor details by which old property they came from). Null for NEW rows.
    /// </summary>
    public int? PropertyIdOld { get; set; }

    // ── Common columns (both new and old) ──────────────────────────────────
    public string? FloorCode { get; set; }
    public string? ConstructionCode { get; set; }

    /// <summary>Type-of-use description.</summary>
    public string? Description { get; set; }

    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }

    public double? CarpetAreaSqMeter { get; set; }
    public double? CarpetAreaSqFeet { get; set; }
    public double? BuiltupAreaSqMeter { get; set; }
    public double? BuiltupAreaSqFeet { get; set; }

    // ── New-survey only (renter, from PTIS.RenterMast) ─────────────────────
    public bool IsRenter { get; set; }
    public string? RenterName { get; set; }
    public string? TaxLiability { get; set; }
    public double? RentMonthly { get; set; }
    public double? FinalYearlyRent { get; set; }
    public string? FinancialYear { get; set; }

    // ── New-survey only (RV calculation, from PTIS.RVCalculationResults) ──
    public decimal? RateableValue { get; set; }
    public double? AnnualRentalValue { get; set; }
    public decimal? Depreciation { get; set; }
    public decimal? Maintenance { get; set; }
    public double? MonthlyRate { get; set; }
    public double? YearlyRate { get; set; }
    public double? YearlyRent { get; set; }

    // ── Certificates (separate fields for each type) ——————————————————————
    public string? OCCertificateNo { get; set; }
    public DateTime? OCCertificateIssueDate { get; set; }

    public string? CCCertificateNo { get; set; }
    public DateTime? CCCertificateIssueDate { get; set; }
}
