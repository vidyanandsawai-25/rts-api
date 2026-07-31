using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static NtisPlatform.Core.Constants.CapitalValueConstants;

namespace NtisPlatform.Application.Services.TaxEngine
{
    /// <summary>
    /// Orchestrates the end-to-end Rateable Value calculation for a single property.
    /// Persistence is delegated to <see cref="IRVPersistenceService"/>;
    /// context loading is delegated to <see cref="IPropertyContextLoaderService"/>;
    /// rule-engine application is delegated to <see cref="IRuleApplierService"/>.
    /// </summary>
    public class RateableValueService : IRateableValueService
    {
        private readonly ITaxMasterDataService _masterDataService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RateableValueService> _logger;
        private readonly IRepository<YearMasterEntity, int> _yearMasterRepo;
        private readonly IPolicyConfigurationService _policyConfigurationService;
        private readonly IRateableValueCalculatorService _rateableValueCalculatorService;
        private readonly IFinanceYearProvider _financeYearProvider;
        private readonly IPropertyContextLoaderService _propertyContextLoaderService;
        private readonly IRuleApplierService _ruleApplierService;
        private readonly IRVPersistenceService _persistenceService;
        private readonly TimeProvider _timeProvider;
        private readonly IRVCalculationCleanupService _rvCalculationCleanupService;

        public RateableValueService(
            ITaxMasterDataService masterDataService,
            IUnitOfWork unitOfWork,
            ILogger<RateableValueService> logger,
            IRepository<YearMasterEntity, int> yearMasterRepo,
            IPolicyConfigurationService policyConfigurationService,
            IRateableValueCalculatorService rateableValueCalculatorService,
            IFinanceYearProvider financeYearProvider,
            IPropertyContextLoaderService propertyContextLoaderService,
            IRuleApplierService ruleApplierService,
            IRVPersistenceService persistenceService,
            TimeProvider timeProvider,
            IRVCalculationCleanupService rvCalculationCleanupService)
        {
            _masterDataService = masterDataService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _yearMasterRepo = yearMasterRepo;
            _policyConfigurationService = policyConfigurationService;
            _rateableValueCalculatorService = rateableValueCalculatorService;
            _financeYearProvider = financeYearProvider;
            _propertyContextLoaderService = propertyContextLoaderService;
            _ruleApplierService = ruleApplierService;
            _persistenceService = persistenceService;
            _timeProvider = timeProvider;
            _rvCalculationCleanupService = rvCalculationCleanupService;
        }

