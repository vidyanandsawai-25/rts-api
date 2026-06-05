using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RuleEngine;
using NtisPlatform.Application.Services.RuleEngine.Effects;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace NtisPlatform.Application.Services.TaxEngine
{
    /// <summary>
    /// Service for calculating and persisting rateable value tax calculations
    /// </summary>
    public class RateableValueService : IRateableValueService
    {
        private readonly IRepository<PropertyEntity, int> _propertyRepo;
        private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepo;
        private readonly IRepository<PropertyTaxCalculationRVResultsEntity, int> _taxResultsRepo;
        private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxRepo;
        private readonly IRepository<RenterMastEntity, int> _renterRepo;
        private readonly IRepository<PropertyOccupancyDetailsEntity, int> _occupancyRepo;
        private readonly IRepository<PropertyMastOldEntity, int> _oldPropertyRepo;
        private readonly IRepository<PropertySocialDetailsEntity, int> _propertySocialDetailsRepo;
        private readonly IRepository<PropertyAssessmentEntity, int> _propertyAssessmentRepo;
        private readonly TaxMasterDataService _masterDataService;
        private readonly IRuleExecutionService _ruleExecutionService;
        private readonly IEnumerable<IRuleEffectApplicator> _effectApplicators;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RateableValueService> _logger;
        private readonly IRepository<TransMastRVEntity, int> _transmastRVRepo;
        private readonly IRepository<YearMasterEntity, int> _yearMasterRepo;
        private readonly IPolicyConfigurationService _policyConfigurationService;

        public RateableValueService(
            IRepository<PropertyEntity, int> propertyRepo,
            IRepository<PropertyDetailsEntity, int> propertyDetailsRepo,
            IRepository<PropertyTaxCalculationRVResultsEntity, int> taxResultsRepo,
            IRepository<PolicyTaxDetailsEntity, int> policyTaxRepo,
            IRepository<RenterMastEntity, int> renterRepo,
            IRepository<PropertyOccupancyDetailsEntity, int> occupancyRepo,
            IRepository<PropertyMastOldEntity, int> oldPropertyRepo,
            IRepository<PropertySocialDetailsEntity, int> propertySocialDetailsRepo,
            IRepository<PropertyAssessmentEntity, int> propertyAssessmentRepo,
            TaxMasterDataService masterDataService,
            IRuleExecutionService ruleExecutionService,
            IEnumerable<IRuleEffectApplicator> effectApplicators,
            IUnitOfWork unitOfWork,
            ILogger<RateableValueService> logger,
            IRepository<TransMastRVEntity, int> transmastRVRepo,
            IRepository<YearMasterEntity, int> yearMasterRepo,
            IPolicyConfigurationService policyConfigurationService)
        {
            _propertyRepo = propertyRepo;
            _propertyDetailsRepo = propertyDetailsRepo;
            _taxResultsRepo = taxResultsRepo;
            _policyTaxRepo = policyTaxRepo;
            _renterRepo = renterRepo;
            _occupancyRepo = occupancyRepo;
            _oldPropertyRepo = oldPropertyRepo;
            _propertySocialDetailsRepo = propertySocialDetailsRepo;
            _propertyAssessmentRepo = propertyAssessmentRepo;
            _masterDataService = masterDataService;
            _ruleExecutionService = ruleExecutionService;
            _effectApplicators = effectApplicators;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _transmastRVRepo = transmastRVRepo;
            _yearMasterRepo = yearMasterRepo;
            _policyConfigurationService = policyConfigurationService;
        }

        public async Task<RateableValueResponseDto> CalculateAndSaveAsync(int propertyId)
        {
            // P3: Start operation tracking
            var operationStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting RV tax calculation for PropertyId={PropertyId}", propertyId);

            try
            {
                // 1. Validation - Get property and assessment
                var property = await _propertyRepo.GetQueryable()
                 .AsNoTracking()
                 .FirstOrDefaultAsync(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                 ?? throw new InvalidOperationException($"Property not found for PropertyId={propertyId}");

                // Load PropertyAssessmentEntity for OwnerType context in rule engine
                var propertyAssessment = _propertyAssessmentRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();

                // ✅ FIX: Log warning if PropertyAssessment is missing
                if (propertyAssessment == null)
                {
                    _logger.LogWarning(
                        "⚠️ PropertyAssessmentEntity not found for PropertyId={PropertyId}. " +
                        "OwnerType will default to 0 in rule engine. This may cause incorrect rule matching.",
                        propertyId);
                }

                // ✅ FIX: Load hasLift ONCE per property (not per detail) to avoid N+1 query
                var hasLift = _propertySocialDetailsRepo.GetQueryable()
                    .Include(psd => psd.SocialAttribute)
                    .Any(psd => psd.PropertyId == propertyId &&
                                     psd.SocialAttribute != null &&
                                     psd.SocialAttribute.SocialAttributeCode == "HAS_LIFT" &&
                                     psd.IsActive);

                _logger.LogDebug("Property {PropertyId} has lift: {HasLift}", propertyId, hasLift);

                // 2. Get property details
                var details = await _propertyDetailsRepo.GetQueryable()
                   .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                   .OrderBy(x => x.Id)
                      .ToListAsync();

                CalculationValidator.CheckCondition(details.Any(), $"PropertyDetails not found for PropertyId={propertyId}");

                // P3: Log property complexity metric
                LogMetric("Property.DetailCount", details.Count, new Dictionary<string, string>
            {
                { "PropertyId", propertyId.ToString() }
            });

                // 3. Load all master data
                _logger.LogDebug("Loading master data for PropertyId={PropertyId}, WardId={WardId}", propertyId, property.WardId);
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
                _logger.LogDebug("Master data loaded: {TypeOfUseCount} TypeOfUses, {TaxCount} Taxes, {RateCount} Rates",
                    typeOfUses.Count, activeTaxes.Count, rates.Count);

                // 4. Get renters
                var detailIds = details.Select(d => d.Id).ToList();
                var renters = await _renterRepo.GetQueryable()
                    .Where(x => detailIds.Contains(x.PropertyDetailsId) && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync();

                // 5. Validate construction year
                var constructionYear = details.FirstOrDefault()?.ConstructionYear;
                CalculationValidator.CheckCondition(!string.IsNullOrWhiteSpace(constructionYear), $"ConstructionYear not found for PropertyId={propertyId}");
                CalculationValidator.CheckCondition(int.TryParse(constructionYear, out int constructionYearValue), $"Invalid ConstructionYear value '{constructionYear}' for PropertyId={propertyId}");

                var yearRange = yearRanges.FirstOrDefault(x => x.FromYear <= constructionYearValue && x.ToYear >= constructionYearValue)
                    ?? throw new InvalidOperationException($"Assessment year range not found for constructionYear={constructionYearValue}");

                // 6. Load tax-related master data
                var taxPercentages = (await _masterDataService.GetActiveTaxPercentagesAsync())
                    .Where(x => x.YearRangeRVId == yearRange.Id)
                    .ToList();
                var educationTaxSlabs = await _masterDataService.GetActiveEducationTaxSlabsAsync();
                var employmentTaxSlabs = await _masterDataService.GetActiveEmploymentTaxSlabsAsync();

                // 7. Pre-calculate base values for all details (cache to avoid redundant computation)
                int financeYear = GetFinanceYear();
                _logger.LogDebug("Calculating base values for {DetailCount} property details, FinanceYear={FinanceYear}",
                    details.Count, financeYear);

                // P2: Use thread-safe ConcurrentDictionary for parallel processing
                var baseResultsCache = new ConcurrentDictionary<int, PropertyTaxCalculationRVResultsEntity>();

                // Resolve the finance-year rate range once (used to look up rates for rule engine input)
                var financeYearRange = yearRanges.FirstOrDefault(x =>
                    x.FromYear <= financeYear && x.ToYear >= financeYear && x.IsActive);

                // Validate finance year range exists (required for rate lookup and tax calculation)
                CalculationValidator.CheckCondition(financeYearRange != null,
                    $"Finance year range not found for FinanceYear={financeYear}. " +
                    $"Tax calculation cannot proceed without valid year range configuration for PropertyId={propertyId}.");
                var yearMaster = await _yearMasterRepo.GetQueryable()
                .FirstOrDefaultAsync(y => y.Year == financeYear && y.IsActive);

                if (yearMaster == null)
                    throw new InvalidOperationException($"Year {financeYear} not found in YearMaster table");

                int yearMasterId = yearMaster.Id;

                // 7a. Fetch Rateable Value policy configuration
                var policyDefaults = new Dictionary<string, string>
            {
                { RateableValuePolicyConstants.RateableValueAreaType, RateableValuePolicyConstants.DefaultAreaType },
                { RateableValuePolicyConstants.RateMasterAreaUnit, RateableValuePolicyConstants.DefaultAreaUnit },
                { RateableValuePolicyConstants.RateMonthlyOrYearly, RateableValuePolicyConstants.DefaultRatePeriod },
                { RateableValuePolicyConstants.EducationEmploymentTaxOnRV, RateableValuePolicyConstants.DefaultEducationEmploymentTaxOnRV }
            };
                var policyValues = await _policyConfigurationService.GetPolicyValuesAsync(policyDefaults);
                var policyOptions = RateableValuePolicyOptions.FromPolicies(policyValues, _logger);

                _logger.LogDebug("Rateable Value Policy Configuration: AreaType={AreaType}, AreaUnit={AreaUnit}, RatePeriod={RatePeriod}, EducationEmploymentTaxOnRV={EducationEmploymentTaxOnRV}",
                    policyOptions.AreaType, policyOptions.AreaUnit, policyOptions.RatePeriod, policyOptions.IsEducationEmploymentTaxOnRV);


                // 7b. Pre-compute selected areas for all property details at once using policy helper
                var selectedAreas = RateableValuePolicyHelper.GetSelectedAreasForProperty(details, policyOptions);

                _logger.LogDebug("Calculating base values for {DetailCount} property details, FinanceYear={FinanceYear}, YearMasterId={YearMasterId}",
                    details.Count, financeYear, yearMasterId);
                // P2: Parallel processing for independent detail calculations (2-10x faster for properties with many details)
                var detailTasks = details.Select(async detail =>
                {
                    // ── ARV Rule Engine: Adjust Annual Rateable Value (Rate per sq.m) ────────
                    decimal? ruleAdjustedRateSqM = null;
                    var detailTypeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);

                    if (detailTypeOfUse != null && financeYearRange != null)
                    {
                        // Mirror the rate lookup from RateableValueCalculator (in-memory, no DB call)
                        var masterRate = rates.FirstOrDefault(x =>
                            x.TaxZoneId == property.TaxZoneId &&
                            x.ConstructionTypeId == detail.ConstructionTypeId &&
                            x.TypeOfUseGroupId == detailTypeOfUse.TypeOfUseGroupId &&
                            x.YearRangeRVId == financeYearRange.Id &&
                            x.IsActive);

                        if (masterRate?.RateSquareMeter > 0)
                        {
                            // 🔍 DEBUG: Log before rule execution
                            _logger.LogInformation(
                                "🔍 [RuleEngine-ARV-DEBUG] About to execute ARV rules for PropertyDetailsId={DetailId}:\n" +
                                "   MasterRate={MasterRate}, Floor={Floor}, UsageType={UsageType}, TypeOfUseGroup={TypeOfUseGroup}",
                                detail.Id, masterRate.RateSquareMeter, detail.FloorId, detail.TypeOfUseId, detailTypeOfUse.TypeOfUseGroupId);

                            await ApplyRulesToRateAsync(
                                category: "RV",
                                detail: detail,
                                detailTypeOfUse: detailTypeOfUse,
                                property: property,
                                propertyAssessment: propertyAssessment,
                                hasLift: hasLift,  // ✅ Pass pre-loaded hasLift
                                constructionYearValue: constructionYearValue,
                                financeYear: financeYear,
                                yearRangeRVId: financeYearRange.Id,  // ✅ Pass year range ID for rate lookup
                                currentRate: masterRate.RateSquareMeter ?? 0m,
                                onApplied: (adjustedRate) =>
                                {
                                    ruleAdjustedRateSqM = adjustedRate;
                                    _logger.LogDebug(
                                        "[RuleEngine-ARV] Adjusted rate for PropertyDetailsId={DetailId}: {OriginalRate} → {AdjustedRate}",
                                        detail.Id, masterRate.RateSquareMeter, adjustedRate);
                                });
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[RuleEngine-ARV] ⚠️ Skipping ARV rule execution for PropertyDetailsId={DetailId}: " +
                                "masterRate is null or zero (TaxZone={TaxZone}, ConstructionType={ConstructionType}, " +
                                "TypeOfUseGroup={TypeOfUseGroup}, YearRange={YearRange}). Using base rate calculation.",
                                detail.Id, property.TaxZoneId, detail.ConstructionTypeId,
                                detailTypeOfUse.TypeOfUseGroupId, financeYearRange.Id);
                        }
                    }
                    // ── End ARV Rule Engine ───────────────────────────────────────────────────────
                    var selectedArea = selectedAreas.TryGetValue(detail.Id, out var area) ? area : 0m;
                    var baseResult = RateableValueCalculator.CalculateBaseValues(
                        detail,
                        financeYear,
                        property.TaxZoneId,
                        property.WardId,
                        typeOfUses,
                        rates,
                        depreciations,
                        yearRanges,
                        renters,
                         selectedArea,
                    policyOptions,
                    ruleAdjustedRateSqM,
                    _logger);  // null = no rule matched, use master rate

                    // Thread-safe add to concurrent dictionary
                    baseResultsCache[detail.Id] = baseResult;
                });

                // Wait for all detail calculations to complete
                await Task.WhenAll(detailTasks);

                _logger.LogDebug("Base values calculated for {CachedCount} details", baseResultsCache.Count);

                // 8. Generate tax calculation rows using cached base results
                _logger.LogDebug("Generating tax calculation rows for {TaxCount} taxes", activeTaxes.Count);
                var newRows = new List<PropertyTaxCalculationRVResultsEntity>();

                foreach (var detail in details)
                {
                    var baseResult = baseResultsCache[detail.Id];
                    var typeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);
                    var propertyType = typeOfUse?.Type;

                    // Apply standard taxes (excluding education and employment)
                    foreach (var tax in activeTaxes)
                    {
                        if (IsEducationTax(tax) || IsEmploymentTax(tax))
                            continue;

                        var taxPct = taxPercentages.FirstOrDefault(x =>
                            x.TaxId == tax.Id &&
                            x.TypeOfUseId == detail.TypeOfUseId);

                        var row = RateableValueTaxCalculator.ApplyTax(baseResult, tax, taxPct);
                        var taxMaster = activeTaxes.FirstOrDefault(t => t.Id == tax.Id);
                        row.TaxMaster = taxMaster;
                        row.IsActive = true;
                        row.MarkedForDeletion = false;
                        row.CreatedDate = DateTime.Now;
                        row.UpdatedDate = DateTime.Now;

                        newRows.Add(row);
                    }
                }

                // 9. Education and Employment tax (grouped by propertyType)
                var propertyTypes = details
                    .Select(d => typeOfUses.FirstOrDefault(x => x.Id == d.TypeOfUseId)?.Type)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .ToList();

                var educationTaxMaster = activeTaxes.FirstOrDefault(IsEducationTax);
                var employmentTaxMaster = activeTaxes.FirstOrDefault(IsEmploymentTax);

                foreach (var propType in propertyTypes)
                {
                    // Get all details of this propertyType
                    var detailsOfType = details
                        .Where(d => string.Equals(
                            typeOfUses.FirstOrDefault(x => x.Id == d.TypeOfUseId)?.Type,
                            propType,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Calculate tax base based on policy: RateableValue or AnnualRentalValue
                    decimal taxBase;
                    if (policyOptions.IsEducationEmploymentTaxOnRV)
                    {
                        // Sum RateableValue for this propertyType
                        taxBase = detailsOfType.Sum(d => baseResultsCache[d.Id].RateableValue ?? 0m);
                    }
                    else
                    {
                        // Sum AnnualRentalValue for this propertyType (default behavior)
                        taxBase = detailsOfType.Sum(d => (decimal)(baseResultsCache[d.Id].AnnualRentalValue ?? 0d));
                    }

                    // Education Tax
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
                                var baseResult = baseResultsCache[d.Id];

                                var row = new PropertyTaxCalculationRVResultsEntity
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
                                    CAreaSqlMtr = baseResult.CAreaSqlMtr,
                                    TaxId = educationTaxMaster.Id,
                                    TaxPercentage = pct,
                                    TaxAmount = amt,
                                    REducationTax = string.Equals(propType, "R", StringComparison.OrdinalIgnoreCase) ? amt : 0m,
                                    CEducationTax = string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase) ? amt : 0m,
                                    REducationTaxPercentage = string.Equals(propType, "R", StringComparison.OrdinalIgnoreCase) ? pct : 0m,
                                    CEducationTaxPercentage = string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase) ? pct : 0m,
                                    REmploymentTax = 0m,
                                    CEmploymentTax = 0m,
                                    REmploymentTaxPercentage = 0m,
                                    CEmploymentTaxPercentage = 0m,
                                    IsActive = true,
                                    MarkedForDeletion = false,
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now
                                };
                                newRows.Add(row);
                            }
                        }
                    }

                    // Employment Tax
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
                                var baseResult = baseResultsCache[d.Id];

                                var row = new PropertyTaxCalculationRVResultsEntity
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
                                    CAreaSqlMtr = baseResult.CAreaSqlMtr,
                                    TaxId = employmentTaxMaster.Id,
                                    TaxPercentage = pct,
                                    TaxAmount = amt,
                                    REducationTax = 0m,
                                    CEducationTax = 0m,
                                    REducationTaxPercentage = 0m,
                                    CEducationTaxPercentage = 0m,
                                    REmploymentTax = string.Equals(propType, "R", StringComparison.OrdinalIgnoreCase) ? amt : 0m,
                                    CEmploymentTax = string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase) ? amt : 0m,
                                    REmploymentTaxPercentage = string.Equals(propType, "R", StringComparison.OrdinalIgnoreCase) ? pct : 0m,
                                    CEmploymentTaxPercentage = string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase) ? pct : 0m,
                                    IsActive = true,
                                    MarkedForDeletion = false,
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now
                                };
                                newRows.Add(row);
                            }
                        }
                    }
                }

                // Wrap all persistence operations in a single transaction to ensure atomicity
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    await ReplaceExistingResults(propertyId, newRows, saveChanges: false);
                    _logger.LogInformation("Persisting {RowCount} tax calculation rows for PropertyId={PropertyId}",
                        newRows.Count, propertyId);

                    // Save to both PolicyTaxDetails and TransmastRV tables in single transaction
                    await SavePolicyAndTransmastRV(propertyId, financeYear, yearMasterId, newRows,
                        educationTaxMaster?.Id, employmentTaxMaster?.Id, saveChanges: false);

                    // Single SaveChanges and Commit for all operations - atomic transaction
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                    _logger.LogInformation("Policy and TransmastRV rows saved successfully for PropertyId={PropertyId}", propertyId);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Failed to save tax calculation results for PropertyId={PropertyId}. Transaction rolled back.", propertyId);
                    throw;
                }

                // Response Mapping - Load policy rows
                var policyRows = await _policyTaxRepo.GetQueryable()
                .Include(x => x.TaxMaster)
                .Where(x => x.PropertyId == propertyId &&
                            x.PolicyYear == financeYear &&
                            x.PolicyCode == "NETTAX" &&
                            x.IsActive &&
                            !x.MarkedForDeletion)
                .ToListAsync();

                var taxMasterCache = new TaxGetterCache<TaxMasterEntity>(
                    activeTaxes,
                    x => x.Id,
                    x => string.IsNullOrWhiteSpace(x.TaxNameAlias) ? x.TaxName : x.TaxNameAlias!
                );

                // Load occupancies
                var occupancies = await _occupancyRepo.GetQueryable()
                    .Where(x => detailIds.Contains(x.PropertyDetailId) && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync();

                // Load old property data (not used but kept for future reference)
                var oldProperty = await _oldPropertyRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                decimal oldTotalRv = oldProperty?.OldRV != null
                    ? Convert.ToDecimal(oldProperty.OldRV)
                    : 0m;

                decimal oldTotalTax = oldProperty?.OldTotalTax != null
                    ? Convert.ToDecimal(oldProperty.OldTotalTax)
                    : 0m;


                var response = RateableValueResponseMapper.Map(
                    propertyId,
                    financeYear,
                    details,
                    newRows,
                    policyRows,
                    floors,
                    constructionTypes,
                    typeOfUses,
                    subTypeOfUses,
                    subFloors,
                    renters,
                    occupancies,
                    taxMasterCache
    );

                LogMetric("TaxCalculation.TotalTax", (double)response.TotalTax, new Dictionary<string, string>
            {
                { "PropertyId", propertyId.ToString() }
            });

                LogMetric("TaxCalculation.TotalRV", (double)response.TotalRateableValue, new Dictionary<string, string>
            {
                { "PropertyId", propertyId.ToString() }
            });

                _logger.LogInformation("RV tax calculation completed for PropertyId={PropertyId}, TotalTax={TotalTax}, TotalRV={TotalRV}, Duration={DurationMs}ms",
                    propertyId, response.TotalTax, response.TotalRateableValue, operationStopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                operationStopwatch.Stop();

                // P3: Log failure metric
                LogMetric("TaxCalculation.Failed", 1, new Dictionary<string, string>
                {
                    { "PropertyId", propertyId.ToString() },
                    { "ErrorType", ex.GetType().Name },
                    { "DurationMs", operationStopwatch.ElapsedMilliseconds.ToString() }
                });

                _logger.LogError(ex, "RV tax calculation failed for PropertyId={PropertyId} after {DurationMs}ms",
                    propertyId, operationStopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        private bool IsEducationTax(TaxMasterEntity tax)
        {
            var taxCode = (tax.TaxCode ?? string.Empty).Trim().ToUpperInvariant();
            var taxName = (tax.TaxName ?? string.Empty).Trim().ToUpperInvariant();
            var taxAlias = (tax.TaxNameAlias ?? string.Empty).Trim().ToUpperInvariant();

            return taxCode.Contains("EDU") ||
                   taxName.Contains("EDUCATION") ||
                   taxAlias.Contains("EDUCATION");
        }

        private bool IsEmploymentTax(TaxMasterEntity tax)
        {
            var taxCode = (tax.TaxCode ?? string.Empty).Trim().ToUpperInvariant();
            var taxName = (tax.TaxName ?? string.Empty).Trim().ToUpperInvariant();
            var taxAlias = (tax.TaxNameAlias ?? string.Empty).Trim().ToUpperInvariant();

            return taxCode.Contains("EMP") ||
                   taxName.Contains("EMPLOYMENT") ||
                   taxAlias.Contains("EMPLOYMENT");
        }

        private bool IsSlabMatch(decimal value, decimal? min, decimal? max)
        {
            var minOk = !min.HasValue || value >= min.Value;
            var maxOk = !max.HasValue || value <= max.Value;
            return minOk && maxOk;
        }

        private async Task ReplaceExistingResults(int propertyId, List<PropertyTaxCalculationRVResultsEntity> newRows, bool saveChanges = true)
        {
            var oldRows = await _taxResultsRepo.GetQueryable()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync();

            foreach (var row in oldRows)
            {
                row.IsActive = false;
                row.MarkedForDeletion = true;
                row.MarkedForDeletionDate = DateTime.Now;
                row.UpdatedDate = DateTime.Now;
                await _taxResultsRepo.UpdateAsync(row);
            }

            foreach (var row in newRows)
            {
                row.IsActive = true;
                row.MarkedForDeletion = false;
                row.MarkedForDeletionDate = null;
                row.CreatedDate = DateTime.Now;
                row.UpdatedDate = DateTime.Now;
            }

            await _taxResultsRepo.AddRangeAsync(newRows);

            if (saveChanges)
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }


        private async Task SavePolicyAndTransmastRV(
            int propertyId,
            int financeYear,
            int yearMasterId,
            List<PropertyTaxCalculationRVResultsEntity> detailRows,
            int? educationTaxId,
            int? employmentTaxId,
            bool saveChanges = true)
        {
            _logger.LogDebug("Saving policy and TransmastRV records for PropertyId={PropertyId}, Year={Year}, YearMasterId={YearMasterId}",
                propertyId, financeYear, yearMasterId);

            // ===== STEP 1: Deactivate old records from BOTH tables =====

            // Deactivate old PolicyTaxDetails records
            var oldPolicyRows = await _policyTaxRepo.GetQueryable()
                .Where(x => x.PropertyId == propertyId &&
                            x.PolicyYear == financeYear &&
                            x.PolicyCode == "NETTAX" &&
                            x.IsActive &&
                            !x.MarkedForDeletion)
                .ToListAsync();

            foreach (var row in oldPolicyRows)
            {
                row.IsActive = false;
                row.MarkedForDeletion = true;
                row.MarkedForDeletionDate = DateTime.Now;
                row.UpdatedDate = DateTime.Now;
                await _policyTaxRepo.UpdateAsync(row);
            }

            // Deactivate old TransmastRV records (using YearMaster.Id)
            var oldTransmastRecords = await _transmastRVRepo.GetQueryable()
                .Where(x => x.PropertyId == propertyId
                         && x.FinanceYearId == yearMasterId
                         && x.IsActive
                         && !x.MarkedForDeletion)
                .ToListAsync();

            foreach (var record in oldTransmastRecords)
            {
                record.IsActive = false;
                record.MarkedForDeletion = true;
                record.MarkedForDeletionDate = DateTime.Now;
                record.UpdatedDate = DateTime.Now;
                await _transmastRVRepo.UpdateAsync(record);
            }

            _logger.LogDebug("Deactivated {PolicyCount} policy and {TransCount} transmast records",
                oldPolicyRows.Count, oldTransmastRecords.Count);

            // ===== STEP 2: Calculate totals and group by TaxId =====

            var totalRv = detailRows
                .GroupBy(x => x.PropertyDetailsId)
                .Sum(g => g.First().RateableValue ?? 0m);

            var taxGroups = detailRows
                .Where(x => x.TaxId > 0)
                .OrderBy(x => x.TaxId)
                .GroupBy(x => x.TaxId)
                .ToList();

            if (!taxGroups.Any())
            {
                _logger.LogWarning("No tax groups found for PropertyId={PropertyId}", propertyId);
                if (saveChanges)
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                return;
            }

            // ===== STEP 3: Create new records for BOTH tables =====

            var newPolicyRecords = new List<PolicyTaxDetailsEntity>();
            var newTransmastRecords = new List<TransMastRVEntity>();
            var now = DateTime.Now;

            foreach (var taxGroup in taxGroups)
            {
                var taxId = taxGroup.Key;

                // Apply MAX aggregation for education/employment taxes (avoids double-counting)
                // Use SUM for all other taxes
                bool isEducationOrEmployment = (educationTaxId.HasValue && taxId == educationTaxId.Value) ||
                                               (employmentTaxId.HasValue && taxId == employmentTaxId.Value);

                decimal taxAmount = isEducationOrEmployment
                    ? taxGroup.Max(x => x.TaxAmount ?? 0m)
                    : taxGroup.Sum(x => x.TaxAmount ?? 0m);

                // Create PolicyTaxDetails record
                var policyRecord = new PolicyTaxDetailsEntity
                {
                    PropertyId = propertyId,
                    PolicyCode = "NETTAX",
                    PolicyDate = now,
                    PolicyYear = (short)financeYear,
                    PolicyRVorCVvalue = totalRv,
                    TaxId = taxId,
                    TaxAmount = taxAmount,
                    IsActive = true,
                    MarkedForDeletion = false,
                    MarkedForDeletionDate = null,
                    CreatedDate = now,
                    UpdatedDate = now
                };

                // Create matching TransmastRV record (using YearMaster.Id)
                var transmastRecord = new TransMastRVEntity
                {
                    PropertyId = propertyId,
                    FinanceYearId = yearMasterId,
                    TaxId = taxId,
                    TaxAmount = taxAmount,
                    RateableValue = totalRv,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedDate = now,
                    UpdatedDate = now
                };

                newPolicyRecords.Add(policyRecord);
                newTransmastRecords.Add(transmastRecord);
            }

            // ===== STEP 4: Save both sets of records in single transaction =====

            if (newPolicyRecords.Any())
            {
                await _policyTaxRepo.AddRangeAsync(newPolicyRecords);
            }

            if (newTransmastRecords.Any())
            {
                await _transmastRVRepo.AddRangeAsync(newTransmastRecords);
            }

            // Save changes only if requested (when not part of outer transaction)
            if (saveChanges)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Saved {PolicyCount} policy and {TransCount} transmast records for PropertyId={PropertyId}, Year={Year}",
                newPolicyRecords.Count, newTransmastRecords.Count, propertyId, financeYear);
        }





        private int GetFinanceYear()
        {
            var today = DateTime.Today;
            return today.Month >= 4 ? today.Year : today.Year - 1;
        }

        /// <summary>
        /// P2: Executes rule with retry logic for resilience against transient failures.
        /// Delegates to RetryHelper for industry-standard retry pattern.
        /// </summary>
        private Task<List<RuleExecutionResultDto>> ExecuteRuleWithRetryAsync(
            RuleExecutionInputDto ruleInput,
            int detailId,
            int maxRetries = 3)
        {
            return RetryHelper.ExecuteWithRetryAsync(
                operation: () => _ruleExecutionService.ExecuteAsync(ruleInput),
                logger: _logger,
                operationName: "RuleEngine",
                contextId: $"PropertyDetailsId={detailId}",
                maxRetries: maxRetries);
        }

        /// <summary>
        /// Applies rule engine effects to ALV or RV values with comprehensive context.
        /// Executes rules for the specified category and applies cumulative effects.
        /// All property and detail context is passed to enable flexible rule expressions.
        /// </summary>
        /// <param name="category">Rule category (ALV or RV)</param>
        /// <param name="detail">Property detail entity</param>
        /// <param name="detailTypeOfUse">Type of use entity for context</param>
        /// <param name="property">Property entity for ward/zone context</param>
        /// <param name="propertyAssessment">Property assessment entity (optional) for owner type context</param>
        /// <param name="hasLift">Whether the property has a lift (pre-loaded to avoid N+1 query)</param>
        /// <param name="constructionYearValue">Parsed construction year value</param>
        /// <param name="financeYear">Current finance year</param>
        /// <param name="yearRangeRVId">Year range ID for rate lookup</param>
        /// <param name="currentValue">Current value to adjust (ALV or RV)</param>
        /// <param name="onApplied">Callback to apply the adjusted value</param>
        private async Task ApplyRulesToValueAsync(
            string category,
            PropertyDetailsEntity detail,
            TypeOfUseEntity detailTypeOfUse,
            PropertyEntity property,
            PropertyAssessmentEntity? propertyAssessment,
            bool hasLift,
            int constructionYearValue,
            int financeYear,
            int yearRangeRVId,
            decimal currentValue,
            Action<decimal> onApplied)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Validate input values
                if (detail.FloorId <= 0 || detailTypeOfUse.TypeOfUseGroupId <= 0)
                {
                    _logger.LogDebug(
                        "[RuleEngine-{Category}] Skipping rule execution for PropertyDetailsId={DetailId}: Invalid Floor or TypeOfUseGroup",
                        category, detail.Id);
                    return;
                }

                // Build comprehensive input context (reusable across ARV/ALV/RV)
                var inputContext = await BuildRuleInputContext(
                    detail, detailTypeOfUse, property, propertyAssessment, hasLift, constructionYearValue, financeYear, yearRangeRVId);

                // Add category-specific parameter: Value (ALV or RV to adjust)
                inputContext["Value"] = (double)currentValue;

                var ruleInput = new RuleExecutionInputDto
                {
                    Category = category,
                    Input = inputContext
                };

                // P2: Execute rules with retry logic
                var ruleResults = await ExecuteRuleWithRetryAsync(ruleInput, detail.Id);
                stopwatch.Stop();

                // P3: Log performance metric
                LogMetric("RuleExecution.Duration", stopwatch.ElapsedMilliseconds, new Dictionary<string, string>
                {
                    { "PropertyDetailsId", detail.Id.ToString() },
                    { "Category", category }
                });

                if (ruleResults != null && ruleResults.Any())
                {
                    // Apply ALL matching rules sequentially in priority order
                    decimal cumulativeValue = currentValue;
                    var appliedRules = new List<string>();
                    var stopProcessing = false;

                    foreach (var rule in ruleResults)
                    {
                        var applicator = _effectApplicators.FirstOrDefault(a => a.CanHandle(rule.EffectType));
                        if (applicator != null)
                        {
                            var previousValue = cumulativeValue;
                            cumulativeValue = await applicator.Apply(cumulativeValue, rule.EffectValue);
                            appliedRules.Add($"{rule.RuleCode}({rule.EffectType} {rule.EffectValue}%: {previousValue:F2}→{cumulativeValue:F2})");

                            _logger.LogDebug(
                                "[RuleEngine-{Category}] Applied rule '{RuleCode}' to PropertyDetailsId={DetailId}: " +
                                "Value {PreviousValue} → {NewValue} ({EffectType} {EffectValue})",
                                category, rule.RuleCode, detail.Id, previousValue, cumulativeValue,
                                rule.EffectType, rule.EffectValue);

                            // 🔹 Check if this rule has StopProcessing flag
                            if (rule.StopProcessing)
                            {
                                _logger.LogInformation(
                                    "🛑 [RuleEngine-{Category}] Rule '{RuleCode}' has StopProcessing=true. " +
                                    "Remaining rules will not be applied for PropertyDetailsId={DetailId}.",
                                    category, rule.RuleCode, detail.Id);
                                stopProcessing = true;
                                break; // Exit the loop, don't apply further rules
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[RuleEngine-{Category}] No applicator found for EffectType='{EffectType}' in rule '{RuleCode}', skipping",
                                category, rule.EffectType, rule.RuleCode);
                        }
                    }

                    // Apply the adjusted value via callback
                    onApplied(cumulativeValue);

                    // P3: Log rule application metric
                    LogMetric("RuleExecution.RulesApplied", ruleResults.Count, new Dictionary<string, string>
                    {
                        { "PropertyDetailsId", detail.Id.ToString() },
                        { "Category", category },
                        { "OriginalValue", currentValue.ToString("F2") },
                        { "FinalValue", cumulativeValue.ToString("F2") },
                        { "StopProcessing", stopProcessing.ToString() }
                    });

                    var statusMsg = stopProcessing ? " (stopped early)" : "";
                    _logger.LogInformation(
                        "[RuleEngine-{Category}] ✅ Applied {RuleCount} rule(s) to PropertyDetailsId={DetailId} in {ElapsedMs}ms{StatusMsg}: " +
                        "Value {OriginalValue} → {FinalValue}. Rules: {AppliedRules}",
                        category, appliedRules.Count, detail.Id, stopwatch.ElapsedMilliseconds, statusMsg,
                        currentValue, cumulativeValue,
                        string.Join(" → ", appliedRules));

                    // Performance budget warning
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        _logger.LogWarning(
                            "[RuleEngine-{Category}] Performance: Rule execution took {ElapsedMs}ms (>100ms budget) for PropertyDetailsId={DetailId}",
                            category, stopwatch.ElapsedMilliseconds, detail.Id);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "[RuleEngine-{Category}] No rules matched for PropertyDetailsId={DetailId} in {ElapsedMs}ms, using original value {Value}",
                        category, detail.Id, stopwatch.ElapsedMilliseconds, currentValue);
                }
            }
            catch (ArgumentException argEx)
            {
                stopwatch.Stop();
                _logger.LogWarning(argEx,
                    "[RuleEngine-{Category}] ⚠️ Validation error for PropertyDetailsId={DetailId} after {ElapsedMs}ms. Using original value.",
                    category, detail.Id, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[RuleEngine-{Category}] ❌ Execution failed for PropertyDetailsId={DetailId} after {ElapsedMs}ms. Using original value.",
                    category, detail.Id, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Applies rule engine effects to ARV rate (Annual Rateable Value rate per sq.m) with comprehensive context.
        /// Executes rules for ARV category and applies cumulative effects to the base rate.
        /// This method provides consistent rule execution logic across ARV, ALV, and RV categories.
        /// </summary>
        /// <param name="category">Rule category (should be "ARV" for rate adjustment)</param>
        /// <param name="detail">Property detail entity</param>
        /// <param name="detailTypeOfUse">Type of use entity for context</param>
        /// <param name="property">Property entity for ward/zone context</param>
        /// <param name="propertyAssessment">Property assessment entity (optional) for owner type context</param>
        /// <param name="hasLift">Whether the property has a lift (pre-loaded to avoid N+1 query)</param>
        /// <param name="constructionYearValue">Parsed construction year value</param>
        /// <param name="financeYear">Current finance year</param>
        /// <param name="yearRangeRVId">Year range ID for rate lookup</param>
        /// <param name="currentRate">Current rate to adjust (base rate per sq.m)</param>
        /// <param name="onApplied">Callback to apply the adjusted rate</param>
        private async Task ApplyRulesToRateAsync(
            string category,
            PropertyDetailsEntity detail,
            TypeOfUseEntity detailTypeOfUse,
            PropertyEntity property,
            PropertyAssessmentEntity? propertyAssessment,
            bool hasLift,
            int constructionYearValue,
            int financeYear,
            int yearRangeRVId,
            decimal currentRate,
            Action<decimal> onApplied)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Validate input values
                if (detail.FloorId <= 0 || detailTypeOfUse.TypeOfUseGroupId <= 0)
                {
                    _logger.LogDebug(
                        "[RuleEngine-{Category}] Skipping rule execution for PropertyDetailsId={DetailId}: Invalid Floor or TypeOfUseGroup",
                        category, detail.Id);
                    return;
                }

                // Build comprehensive input context (reusable across ARV/ALV/RV)
                var inputContext = await BuildRuleInputContext(
                    detail, detailTypeOfUse, property, propertyAssessment, hasLift, constructionYearValue, financeYear, yearRangeRVId);

                // Add category-specific parameter: Rate (per sq.m for ARV)
                inputContext["Rate"] = (double)currentRate;

                var ruleInput = new RuleExecutionInputDto
                {
                    Category = category,
                    Input = inputContext
                };

                // P2: Execute rules with retry logic
                var ruleResults = await ExecuteRuleWithRetryAsync(ruleInput, detail.Id);
                stopwatch.Stop();

                // P3: Log performance metric
                LogMetric("RuleExecution.Duration", stopwatch.ElapsedMilliseconds, new Dictionary<string, string>
                {
                    { "PropertyDetailsId", detail.Id.ToString() },
                    { "Category", category }
                });

                if (ruleResults != null && ruleResults.Any())
                {
                    // Apply ALL matching rules sequentially in priority order
                    decimal cumulativeRate = currentRate;
                    var appliedRules = new List<string>();

                    foreach (var rule in ruleResults)
                    {
                        var applicator = _effectApplicators.FirstOrDefault(a => a.CanHandle(rule.EffectType));
                        if (applicator != null)
                        {
                            var previousRate = cumulativeRate;
                            cumulativeRate = await applicator.Apply(cumulativeRate, rule.EffectValue);
                            appliedRules.Add($"{rule.RuleCode}({rule.EffectType} {rule.EffectValue}%: {previousRate:F2}→{cumulativeRate:F2})");

                            _logger.LogDebug(
                                "[RuleEngine-{Category}] Applied rule '{RuleCode}' to PropertyDetailsId={DetailId}: " +
                                "Rate {PreviousRate} → {NewRate} ({EffectType} {EffectValue})",
                                category, rule.RuleCode, detail.Id, previousRate, cumulativeRate,
                                rule.EffectType, rule.EffectValue);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[RuleEngine-{Category}] No applicator found for EffectType='{EffectType}' in rule '{RuleCode}', skipping",
                                category, rule.EffectType, rule.RuleCode);
                        }
                    }

                    // Apply the adjusted rate via callback
                    onApplied(cumulativeRate);

                    // P3: Log rule application metric
                    LogMetric("RuleExecution.RulesApplied", ruleResults.Count, new Dictionary<string, string>
                    {
                        { "PropertyDetailsId", detail.Id.ToString() },
                        { "Category", category },
                        { "OriginalRate", currentRate.ToString("F2") },
                        { "FinalRate", cumulativeRate.ToString("F2") }
                    });

                    _logger.LogInformation(
                        "[RuleEngine-{Category}] ✅ Applied {RuleCount} rule(s) to PropertyDetailsId={DetailId} in {ElapsedMs}ms: " +
                        "Rate {OriginalRate} → {FinalRate}. Rules: {AppliedRules}",
                        category, ruleResults.Count, detail.Id, stopwatch.ElapsedMilliseconds,
                        currentRate, cumulativeRate,
                        string.Join(" → ", appliedRules));

                    // Performance budget warning
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        _logger.LogWarning(
                            "[RuleEngine-{Category}] Performance: Rule execution took {ElapsedMs}ms (>100ms budget) for PropertyDetailsId={DetailId}",
                            category, stopwatch.ElapsedMilliseconds, detail.Id);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "[RuleEngine-{Category}] No rules matched for PropertyDetailsId={DetailId} in {ElapsedMs}ms, using original rate {Rate}",
                        category, detail.Id, stopwatch.ElapsedMilliseconds, currentRate);
                }
            }
            catch (ArgumentException argEx)
            {
                stopwatch.Stop();
                _logger.LogWarning(argEx,
                    "[RuleEngine-{Category}] ⚠️ Validation error for PropertyDetailsId={DetailId} after {ElapsedMs}ms. Using original rate.",
                    category, detail.Id, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[RuleEngine-{Category}] ❌ Execution failed for PropertyDetailsId={DetailId} after {ElapsedMs}ms. Using original rate.",
                    category, detail.Id, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Builds comprehensive rule input context with all property and detail parameters.
        /// This context enables flexible rule configuration using any available property/detail attributes.
        /// </summary>
        /// <param name="detail">Property detail entity</param>
        /// <param name="detailTypeOfUse">Type of use entity for group classification</param>
        /// <param name="property">Property entity for location context</param>
        /// <param name="propertyAssessment">Property assessment entity for owner type</param>
        /// <param name="hasLift">Whether the property has a lift (pre-loaded to avoid N+1 query)</param>
        /// <param name="constructionYearValue">Parsed construction year</param>
        /// <param name="financeYear">Current finance year</param>
        /// <param name="yearRangeRVId">Year range ID for rate lookup</param>
        /// <returns>Dictionary with comprehensive context parameters (excluding category-specific Rate/Value)</returns>
        private async Task<Dictionary<string, object>> BuildRuleInputContext(
            PropertyDetailsEntity detail,
            TypeOfUseEntity detailTypeOfUse,
            PropertyEntity property,
            PropertyAssessmentEntity? propertyAssessment,
            bool hasLift,
            int constructionYearValue,
            int financeYear,
            int yearRangeRVId)
        {
            // ✅ Use pre-loaded hasLift parameter instead of querying database
            // This prevents N+1 query problem when processing multiple details

            var context = new Dictionary<string, object>
            {
                // Primary classification
                { "Floor",              detail.FloorId },
                { "Type",     detailTypeOfUse.TypeOfUseGroupId },
                
                // Property context
                { "Property Type",         property.Id },
                { "Ward",               property.WardId },
                { "TaxZone",            property.TaxZoneId },
                
                // Detail context
                { "PropertyDetailsId",  detail.Id },
                { "Construction Type",   detail.ConstructionTypeId },
                { "Type Of Use",          detail.TypeOfUseId },
                { "Carpet Area SqMeter",  detail.CarpetAreaSqMeter ?? 0 },
                { "Carpet Area SqFeet",   detail.CarpetAreaSqFeet ?? 0 },
                { "Builtup Area SqMeter", detail.BuiltupAreaSqMeter ?? 0 },
                { "Builtup Area SqFeet",  detail.BuiltupAreaSqFeet ?? 0 },
                { "NoOfRooms",          detail.NoOfRooms ?? 0 },
                { "Rented",          detail.IsRenter ?? false },
                
                // Building age context
                { "ConstructionYear",   constructionYearValue },
                { "PropertyAge",        financeYear - constructionYearValue },
                { "FinanceYear",        financeYear },
                { "YearRangeRVId",      yearRangeRVId },  // ✅ For RateLookupApplicator
                { "Zone",       property.TaxZoneId },
                { "Ward",        property.WardId },
                { "Owner Type",        propertyAssessment?.OwnerTypeId ?? 0},
                { "Sub Floor",   detail.SubFloorId ?? 0 },
                { "Lift",   hasLift }
            };

            // 🔍 DEBUG: Log the complete input context for rule execution
            _logger.LogInformation(
                "🔍 [RuleEngine-DEBUG] Building input context for PropertyDetailsId={PropertyDetailsId}, PropertyId={PropertyId}:\n" +
                "   Floor={Floor}, UsageType={UsageType}, TypeOfUseGroup={TypeOfUseGroup}\n" +
                "   ConstructionType={ConstructionType}, Ward={Ward}, TaxZone={TaxZone}\n" +
                "   CarpetAreaSqMeter={CarpetAreaSqMeter}, NoOfRooms={NoOfRooms}\n" +
                "   ConstructionYear={ConstructionYear}, PropertyAge={PropertyAge}, FinanceYear={FinanceYear}\n" +
                "   Lift={HasLift}, OwnerType={OwnerType}",
                detail.Id, property.Id,
                detail.FloorId, detail.TypeOfUseId, detailTypeOfUse.TypeOfUseGroupId,
                detail.ConstructionTypeId, property.WardId, property.TaxZoneId,
                detail.CarpetAreaSqMeter, detail.NoOfRooms,
                constructionYearValue, financeYear - constructionYearValue, financeYear,
                hasLift, propertyAssessment?.OwnerTypeId ?? 0);

            return context;
        }

        /// <summary>
        /// P3: Logs custom metrics for Application Insights / monitoring dashboards.
        /// These structured logs can be queried and visualized in monitoring systems.
        /// </summary>
        private void LogMetric(string metricName, double value, Dictionary<string, string>? properties = null)
        {
            var logEntry = new Dictionary<string, object>
            {
                { "MetricName", metricName },
                { "Value", value },
                { "Timestamp", DateTime.UtcNow }
            };

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    logEntry[$"Property_{prop.Key}"] = prop.Value;
                }
            }

            // P3: Structured logging for metrics (Application Insights automatically picks up these logs)
            _logger.LogInformation(
                "[Metric] {MetricName} = {Value} {@Properties}",
                metricName, value, properties ?? new Dictionary<string, string>());
        }
    }
}