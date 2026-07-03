namespace NtisPlatform.Application.DTOs.PropertyReassessment;

/// <summary>
/// One tax head in the "Tax Details &amp; Reassessment Summary" comparison (STEP 4 of the SQL).
/// Replaces the legacy dynamic PIVOT: instead of tax names as columns, each active tax head is a
/// row carrying its old (TransMastOld) and new (TransMast) amounts. Ordered by <see cref="DisplayOrder"/>.
/// </summary>
public class ReassessmentTaxHeadDto
{
    public int TaxId { get; set; }
    public string TaxName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>Old amount from PTIS.TransMastOld (0 when no old transaction exists).</summary>
    public decimal OldAmount { get; set; }

    /// <summary>New amount from PTIS.TransMast (0 when no new transaction exists).</summary>
    public decimal NewAmount { get; set; }
}