        public async Task<RateableValueResponseDto> CalculateAndSaveAsync(int propertyId)
        {
            var operationStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting RV tax calculation for PropertyId={PropertyId}", propertyId);

            try
            {
                // 1. Resolve finance year
                int financeYear = _financeYearProvider.GetCurrentFinanceYear();

                // 2. Load property context (contains property, details, assessment, renters, occupancies, social attributes)
                var propertyContext = await _propertyContextLoaderService.LoadPropertyContextAsync(propertyId, financeYear);
                var property = propertyContext.Property;
                var details = propertyContext.Details;
                var propertyAssessment = propertyContext.PropertyAssessment;
                var renters = propertyContext.Renters;
                var occupancies = propertyContext.Occupancies;
                var certificates = propertyContext.Certificates;

                if (!details.Any())
                {
                    return new RateableValueResponseDto
                    {
                        PropertyId = propertyId,
                        FinanceYear = propertyContext.Parameters.FinanceYear,
                        TotalRateableValue = 0
                    };
                }

                var constructionYearValue = propertyContext.Parameters.ConstructionYearValue;
                var yearRangeRVId = propertyContext.Parameters.YearRangeRVId;

                LogMetric("Property.DetailCount", details.Count, new Dictionary<string, string>
                {
                    { "PropertyId", propertyId.ToString() }
                });

                // 3. Load master data — sequential; TaxMasterDataService uses IMemoryCache
                _logger.LogDebug("Loading master data for PropertyId={PropertyId}, WardId={WardId}",
                    propertyId, property.WardId);

                var typeOfUses = await _masterDataService.GetActiveTypeOfUsesAsync();
                var subTypeOfUses = await _masterDataService.GetActiveSubTypeOfUsesAsync();
                var floors = await _masterDataService.GetActiveFloorsAsync();
                var subFloors = await _masterDataService.GetActiveSubFloorsAsync();
                var constructionTypes = await _masterDataService.GetActiveConstructionTypesAsync();
                var rateSectionId = await _masterDataService.GetRateSectionIdForWardAsync(property.WardId);
                var rates = await _masterDataService.GetRatesForSectionAsync(rateSectionId);
                var depreciations = await _masterDataService.GetActiveDepreciationsAsync();
                var yearRanges = await _masterDataService.GetActiveYearRangesAsync();
                var activeTaxes = await _masterDataService.GetActiveTaxesAsync();

                _logger.LogInformation(
                    "Master data loaded for PropertyId={PropertyId}: " +
                    "TypeOfUses={TypeOfUseCount}, Taxes={TaxCount}, Rates={RateCount}, " +
                    "RateSectionId={RateSectionId}, WardId={WardId}",
                    propertyId, typeOfUses.Count, activeTaxes.Count, rates.Count,
                    rateSectionId, property.WardId);

                if (activeTaxes.Count == 0)
                    _logger.LogWarning(
                        "No active taxes found in TaxMaster for PropertyId={PropertyId}. " +
                        "No tax rows will be generated.", propertyId);

                if (rates.Count == 0)
                    _logger.LogWarning(
                        "No rates found for RateSectionId={RateSectionId} (WardId={WardId}). " +
                        "All base rates will be zero for PropertyId={PropertyId}.",
                        rateSectionId, property.WardId, propertyId);

                // 4. Load tax-related master data
                // Note: We now keep ALL tax percentages and filter per-detail using YearRangeRVIdForDetail
                // to support properties with mixed assessment years across details.
                var allTaxPercentages = await _masterDataService.GetActiveTaxPercentagesAsync();
                var educationTaxSlabs = await _masterDataService.GetActiveEducationTaxSlabsAsync();
                var employmentTaxSlabs = await _masterDataService.GetActiveEmploymentTaxSlabsAsync();

                _logger.LogInformation(
                    "Tax data for PropertyId={PropertyId}: " +
                    "PropertyYearRangeId={YearRangeId}, TaxPercentages(total)={AllPct}, " +
                    "EducationSlabs={EduSlabs}, EmploymentSlabs={EmpSlabs}",
                    propertyId, yearRangeRVId, allTaxPercentages.Count,
                    educationTaxSlabs.Count, employmentTaxSlabs.Count);

                // 5. Resolve finance year range and year master record
                var financeYearRange = yearRanges.FirstOrDefault(x =>
                    x.FromYear <= financeYear && x.ToYear >= financeYear && x.IsActive);

                CalculationValidator.CheckCondition(financeYearRange != null,
                    $"Finance year range not found for FinanceYear={financeYear}. " +
                    $"Tax calculation cannot proceed for PropertyId={propertyId}.");

                var yearMaster = await _yearMasterRepo.GetQueryable()
                    .FirstOrDefaultAsync(y => y.Year == financeYear && y.IsActive)
                    ?? throw new InvalidOperationException(
                        $"Year {financeYear} not found in YearMaster table");

                int yearMasterId = yearMaster.Id;

                // 6. Load policy configuration
                var policyDefaults = new Dictionary<string, string>
                {
                    { RateableValuePolicyConstants.RateableValueAreaType,                  RateableValuePolicyConstants.DefaultAreaType },
                    { RateableValuePolicyConstants.RateMasterAreaUnit,                     RateableValuePolicyConstants.DefaultAreaUnit },
                    { RateableValuePolicyConstants.RateMonthlyOrYearly,                    RateableValuePolicyConstants.DefaultRatePeriod },
                    { RateableValuePolicyConstants.EducationEmploymentTaxCalculationMethod, RateableValuePolicyConstants.DefaultEducationEmploymentTaxCalculationMethod },
                    { RateableValuePolicyConstants.MaintenanceRateKey,                     RateableValuePolicyConstants.DefaultMaintenanceRate }
                };
                var policyValues = await _policyConfigurationService.GetPolicyValuesAsync(policyDefaults);
                var policyOptions = RateableValuePolicyOptions.FromPolicies(policyValues, _logger);

                _logger.LogDebug(
                    "RV Policy: AreaType={AreaType}, AreaUnit={AreaUnit}, RatePeriod={RatePeriod}, " +
                    "EducationEmploymentTaxCalculationMethod={TaxCalcMethod}, Maintenance={Maintenance}%",
                    policyOptions.AreaType, policyOptions.AreaUnit, policyOptions.RatePeriod,
                    policyOptions.EducationEmploymentTaxCalculationMethod, policyOptions.MaintenanceRatePercent);

                // 7. Pre-compute selected areas for all details
                var selectedAreas = RateableValuePolicyHelper.GetSelectedAreasForProperty(details, policyOptions);

                _logger.LogDebug("Calculating base values for {DetailCount} property details, FinanceYear={FinanceYear}, YearMasterId={YearMasterId}",
                    details.Count, financeYear, yearMasterId);

                // 8. Calculate base values (sequential)
                var baseResultsCache = new Dictionary<int, RVCalculationResultsEntity>();
                var ruleTracesCache = new Dictionary<int, List<RuleApplicationTraceEntry>>();

                foreach (var detail in details)
                {
                    decimal? ruleAdjustedRate = null;
                    decimal? ruleAdjustedRent = null;
                    var detailAppliedRules = new List<RuleApplicationTraceEntry>();

                    var detailTypeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);

                    var detailYearRangeRVId = propertyContext.DetailYearRangeRVIdMap.TryGetValue(
                        detail.Id,
                        out var yearRangeId)
                        ? yearRangeId
                        : propertyContext.Parameters.YearRangeRVId;

                    if (detailTypeOfUse != null)
                    {
                        var masterRate = rates.FirstOrDefault(x =>
                            x.TaxZoneId == property.TaxZoneId &&
                            x.ConstructionTypeId == detail.ConstructionTypeId &&
                            x.TypeOfUseGroupId == detailTypeOfUse.TypeOfUseGroupId &&
                            x.YearRangeRVId == detailYearRangeRVId &&
                            x.IsActive);

                        decimal masterRatePerUnit = RateableValueCalculator.GetRatePerUnit(masterRate, policyOptions);

                        if (masterRate != null && masterRatePerUnit >= 0)
                        {
                            _logger.LogDebug(
                                "[RuleEngine-RV] Executing RV rules for PropertyDetailsId={DetailId}: MasterRate={MasterRate} ({Unit}) " +
                                "YearRangeRVId={YearRangeId}",
                                detail.Id, masterRatePerUnit, policyOptions.IsSqFeetUnit ? "sqft" : "sqm", detailYearRangeRVId);

                            // Execute Rate parameter rules
                            var clonedContext = propertyContext.CloneForDetail(detail, detailTypeOfUse);
                            var applierContext = new RuleApplierContext
                            {
                                PropertyContext = clonedContext,
                                InitialValue = masterRatePerUnit,
                                Category = "RV",
                                ValueKey = "Rate"
                            };

                            var ruleResult = await _ruleApplierService.ApplyRulesAsync(applierContext);
                            if (ruleResult.AppliedRules != null && ruleResult.AppliedRules.Any())
                            {
                                detailAppliedRules.AddRange(ruleResult.AppliedRules);
                            }

                            if (ruleResult.FinalValue != masterRatePerUnit)
                            {
                                ruleAdjustedRate = ruleResult.FinalValue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[RuleEngine-RV] Skipping rule execution for PropertyDetailsId={DetailId}: " +
                                "masterRate is null or zero. Using base rate.",
                                detail.Id);
                        }

                        // Execute Rent parameter rules if property floor is rented
                        if (detail.IsRenter == true && renters != null)
                        {
                            var renterRow = renters
                                .Where(r => r.PropertyDetailsId == detail.Id && r.IsActive && !r.MarkedForDeletion)
                                .OrderByDescending(r => r.CreatedDate)
                                .FirstOrDefault();

                            if (renterRow != null)
                            {
                                double yearlyRentValue = renterRow.FinalYearlyRent > 0
                                    ? renterRow.FinalYearlyRent.Value
                                    : ((renterRow.RentMonthly ?? 0d) * 12d);

                                if (yearlyRentValue > 0)
                                {
                                    var clonedRentContext = propertyContext.CloneForDetail(detail, detailTypeOfUse);
                                    var rentApplierContext = new RuleApplierContext
                                    {
                                        PropertyContext = clonedRentContext,
                                        InitialValue = (decimal)yearlyRentValue,
                                        Category = "RV",
                                        ValueKey = "Rent"
                                    };

                                    var rentRuleResult = await _ruleApplierService.ApplyRulesAsync(rentApplierContext);
                                    if (rentRuleResult.AppliedRules != null && rentRuleResult.AppliedRules.Any())
                                    {
                                        detailAppliedRules.AddRange(rentRuleResult.AppliedRules);
                                    }

                                    ruleAdjustedRent = rentRuleResult.FinalValue;
                                }
                            }
                        }

                        if (detailAppliedRules.Any())
                        {
                            ruleTracesCache[detail.Id] = detailAppliedRules;
                        }
                    }

                    var selectedArea = selectedAreas.TryGetValue(detail.Id, out var area) ? area : 0m;
                    baseResultsCache[detail.Id] = _rateableValueCalculatorService.CalculateBaseValues(
                        detail, financeYear, property.TaxZoneId, property.WardId,
                        typeOfUses, rates, depreciations, yearRanges, renters ?? new List<RenterMastEntity>(),
                        selectedArea, policyOptions, ruleAdjustedRate, detailYearRangeRVId, ruleAdjustedRent);
                }

                _logger.LogInformation(
                    "Base values calculated for PropertyId={PropertyId}: {CachedCount} detail(s). " +
                    "FinanceYear={FinanceYear}, FinanceYearRangeId={FinanceYearRangeId}",
                    propertyId, baseResultsCache.Count, financeYear, financeYearRange!.Id);

                // 9. Build standard tax rows (excluding education and employment)
                var regularTaxes = activeTaxes
                    .Where(t => !IsEducationTax(t) && !IsEmploymentTax(t))
                    .ToList();
                var eduTaxCount = activeTaxes.Count(IsEducationTax);
                var empTaxCount = activeTaxes.Count(IsEmploymentTax);

                _logger.LogInformation(
                    "Tax classification for PropertyId={PropertyId}: " +
                    "Regular={Regular}, Education={Edu}, Employment={Emp}. " +
                    "CategoryCode check: first tax CategoryCode='{Code}'",
                    propertyId, regularTaxes.Count, eduTaxCount, empTaxCount,
                    activeTaxes.FirstOrDefault()?.TaxCategoryMaster?.CategoryCode ?? "NULL(nav not loaded)");

                // Build one results row per PropertyDetailsId, and store per-tax amounts as separate tax-detail rows.
                var newResultsRows = new List<RVCalculationResultsEntity>();
                var newTaxDetailRows = new List<RVCalculationTaxDetailsEntity>();
                // Reuse the same results row across regular and special (education/employment) tax calculations.
                var detailResultsRowCache = new Dictionary<int, RVCalculationResultsEntity>();
                var now = _timeProvider.GetLocalNow().DateTime;

                foreach (var detail in details)
                {
                    var baseResult = baseResultsCache[detail.Id];

                    // Get detail's year range ID (same as base calculation loop)
                    var detailYearRangeRVId = propertyContext.DetailYearRangeRVIdMap.TryGetValue(
                        detail.Id,
                        out var yearRangeId)
                        ? yearRangeId
                        : propertyContext.Parameters.YearRangeRVId;

                    // Validate tax calculation prerequisites (same pattern as RateableValueCalculatorService)
                    if (detailYearRangeRVId == 0)
                    {
                        _logger.LogWarning(
                            "YearRangeRVId not found for PropertyDetailsId={PropertyDetailsId}, AssessmentYear={AssessmentYear}. Returning zero taxes.",
                            detail.Id, detail.AssessmentYear);
                        // Skip tax rows for this detail - it will show with zero values from CreateZeroResult in base calculation
                    }
                    else
                    {
                        // Only process tax rows if year range ID is valid
                        foreach (var tax in regularTaxes)
                        {
                            // Filter tax percentages by the detail's assessment year range (or property-level fallback)
                            var taxPct = allTaxPercentages.FirstOrDefault(x =>
                                x.YearRangeRVId == detailYearRangeRVId &&
                                x.TaxId == tax.Id &&
                                x.TypeOfUseId == detail.TypeOfUseId);

                            if (taxPct == null)
                            {
                                _logger.LogWarning(
                                    "TaxPercentage not found for TaxId={TaxId} ({TaxCode}), " +
                                    "TypeOfUseId={TypeOfUseId}, YearRangeRVId={YearRangeId}. Returning zero tax.",
                                    tax.Id, tax.TaxCode, detail.TypeOfUseId, detailYearRangeRVId);
                                continue;  // Skip if no tax percentage found
                            }

                            var calculationResult = RateableValueTaxCalculator.ApplyTax(baseResult, tax, taxPct);

                            // Create results row ONCE per detail using shared cache
                            if (!detailResultsRowCache.ContainsKey(detail.Id))
                            {
                                var detailResultsRow = calculationResult.ResultsRow;
                                detailResultsRow.IsActive = true;
                                detailResultsRow.MarkedForDeletion = false;
                                detailResultsRow.CreatedDate = now;
                                detailResultsRow.UpdatedDate = now;
                                detailResultsRowCache[detail.Id] = detailResultsRow;
                                newResultsRows.Add(detailResultsRow);
                            }

                            // All tax details for this detail reference the SAME results row from shared cache
                            calculationResult.TaxDetail.RVCalculationResults = detailResultsRowCache[detail.Id];
                            calculationResult.TaxDetail.IsActive = true;
                            calculationResult.TaxDetail.MarkedForDeletion = false;
                            calculationResult.TaxDetail.MarkedForDeletionDate = null;
                            calculationResult.TaxDetail.CreatedDate = now;
                            calculationResult.TaxDetail.UpdatedDate = now;
                            newTaxDetailRows.Add(calculationResult.TaxDetail);
                        }
                    }
                }

                // 10. Education and Employment tax — grouped by property type (R/C)
                // Only include details that have valid year range IDs (exclude detailYearRangeRVId == 0)
                var validDetailsForSpecialTax = details
                    .Where(d =>
                    {
                        var detailYearRangeRVId = propertyContext.DetailYearRangeRVIdMap.TryGetValue(
                            d.Id,
                            out var yearRangeId)
                            ? yearRangeId
                            : propertyContext.Parameters.YearRangeRVId;

                        return detailYearRangeRVId != 0;  // Exclude zero (AssessmentYear not found)
                    })
                    .ToList();

                // Get distinct property types, then normalize: C and I are grouped together as "C"
                var propertyTypesRaw = validDetailsForSpecialTax
                    .Select(d => typeOfUses.FirstOrDefault(x => x.Id == d.TypeOfUseId)?.Type)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .ToList();

                // Normalize: C and I types are grouped together
                var propertyTypesForTax = propertyTypesRaw
                    .Select(t => (t?.Equals("I", StringComparison.OrdinalIgnoreCase) ?? false) ? "C" : t)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .ToList();

                var educationTaxMaster = activeTaxes.FirstOrDefault(IsEducationTax);
                var employmentTaxMaster = activeTaxes.FirstOrDefault(IsEmploymentTax);

                foreach (var propType in propertyTypesForTax)
                {
                    // Include details that match the property type OR (if type is C, include both C and I)
                    var detailsOfType = validDetailsForSpecialTax
                        .Where(d =>
                        {
                            var detailType = typeOfUses.FirstOrDefault(x => x.Id == d.TypeOfUseId)?.Type;
                            if (string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase))
                            {
                                // For C type, include both C and I
                                return string.Equals(detailType, "C", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(detailType, "I", StringComparison.OrdinalIgnoreCase);
                            }
                            else
                            {
                                // For other types, exact match
                                return string.Equals(detailType, propType, StringComparison.OrdinalIgnoreCase);
                            }
                        })
                        .ToList();

                    // Calculate education/employment tax base based on policy: RV or ALV (default)
                    bool isCalculatedOnRV = string.Equals(
                        policyOptions.EducationEmploymentTaxCalculationMethod,
                        RateableValuePolicyConstants.RV,
                        StringComparison.OrdinalIgnoreCase);

                    decimal taxBase = isCalculatedOnRV
                        ? detailsOfType.Sum(d => baseResultsCache[d.Id].RateableValue ?? 0m)
                        : detailsOfType.Sum(d => (decimal)(baseResultsCache[d.Id].AnnualRentalValue ?? 0d));

                    _logger.LogInformation(
                        "[EducationEmploymentTax] PropertyType={PropertyType}, Policy={PolicyValue}, " +
                        "IsCalculatedOnRV={IsCalculatedOnRV}, TaxBase={TaxBase}",
                        propType, policyOptions.EducationEmploymentTaxCalculationMethod, isCalculatedOnRV, taxBase);

                    if (educationTaxMaster != null)
                    {
                        var slab = educationTaxSlabs.FirstOrDefault(x =>
                            IsSlabMatch(taxBase, x.MinAmount, x.MaxAmount) &&
                            (string.IsNullOrWhiteSpace(x.Type) ||
                             string.Equals(x.Type, propType, StringComparison.OrdinalIgnoreCase)));

                        if (slab != null)
                        {
                            var pct = slab.Rate ?? 0m;
                            var amt = Math.Round(taxBase * pct / 100m, 0, MidpointRounding.AwayFromZero);
                            foreach (var d in detailsOfType)
                            {
                                var calculationResult = BuildSpecialTaxRow(
                                    baseResultsCache[d.Id], educationTaxMaster, propType!, pct, amt,
                                    isEducation: true, now);

                                // Create ResultsRow ONCE per detail, reuse for all taxes of this detail
                                if (!detailResultsRowCache.ContainsKey(d.Id))
                                {
                                    var resultRow = calculationResult.ResultsRow;
                                    resultRow.IsActive = true;
                                    resultRow.MarkedForDeletion = false;
                                    resultRow.CreatedDate = now;
                                    resultRow.UpdatedDate = now;
                                    detailResultsRowCache[d.Id] = resultRow;
                                    newResultsRows.Add(resultRow);
                                }

                                // Set education tax amount on cached ResultsRow based on property type
                                var cachedRow = detailResultsRowCache[d.Id];
                                if (string.Equals(propType, "R", StringComparison.OrdinalIgnoreCase))
                                    cachedRow.REducationTax = amt;
                                else if (string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase))
                                    cachedRow.CEducationTax = amt;

                                // Reuse the cached ResultsRow instance for all tax details of this detail
                                calculationResult.TaxDetail.RVCalculationResults = cachedRow;
                                newTaxDetailRows.Add(calculationResult.TaxDetail);
                            }
                        }
                    }

                    if (employmentTaxMaster != null)
                    {
                        var slab = employmentTaxSlabs.FirstOrDefault(x =>
                            IsSlabMatch(taxBase, x.MinAmount, x.MaxAmount) &&
                            (string.IsNullOrWhiteSpace(x.Type) ||
                             string.Equals(x.Type, propType, StringComparison.OrdinalIgnoreCase)));

                        if (slab != null)
                        {
                            var pct = slab.Rate ?? 0m;
                            var amt = Math.Round(taxBase * pct / 100m, 0, MidpointRounding.AwayFromZero);
                            foreach (var d in detailsOfType)
                            {
                                var calculationResult = BuildSpecialTaxRow(
                                    baseResultsCache[d.Id], employmentTaxMaster, propType!, pct, amt,
                                    isEducation: false, now);

                                // Create ResultsRow ONCE per detail, reuse for all taxes of this detail
                                if (!detailResultsRowCache.ContainsKey(d.Id))
                                {
                                    var resultRow = calculationResult.ResultsRow;
                                    resultRow.IsActive = true;
                                    resultRow.MarkedForDeletion = false;
                                    resultRow.CreatedDate = now;
                                    resultRow.UpdatedDate = now;
                                    detailResultsRowCache[d.Id] = resultRow;
                                    newResultsRows.Add(resultRow);
                                }

                                // Employment tax is only applicable for C (Commercial) type properties
                                if (string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase))
                                {
                                    var cachedRow = detailResultsRowCache[d.Id];
                                    cachedRow.CEmploymentTax = amt;

                                    // Reuse the cached ResultsRow instance for all tax details of this detail
                                    calculationResult.TaxDetail.RVCalculationResults = cachedRow;
                                    newTaxDetailRows.Add(calculationResult.TaxDetail);
                                }
                            }
                        }
                    }
                }

