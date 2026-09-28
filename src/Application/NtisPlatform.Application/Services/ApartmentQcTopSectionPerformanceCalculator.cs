using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Computes additional revenue metrics shown on the ApartmentQC top-section panel.
/// </summary>
public sealed class ApartmentQcTopSectionPerformanceCalculator
{
    public AdditionalRevenueDto ComputeAdditionalRevenue(decimal? currentTax, decimal? retroTax, decimal? oldCurrentTax, decimal? pendingCurrent)
    {
        decimal? totalTax = null;
        if (currentTax != null || retroTax != null)
        {
            totalTax = (currentTax ?? 0m) + (retroTax ?? 0m);
        }

        // pendingDemand = retroTax + pendingCurrent
        decimal? pendingDemand = null;
        if (retroTax != null || pendingCurrent != null)
        {
            pendingDemand = (retroTax ?? 0m) + (pendingCurrent ?? 0m);
        }

        // totalDemand = currentTax + pendingDemand
        decimal? totalDemand = null;
        if (currentTax != null || pendingDemand != null)
        {
            totalDemand = (currentTax ?? 0m) + (pendingDemand ?? 0m);
        }

        decimal? differenceAmount = null;
        double? changePercent = null;

        if (totalTax.HasValue && oldCurrentTax.HasValue)
        {
            differenceAmount = totalTax.Value - oldCurrentTax.Value;
            if (oldCurrentTax.Value != 0)
            {
                changePercent = Math.Round((double)(differenceAmount.Value / oldCurrentTax.Value * 100m), 2);
            }
        }

        return new AdditionalRevenueDto
        {
            CurrentTax = currentTax,
            RetroTax = retroTax,
            TotalTax = totalTax,
            PendingCurrent = pendingCurrent,
            PendingDemand = pendingDemand,
            TotalDemand = totalDemand,
            Collection = null,
            TotalBalance = null,
            OldCurrentTax = oldCurrentTax,
            DifferenceAmount = differenceAmount,
            ChangePercent = changePercent
        };
    }
}
