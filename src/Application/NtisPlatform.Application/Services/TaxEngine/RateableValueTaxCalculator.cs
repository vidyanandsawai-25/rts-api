using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public sealed class TaxCalculationResult
    {
        public RVCalculationResultsEntity ResultsRow { get; set; } = null!;
        public RVCalculationTaxDetailsEntity TaxDetail { get; set; } = null!;
    }

    /// <summary>
    /// Static helper for applying tax calculations to rateable value base results
    /// </summary>
    public static class RateableValueTaxCalculator
    {
        /// <summary>
        /// Applies a specific tax to a base calculation result
        /// </summary>
        /// <param name="baseResult">The base calculation containing rateable value</param>
        /// <param name="tax">The tax master entity to apply</param>
        /// <param name="taxPercentage">The tax percentage configuration for this property type</param>
        /// <returns>A result containing both the results row and tax detail entity</returns>
        public static TaxCalculationResult ApplyTax(
            RVCalculationResultsEntity baseResult,
            TaxMasterEntity tax,
            TaxPercentageMasterRVEntity? taxPercentage)
        {
            if (baseResult == null) throw new ArgumentNullException(nameof(baseResult));
            if (tax == null) throw new ArgumentNullException(nameof(tax));

            var baseValue = taxPercentage != null && string.Equals(taxPercentage.BaseType, "ALV", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToDecimal(baseResult.AnnualRentalValue ?? 0d)
                : baseResult.RateableValue ?? 0m;
            var percentage = taxPercentage != null ? Convert.ToDecimal(taxPercentage.TaxPercentage) : 0m;
        
            var amount = Math.Round(baseValue * percentage / 100m, 2, MidpointRounding.AwayFromZero);

            var resultsRow = new RVCalculationResultsEntity
            {
                PropertyId = baseResult.PropertyId,
                PropertyDetailsId = baseResult.PropertyDetailsId,
                MonthlyRate = baseResult.MonthlyRate,
                YearlyRate = baseResult.YearlyRate,
                YearlyRent = baseResult.YearlyRent,
                Depreciation = baseResult.Depreciation,
                DepreciationPer = baseResult.DepreciationPer,
                AppliedOn = baseResult.AppliedOn,
                AnnualRentalValue = baseResult.AnnualRentalValue,
                Maintenance = baseResult.Maintenance,
                RateableValue = baseResult.RateableValue,
                TotalAreaSqMtr = baseResult.TotalAreaSqMtr,
                RAreaSqMtr = baseResult.RAreaSqMtr,
                CAreaSqlMtr = baseResult.CAreaSqlMtr
            };

            var taxDetail = new RVCalculationTaxDetailsEntity
            {
                TaxId = tax.Id,
                TaxPercentage = percentage,
                TaxAmount = amount
            };

            return new TaxCalculationResult
            {
                ResultsRow = resultsRow,
                TaxDetail = taxDetail
            };
        }
    }
}