                // 11. Total RV across all details
                // Calculate total RV from unique details (first result per detail) to match response mapper
                // Group by PropertyDetailsId once to avoid O(n²) complexity from repeated Where scans
                decimal totalRv = newResultsRows
                    .GroupBy(r => r.PropertyDetailsId)
                    .Sum(g => g.FirstOrDefault()?.RateableValue ?? 0m);

                _logger.LogInformation(
                    "Row summary for PropertyId={PropertyId}: ResultsRows={Results}, TaxDetailRows={TaxDetails}, TotalRV={TotalRv}",
                    propertyId, newResultsRows.Count, newTaxDetailRows.Count, totalRv);

                if (newResultsRows.Count == 0)
                    _logger.LogWarning(
                        "PropertyId={PropertyId}: 0 results rows generated. " +
                        "Likely causes: no active taxes, no matching TaxPercentages for YearRangeId={YearRangeId}, " +
                        "or no rates for RateSectionId={RateSectionId}.",
                        propertyId, yearRangeRVId, rateSectionId);

                // Persist results rows and tax-detail rows in a single transaction.
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    await _persistenceService.ReplaceExistingResultsAsync(propertyId, newResultsRows, newTaxDetailRows);

                    _logger.LogInformation(
                        "Persisting {ResultsRowCount} results rows and {TaxDetailCount} tax detail rows for PropertyId={PropertyId}",
                        newResultsRows.Count, newTaxDetailRows.Count, propertyId);

