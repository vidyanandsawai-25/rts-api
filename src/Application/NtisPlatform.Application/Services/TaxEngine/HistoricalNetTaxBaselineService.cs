using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// See <see cref="IHistoricalNetTaxBaselineService"/>. Deliberately does NOT reuse
/// PropertyContextLoaderService.LoadPropertyContextAsync -- that loader bulk soft-deletes existing
/// RVCalculationResults/PolicyTaxDetails/TransMast rows as a side effect of loading context for a
/// property with zero active details, which is never acceptable for a read-only preview. This
/// service loads only what it needs, directly, with no writes anywhere in its path.
///
/// Known, accepted simplifications (out of scope for TAXATION_RATE_MODE/TAX_PERCENTAGE_MODE, which
/// only govern rate-per-area and percentage-of-RV):
/// - Rule-engine rate/rent/maintenance adjustments (RuleApplierService) are not replayed here --
///   this uses the raw RateEntity/TaxPercentageMasterRV values and RateableValuePolicyOptions.Default
///   (or the corporation's configured area/rate-period/maintenance policy values) directly.
/// - Education/Employment tax (TaxCategoryMaster.CategoryCode "EDU"/"EMP") is amount-slab-driven,
///   not rate- or percentage-driven, so it is excluded from this recomputation entirely -- it is
///   not affected by either mode and keeps whatever value the current pipeline already gave it.
/// - Historical tax-percentage lookups use the property's CURRENT TypeOfUseId for every year (no
///   type-of-use history exists) -- an explicitly accepted limitation.
/// </summary>
public sealed class HistoricalNetTaxBaselineService : IHistoricalNetTaxBaselineService
{
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<RenterMastEntity, int> _renterRepository;
    private readonly ITaxMasterDataService _masterDataService;
    private readonly IRateableValueCalculatorService _rateableValueCalculatorService;
    private readonly IPolicyConfigurationService _policyConfigurationService;

