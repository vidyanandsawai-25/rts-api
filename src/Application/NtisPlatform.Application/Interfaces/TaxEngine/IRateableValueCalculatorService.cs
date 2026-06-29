using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System.Collections.Generic;

namespace NtisPlatform.Application.Interfaces.TaxEngine
{
    public interface IRateableValueCalculatorService
    {
        PropertyTaxCalculationRVResultsEntity CalculateBaseValues(
            PropertyDetailsEntity detail,
            int financeYear,
            int taxZoneId,
            int? wardId,
            List<TypeOfUseEntity> typeOfUses,
            List<RateEntity> rates,
            List<DepreciationMasterEntity> depreciations,
            List<AssessmentYearRangeEntity> yearRanges,
            IReadOnlyList<RenterMastEntity> renters,
            decimal selectedArea,
            RateableValuePolicyOptions policyOptions,
            decimal? overrideRate = null,
            int? detailYearRangeRVId = null);
    }
}