                    // Save rule application trace logs
                    foreach (var detail in details)
                    {
                        var appliedRules = ruleTracesCache.TryGetValue(detail.Id, out var traceList)
                            ? traceList
                            : new List<RuleApplicationTraceEntry>();

                        await _persistenceService.SaveRuleApplicationLogAsync(
                            propertyId,
                            financeYear,
                            detail.Id,
                            appliedRules,
                            "RV",
                            now);
                    }

                    var savedPolicyRecords = await _persistenceService.SavePolicyAndTransmastRVAsync(
                        propertyId, financeYear, yearMasterId, newResultsRows, newTaxDetailRows, totalRv,
                        educationTaxMaster?.Id, employmentTaxMaster?.Id);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation(
                        "Policy and TransmastRV rows saved for PropertyId={PropertyId}", propertyId);

                    // 13. Build response from in-memory data
                    var detailIds = details.Select(d => d.Id).ToList();
                    var taxMasterCache = new TaxGetterCache<TaxMasterEntity>(
                        activeTaxes,
                        x => x.Id,
                        x => string.IsNullOrWhiteSpace(x.TaxNameAlias) ? x.TaxName : x.TaxNameAlias!,
                        x => x.TaxCategoryMaster?.CategoryCode ?? string.Empty);  // Pass category code for filtering