    public HistoricalNetTaxBaselineService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<RenterMastEntity, int> renterRepository,
        ITaxMasterDataService masterDataService,
        IRateableValueCalculatorService rateableValueCalculatorService,
        IPolicyConfigurationService policyConfigurationService)
    {
        _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
        _propertyDetailsRepository = propertyDetailsRepository ?? throw new ArgumentNullException(nameof(propertyDetailsRepository));
        _renterRepository = renterRepository ?? throw new ArgumentNullException(nameof(renterRepository));
        _masterDataService = masterDataService ?? throw new ArgumentNullException(nameof(masterDataService));
        _rateableValueCalculatorService = rateableValueCalculatorService ?? throw new ArgumentNullException(nameof(rateableValueCalculatorService));
        _policyConfigurationService = policyConfigurationService ?? throw new ArgumentNullException(nameof(policyConfigurationService));
    }

    public async Task<(decimal AnnualNetTax, decimal GeneralTaxPortion)?> ComputeBaselineAsync(
        int propertyId,
        int rateFinanceYear,
        int percentageFinanceYear,
        decimal? fixedTaxPercentage,
        CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);
        if (property == null)
        {
            return null;
        }

        var details = await _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(d => d.PropertyId == propertyId && d.IsActive && !d.MarkedForDeletion)
            .ToListAsync(cancellationToken);
        if (details.Count == 0)
        {
            return null;
        }

        var yearRanges = await _masterDataService.GetActiveYearRangesAsync();
        var rateYearRange = yearRanges.FirstOrDefault(y => y.FromYear <= rateFinanceYear && y.ToYear >= rateFinanceYear);
        if (rateYearRange == null)
        {
            return null;
        }
        var percentageYearRange = fixedTaxPercentage.HasValue
            ? null
            : yearRanges.FirstOrDefault(y => y.FromYear <= percentageFinanceYear && y.ToYear >= percentageFinanceYear);
        if (!fixedTaxPercentage.HasValue && percentageYearRange == null)
        {
            return null;
        }

        var typeOfUses = await _masterDataService.GetActiveTypeOfUsesAsync();
        var rateSectionId = await _masterDataService.GetRateSectionIdForWardAsync(property.WardId);
        var rates = await _masterDataService.GetRatesForSectionAsync(rateSectionId);
        var depreciations = await _masterDataService.GetActiveDepreciationsAsync();
        var activeTaxes = await _masterDataService.GetActiveTaxesAsync();
        var taxPercentages = fixedTaxPercentage.HasValue
            ? new List<TaxPercentageMasterRVEntity>()
            : await _masterDataService.GetActiveTaxPercentagesAsync();

        var renters = await _renterRepository.GetQueryable()
            .AsNoTracking()
            .Where(r => r.IsActive && !r.MarkedForDeletion && details.Select(d => d.Id).Contains(r.PropertyDetailsId))
            .ToListAsync(cancellationToken);

        var policyDefaults = new Dictionary<string, string>
        {
            { RateableValuePolicyConstants.RateableValueAreaType, RateableValuePolicyConstants.DefaultAreaType },
            { RateableValuePolicyConstants.RateMasterAreaUnit, RateableValuePolicyConstants.DefaultAreaUnit },
            { RateableValuePolicyConstants.RateMonthlyOrYearly, RateableValuePolicyConstants.DefaultRatePeriod },
            { RateableValuePolicyConstants.MaintenanceRateKey, RateableValuePolicyConstants.DefaultMaintenanceRate }
        };
        var policyValues = await _policyConfigurationService.GetPolicyValuesAsync(policyDefaults, cancellationToken);
        var policyOptions = RateableValuePolicyOptions.FromPolicies(policyValues);

        var selectedAreas = RateableValuePolicyHelper.GetSelectedAreasForProperty(details, policyOptions);

        // Regular (non-Education/Employment) taxes only -- see class doc comment.
        var regularTaxes = activeTaxes.Where(t => !IsEducationTax(t) && !IsEmploymentTax(t)).ToList();
        if (regularTaxes.Count == 0)
        {
            return null;
        }

        var totalsByTaxId = new Dictionary<int, decimal>();

        foreach (var detail in details)
        {
            selectedAreas.TryGetValue(detail.Id, out var selectedArea);

            var baseResult = _rateableValueCalculatorService.CalculateBaseValues(
                detail, rateFinanceYear, property.TaxZoneId, property.WardId,
                typeOfUses, rates, depreciations, yearRanges, renters, selectedArea, policyOptions,
                detailYearRangeRVId: rateYearRange.Id);

            foreach (var tax in regularTaxes)
            {
                var taxPercentage = fixedTaxPercentage.HasValue
                    ? new TaxPercentageMasterRVEntity
                    {
                        TaxId = tax.Id,
                        TypeOfUseId = detail.TypeOfUseId,
                        YearRangeRVId = rateYearRange.Id,
                        TaxPercentage = fixedTaxPercentage.Value,
                        BaseType = "RV"
                    }
                    : taxPercentages.FirstOrDefault(x =>
                        x.YearRangeRVId == percentageYearRange!.Id &&
                        x.TaxId == tax.Id &&
                        x.TypeOfUseId == detail.TypeOfUseId);

                var result = RateableValueTaxCalculator.ApplyTax(baseResult, tax, taxPercentage);
                totalsByTaxId.TryGetValue(tax.Id, out var existing);
                totalsByTaxId[tax.Id] = existing + (result.TaxDetail.TaxAmount ?? 0m);
            }
        }

        var annualNetTax = totalsByTaxId.Values.Sum();

        var generalTax = regularTaxes.FirstOrDefault(t =>
            t.TaxName.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
            t.TaxCode.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
            t.TaxName.Contains("General", StringComparison.OrdinalIgnoreCase));
        var generalTaxPortion = generalTax != null && totalsByTaxId.TryGetValue(generalTax.Id, out var gt)
            ? gt
            : annualNetTax * 0.6m;

        return (annualNetTax, generalTaxPortion);
    }

    private static bool IsEducationTax(TaxMasterEntity tax) =>
        string.Equals(tax.TaxCategoryMaster?.CategoryCode, "EDU", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmploymentTax(TaxMasterEntity tax) =>
        string.Equals(tax.TaxCategoryMaster?.CategoryCode, "EMP", StringComparison.OrdinalIgnoreCase);
}
