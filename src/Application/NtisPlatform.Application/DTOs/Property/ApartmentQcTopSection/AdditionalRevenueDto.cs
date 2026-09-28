namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Latest tax demand, retrospective tax, and historical pre-merge demand details.
/// </summary>
public sealed class AdditionalRevenueDto
{
    public decimal? CurrentTax { get; init; }
    public decimal? RetroTax { get; init; }
    public decimal? TotalTax { get; init; }
    public decimal? PendingCurrent { get; init; }
    public decimal? PendingDemand { get; init; }
    public decimal? TotalDemand { get; init; }
    public decimal? Collection { get; init; }
    public decimal? TotalBalance { get; init; }
    public decimal? OldCurrentTax { get; init; }
    public decimal? DifferenceAmount { get; init; }
    public double? ChangePercent { get; init; }
}
