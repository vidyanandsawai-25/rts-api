using System;
using System.Collections.Generic;
using System.Text;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces
{
    public interface IRateableValueService
    {
        /// <param name="propertyId">The property to calculate Rateable Value for.</param>
        /// <param name="forceRecalculate">
        /// When true, bypasses the input-signature fast path and always recalculates, even if
        /// nothing detectable has changed since the last run. Defaults to false.
        /// </param>
        Task<RateableValueResponseDto> CalculateAndSaveAsync(int propertyId, bool forceRecalculate = false);

        /// <summary>
        /// Read-only: computes what this property's total tax would be for
        /// <paramref name="financeYear"/> (any year, past or current) using the same rate/tax%
        /// master data and rule-engine logic as <see cref="CalculateAndSaveAsync"/> — but never
        /// persists anything (no RVCalculationResults/TaxDetails/TransMast writes). Used by the
        /// retrospective tax calculation engine to price a historical year without touching the
        /// property's real, current RV/billing records.
        /// </summary>
        Task<RetrospectiveRatePreviewDto> PreviewTotalTaxAsync(int propertyId, int financeYear);
    }
}