                    var response = RateableValueResponseMapper.Map(
                        propertyId, financeYear, details, newResultsRows, newTaxDetailRows, savedPolicyRecords,
                        floors, constructionTypes, typeOfUses, subTypeOfUses, subFloors,
                        renters ?? new List<RenterMastEntity>(), occupancies, certificates, taxMasterCache);

                    LogMetric("TaxCalculation.TotalRV", (double)response.TotalRateableValue, new Dictionary<string, string>
                        { { "PropertyId", propertyId.ToString() } });

                    _logger.LogInformation(
                        "RV calculation completed for PropertyId={PropertyId}, TotalRV={TotalRV}, Duration={DurationMs}ms",
                        propertyId, response.TotalRateableValue,
                        operationStopwatch.ElapsedMilliseconds);

                    return response;
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex,
                        "Failed to save RV results for PropertyId={PropertyId}. Transaction rolled back.",
                        propertyId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                operationStopwatch.Stop();

                LogMetric("TaxCalculation.Failed", 1, new Dictionary<string, string>
                {
                    { "PropertyId",  propertyId.ToString() },
                    { "ErrorType",   ex.GetType().Name },
                    { "DurationMs",  operationStopwatch.ElapsedMilliseconds.ToString() }
                });

                _logger.LogError(ex,
                    "RV calculation failed for PropertyId={PropertyId} after {DurationMs}ms",
                    propertyId, operationStopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        private static bool IsEducationTax(TaxMasterEntity tax) =>
            string.Equals(
                tax.TaxCategoryMaster?.CategoryCode, "EDU",
                StringComparison.OrdinalIgnoreCase);

        private static bool IsEmploymentTax(TaxMasterEntity tax) =>
            string.Equals(
                tax.TaxCategoryMaster?.CategoryCode, "EMP",
                StringComparison.OrdinalIgnoreCase);

        private static bool IsSlabMatch(decimal value, decimal? min, decimal? max)
        {
            var minOk = !min.HasValue || value >= min.Value;
            var maxOk = !max.HasValue || value <= max.Value;
            return minOk && maxOk;
        }

        /// <summary>
        /// Updated to return TaxCalculationResult with separated entities.
        /// Builds both the results row and tax detail row for education/employment taxes.
        /// </summary>
        private static TaxCalculationResult BuildSpecialTaxRow(
            RVCalculationResultsEntity baseResult,
            TaxMasterEntity taxMaster,
            string propType,
            decimal percentage,
            decimal amount,
            bool isEducation,
            DateTime now)
        {
            bool isR = string.Equals(propType, "R", StringComparison.OrdinalIgnoreCase);
            bool isC = string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase);

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
                RAreaSqMtr = isR ? baseResult.TotalAreaSqMtr : 0d,
                CAreaSqlMtr = isC ? baseResult.TotalAreaSqMtr : 0d
            };

            var taxDetail = new RVCalculationTaxDetailsEntity
            {
                TaxId = taxMaster.Id,
                TaxPercentage = percentage,
                TaxAmount = amount,
                IsActive = true,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null,
                CreatedDate = now,
                UpdatedDate = now
            };

            return new TaxCalculationResult
            {
                ResultsRow = resultsRow,
                TaxDetail = taxDetail
            };
        }

        private void LogMetric(string metricName, double value, Dictionary<string, string>? properties = null)
        {
            _logger.LogInformation("[Metric] {MetricName} = {Value} {@Properties}",
                metricName, value, properties ?? new Dictionary<string, string>());
        }
    }
}
