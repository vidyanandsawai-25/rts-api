
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public static class RateableValueCalculator
    {
        /// <summary>
        /// Selects the appropriate rate per unit based on policy configuration.
        /// This is reusable because RV rule engine and RV calculator both need the same rate selection logic.
        /// </summary>
        /// <param name="rate">Rate entity containing both square meter and square feet rates.</param>
        /// <param name="policyOptions">Policy options that decide which unit to use.</param>
        /// <returns>Rate in configured unit, or 0 if rate is null.</returns>
        public static decimal GetRatePerUnit(
            RateEntity? rate,
            RateableValuePolicyOptions? policyOptions)
        {
            if (rate == null)
                return 0m;

            var options = policyOptions ?? RateableValuePolicyOptions.Default;

            return options.IsSqFeetUnit
                ? rate.RateSquareFeet ?? 0m
                : rate.RateSquareMeter ?? 0m;
        }
    }
}
