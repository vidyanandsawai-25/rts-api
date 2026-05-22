using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;

namespace NtisPlatform.Application.Services.TaxEngine
{
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
        /// <returns>A new result entity with tax calculations applied</returns>
        public static PropertyTaxCalculationRVResultsEntity ApplyTax(
            PropertyTaxCalculationRVResultsEntity baseResult,
            TaxMasterEntity tax,
            TaxPercentageMasterRVEntity? taxPercentage)
        {
            if (baseResult == null) throw new ArgumentNullException(nameof(baseResult));
            if (tax == null) throw new ArgumentNullException(nameof(tax));

            var rv = baseResult.RateableValue ?? 0m;
            var percentage = taxPercentage != null ? Convert.ToDecimal(taxPercentage.TaxPercentage) : 0m;
            var amount = Math.Round(rv * percentage / 100m, 0, MidpointRounding.AwayFromZero);

            return new PropertyTaxCalculationRVResultsEntity
            {
                PropertyId = baseResult.PropertyId,
                PropertyDetailsId = baseResult.PropertyDetailsId,
                MonthlyRate = baseResult.MonthlyRate,
                YearlyRate = baseResult.YearlyRate,
                YearlyRent = baseResult.YearlyRent,
                Depreciation = baseResult.Depreciation,
                AnnualRentalValue = baseResult.AnnualRentalValue,
                Maintenance = baseResult.Maintenance,
                RateableValue = baseResult.RateableValue,
                TotalAreaSqMtr = baseResult.TotalAreaSqMtr,
                RAreaSqMtr = baseResult.RAreaSqMtr,
                CAreaSqlMtr = baseResult.CAreaSqlMtr,
                TaxId = tax.Id,
                TaxPercentage = percentage,
                TaxAmount = amount
            };
        }
    }
}
