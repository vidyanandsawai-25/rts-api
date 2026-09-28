namespace NtisPlatform.Application.DTOs.RetrospectiveTax;

/// <summary>
/// Read-only "what would this property's total tax have been for this finance year" result,
/// used by the retrospective tax calculation engine to price a single retrospective year. Never
/// persists anything — unlike <see cref="Interfaces.IRateableValueService.CalculateAndSaveAsync"/>,
/// which replaces the property's real, current RV/billing records.
/// </summary>
public class RetrospectiveRatePreviewDto
{
    public int PropertyId { get; set; }
    public int FinanceYear { get; set; }
    public decimal TotalRateableValue { get; set; }
    public decimal TotalTaxAmount { get; set; }
}
