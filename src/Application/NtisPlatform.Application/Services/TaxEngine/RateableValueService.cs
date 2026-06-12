using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services.TaxEngine
{
    /// <summary>
    /// Orchestrates the end-to-end Rateable Value calculation for a single property.
    /// Persistence is delegated to <see cref="IRVPersistenceService"/>;
    /// rule-engine application to <see cref="IRVRuleApplicator"/>.
    /// </summary>
    public class RateableValueService : IRateableValueService
    {
        private readonly IRepository<PropertyEntity, int> _propertyRepo;
        private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepo;
        private readonly IRepository<RenterMastEntity, int> _renterRepo;
        private readonly IRepository<PropertyOccupancyDetailsEntity, int> _occupancyRepo;
        private readonly IRepository<PropertySocialDetailsEntity, int> _propertySocialDetailsRepo;
        private readonly IRepository<PropertyAssessmentEntity, int> _propertyAssessmentRepo;
        private readonly ITaxMasterDataService _masterDataService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RateableValueService> _logger;
        private readonly IRepository<YearMasterEntity, int> _yearMasterRepo;
        private readonly IPolicyConfigurationService _policyConfigurationService;
        private readonly IRateableValueCalculatorService _rateableValueCalculatorService;
    
        private readonly IFinanceYearProvider _financeYearProvider;
        private readonly IRVRuleApplicator _ruleApplicator;
        private readonly IRVPersistenceService _persistenceService;
        private readonly TimeProvider _timeProvider;

        public RateableValueService(
            IRepository<PropertyEntity, int> propertyRepo,
            IRepository<PropertyDetailsEntity, int> propertyDetailsRepo,
            IRepository<RenterMastEntity, int> renterRepo,
            IRepository<PropertyOccupancyDetailsEntity, int> occupancyRepo,
            IRepository<PropertySocialDetailsEntity, int> propertySocialDetailsRepo,
            IRepository<PropertyAssessmentEntity, int> propertyAssessmentRepo,
            ITaxMasterDataService masterDataService,
            IUnitOfWork unitOfWork,
            ILogger<RateableValueService> logger,
            IRepository<YearMasterEntity, int> yearMasterRepo,
            IPolicyConfigurationService policyConfigurationService,
            IRateableValueCalculatorService rateableValueCalculatorService,
         
            IFinanceYearProvider financeYearProvider,
            IRVRuleApplicator ruleApplicator,
            IRVPersistenceService persistenceService,
            TimeProvider timeProvider)


        {
            _propertyRepo = propertyRepo;
            _propertyDetailsRepo = propertyDetailsRepo;
            _renterRepo = renterRepo;
            _occupancyRepo = occupancyRepo;
            _propertySocialDetailsRepo = propertySocialDetailsRepo;
            _propertyAssessmentRepo = propertyAssessmentRepo;
            _masterDataService = masterDataService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _yearMasterRepo = yearMasterRepo;
            _policyConfigurationService = policyConfigurationService;
            _rateableValueCalculatorService = rateableValueCalculatorService;
        
            _financeYearProvider = financeYearProvider;
            _ruleApplicator = ruleApplicator;
            _persistenceService = persistenceService;
            _timeProvider = timeProvider;
        }

        public async Task<RateableValueResponseDto> CalculateAndSaveAsync(int propertyId)
        {
            var operationStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting RV tax calculation for PropertyId={PropertyId}", propertyId);

            try
            {
                // 1. Load property
                var property = await _propertyRepo.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                    ?? throw new InvalidOperationException($"Property not found for PropertyId={propertyId}");

                // Load assessment for owner-type context passed to rule engine
                var propertyAssessment = await _propertyAssessmentRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync();

                if (propertyAssessment == null)
                    _logger.LogWarning(
                        "PropertyAssessmentEntity not found for PropertyId={PropertyId}. " +
                        "OwnerType will default to 0 in rule engine.", propertyId);

                // Load hasLift once per property to avoid per-detail queries
                var hasLift = await _propertySocialDetailsRepo.GetQueryable()
                    .Include(psd => psd.SocialAttribute)
                    .AnyAsync(psd => psd.PropertyId == propertyId &&
                                     psd.SocialAttribute != null &&
                                     psd.SocialAttribute.SocialAttributeCode == "HAS_LIFT" &&
                                     psd.IsActive);

                _logger.LogDebug("Property {PropertyId} has lift: {HasLift}", propertyId, hasLift);

                // 2. Get property details — AsNoTracking: this is a read-only calc path
                var details = await _propertyDetailsRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                if (!details.Any())
                    throw new InvalidOperationException($"PropertyDetails not found for PropertyId={propertyId}");

                LogMetric("Property.DetailCount", details.Count, new Dictionary<string, string>
                {
                    { "PropertyId", propertyId.ToString() }
                });

                // 3. Load master data — sequential; TaxMasterDataService uses IMemoryCache
                //    so repeated calls within the same cache window are in-memory (< 1 ms each).
                //    Task.WhenAll cannot be used here: all repositories share the same scoped
                //    DbContext instance, which is not thread-safe for concurrent async operations.
                _logger.LogDebug("Loading master data for PropertyId={PropertyId}, WardId={WardId}",
                    propertyId, property.WardId);

                var typeOfUses        = await _masterDataService.GetActiveTypeOfUsesAsync();
                var subTypeOfUses     = await _masterDataService.GetActiveSubTypeOfUsesAsync();
                var floors            = await _masterDataService.GetActiveFloorsAsync();
                var subFloors         = await _masterDataService.GetActiveSubFloorsAsync();
                var constructionTypes = await _masterDataService.GetActiveConstructionTypesAsync();
                var rateSectionId     = await _masterDataService.GetRateSectionIdForWardAsync(property.WardId);
                var rates             = await _masterDataService.GetRatesForSectionAsync(rateSectionId);
                var depreciations     = await _masterDataService.GetActiveDepreciationsAsync();
                var yearRanges        = await _masterDataService.GetActiveYearRangesAsync();
                var activeTaxes       = await _masterDataService.GetActiveTaxesAsync();

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

                // 4. Load renters — AsNoTracking: read-only
                var detailIds = details.Select(d => d.Id).ToList();
                var renters = await _renterRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(x => detailIds.Contains(x.PropertyDetailsId) && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync();

                // 5. Validate construction year
                var constructionYear = details.FirstOrDefault()?.ConstructionYear;
                CalculationValidator.CheckCondition(
                    !string.IsNullOrWhiteSpace(constructionYear),
                    $"ConstructionYear not found for PropertyId={propertyId}");
                CalculationValidator.CheckCondition(
                    int.TryParse(constructionYear, out int constructionYearValue),
                    $"Invalid ConstructionYear value '{constructionYear}' for PropertyId={propertyId}");

                var yearRange = yearRanges.FirstOrDefault(x =>
                    x.FromYear <= constructionYearValue && x.ToYear >= constructionYearValue)
                    ?? throw new InvalidOperationException(
                        $"Assessment year range not found for constructionYear={constructionYearValue}");

                // 6. Load tax-related master data (sequential; served from cache after first request)
                var allTaxPercentages  = await _masterDataService.GetActiveTaxPercentagesAsync();
                var taxPercentages     = allTaxPercentages.Where(x => x.YearRangeRVId == yearRange.Id).ToList();
                var educationTaxSlabs  = await _masterDataService.GetActiveEducationTaxSlabsAsync();
                var employmentTaxSlabs = await _masterDataService.GetActiveEmploymentTaxSlabsAsync();

                _logger.LogInformation(
                    "Tax data for PropertyId={PropertyId}: " +
                    "YearRange={YearRangeId} ({From}-{To}), TaxPercentages(all)={AllPct}, TaxPercentages(filtered)={FilteredPct}, " +
                    "EducationSlabs={EduSlabs}, EmploymentSlabs={EmpSlabs}",
                    propertyId, yearRange.Id, yearRange.FromYear, yearRange.ToYear,
                    allTaxPercentages.Count, taxPercentages.Count,
                    educationTaxSlabs.Count, employmentTaxSlabs.Count);

                if (taxPercentages.Count == 0)
                    _logger.LogWarning(
                        "No TaxPercentages found for YearRangeRVId={YearRangeId} (ConstructionYear={Year}). " +
                        "Tax amounts will be zero for PropertyId={PropertyId}. " +
                        "Total TaxPercentages in DB (any year): {AllCount}.",
                        yearRange.Id, constructionYearValue, propertyId, allTaxPercentages.Count);

                // 7. Resolve finance year
                int financeYear = _financeYearProvider.GetCurrentFinanceYear();
                _logger.LogDebug("Calculating base values for {DetailCount} property details, FinanceYear={FinanceYear}",
                    details.Count, financeYear);

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

                // 7a. Load policy configuration
                var policyDefaults = new Dictionary<string, string>
                {
                    { RateableValuePolicyConstants.RateableValueAreaType,        RateableValuePolicyConstants.DefaultAreaType },
                    { RateableValuePolicyConstants.RateMasterAreaUnit,           RateableValuePolicyConstants.DefaultAreaUnit },
                    { RateableValuePolicyConstants.RateMonthlyOrYearly,          RateableValuePolicyConstants.DefaultRatePeriod },
                    { RateableValuePolicyConstants.EducationEmploymentTaxOnRV,   RateableValuePolicyConstants.DefaultEducationEmploymentTaxOnRV },
                    { RateableValuePolicyConstants.MaintenanceRateKey,           RateableValuePolicyConstants.DefaultMaintenanceRate }
                };
                var policyValues  = await _policyConfigurationService.GetPolicyValuesAsync(policyDefaults);
                var policyOptions = RateableValuePolicyOptions.FromPolicies(policyValues, _logger);

                _logger.LogDebug(
                    "RV Policy: AreaType={AreaType}, AreaUnit={AreaUnit}, RatePeriod={RatePeriod}, " +
                    "EducationEmploymentTaxOnRV={EducationEmploymentTaxOnRV}, Maintenance={Maintenance}%",
                    policyOptions.AreaType, policyOptions.AreaUnit, policyOptions.RatePeriod,
                    policyOptions.IsEducationEmploymentTaxOnRV, policyOptions.MaintenanceRatePercent);

                // 7b. Pre-compute selected areas for all details
                var selectedAreas = RateableValuePolicyHelper.GetSelectedAreasForProperty(details, policyOptions);

                // 8. Calculate base values (sequential — scoped DbContext is not safe for concurrent async)
                var baseResultsCache = new Dictionary<int, PropertyTaxCalculationRVResultsEntity>();

                foreach (var detail in details)
                {
                    decimal? ruleAdjustedRate = null;
                    var detailTypeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);

                    if (detailTypeOfUse != null && financeYearRange != null)
                    {
                        var masterRate = rates.FirstOrDefault(x =>
                            x.TaxZoneId           == property.TaxZoneId &&
                            x.ConstructionTypeId  == detail.ConstructionTypeId &&
                            x.TypeOfUseGroupId    == detailTypeOfUse.TypeOfUseGroupId &&
                            x.YearRangeRVId       == financeYearRange.Id &&
                            x.IsActive);

                        decimal masterRatePerUnit = RateableValueCalculator.GetRatePerUnit(masterRate, policyOptions);

                        if (masterRatePerUnit > 0)
                        {
                            _logger.LogDebug(
                                "[RuleEngine-RV] Executing RV rules for PropertyDetailsId={DetailId}: MasterRate={MasterRate} ({Unit})",
                                detail.Id, masterRatePerUnit, policyOptions.IsSqFeetUnit ? "sqft" : "sqm");

                            ruleAdjustedRate = await _ruleApplicator.GetAdjustedRateAsync(
                                detail, detailTypeOfUse, property, propertyAssessment,
                                hasLift, constructionYearValue, financeYear,
                                financeYearRange.Id, masterRatePerUnit);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[RuleEngine-RV] Skipping rule execution for PropertyDetailsId={DetailId}: " +
                                "masterRate is null or zero. Using base rate.",
                                detail.Id);
                        }
                    }

                    var selectedArea = selectedAreas.TryGetValue(detail.Id, out var area) ? area : 0m;
                    baseResultsCache[detail.Id] = _rateableValueCalculatorService.CalculateBaseValues(
                        detail, financeYear, property.TaxZoneId, property.WardId,
                        typeOfUses, rates, depreciations, yearRanges, renters,
                        selectedArea, policyOptions, ruleAdjustedRate);
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

                var newRows = new List<PropertyTaxCalculationRVResultsEntity>();
                var now = _timeProvider.GetLocalNow().DateTime;

                foreach (var detail in details)
                {
                    var baseResult = baseResultsCache[detail.Id];

                    foreach (var tax in regularTaxes)
                    {
                        var taxPct = taxPercentages.FirstOrDefault(x =>
                            x.TaxId == tax.Id && x.TypeOfUseId == detail.TypeOfUseId);

                        if (taxPct == null)
                            _logger.LogWarning(
                                "No TaxPercentage found for TaxId={TaxId} ({TaxCode}), " +
                                "TypeOfUseId={TypeOfUseId}, YearRangeRVId={YearRangeId}. " +
                                "Tax amount will be zero for PropertyDetailsId={DetailId}.",
                                tax.Id, tax.TaxCode, detail.TypeOfUseId, yearRange.Id, detail.Id);

                        var row = RateableValueTaxCalculator.ApplyTax(baseResult, tax, taxPct);
                        // Do NOT set row.TaxMaster — assigning an AsNoTracking entity as a navigation
                        // property on a new (Added) entity causes EF to attempt INSERT on TaxMaster.
                        // TaxId (FK) is already set by ApplyTax; that is sufficient for persistence.
                        row.IsActive          = true;
                        row.MarkedForDeletion = false;
                        row.CreatedDate       = now;
                        row.UpdatedDate       = now;
                        newRows.Add(row);
                    }
                }

                // 10. Education and Employment tax — grouped by property type (R/C)
                var propertyTypes = details
                    .Select(d => typeOfUses.FirstOrDefault(x => x.Id == d.TypeOfUseId)?.Type)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .ToList();

                var educationTaxMaster  = activeTaxes.FirstOrDefault(IsEducationTax);
                var employmentTaxMaster = activeTaxes.FirstOrDefault(IsEmploymentTax);

                foreach (var propType in propertyTypes)
                {
                    var detailsOfType = details
                        .Where(d => string.Equals(
                            typeOfUses.FirstOrDefault(x => x.Id == d.TypeOfUseId)?.Type,
                            propType,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    decimal taxBase = policyOptions.IsEducationEmploymentTaxOnRV
                        ? detailsOfType.Sum(d => baseResultsCache[d.Id].RateableValue ?? 0m)
                        : detailsOfType.Sum(d => baseResultsCache[d.Id].AnnualRentalValue ?? 0m);

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
                                newRows.Add(BuildSpecialTaxRow(
                                    baseResultsCache[d.Id], educationTaxMaster, propType!, pct, amt,
                                    isEducation: true, now));
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
                                newRows.Add(BuildSpecialTaxRow(
                                    baseResultsCache[d.Id], employmentTaxMaster, propType!, pct, amt,
                                    isEducation: false, now));
                        }
                    }
                }

                // 11. Total RV across all details (each detail contributes its RV once)
                decimal totalRv = baseResultsCache.Values.Sum(r => r.RateableValue ?? 0m);

                _logger.LogInformation(
                    "Row summary for PropertyId={PropertyId}: TotalRows={Total}, TotalRV={TotalRv}",
                    propertyId, newRows.Count, totalRv);

                if (newRows.Count == 0)
                    _logger.LogWarning(
                        "PropertyId={PropertyId}: 0 tax rows generated. " +
                        "Likely causes: no active taxes, no matching TaxPercentages for YearRangeId={YearRangeId}, " +
                        "or no rates for RateSectionId={RateSectionId}.",
                        propertyId, yearRange.Id, rateSectionId);

                // 12. Persist all results in a single transaction
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    await _persistenceService.ReplaceExistingResultsAsync(propertyId, newRows);

                    _logger.LogInformation(
                        "Persisting {RowCount} tax calculation rows for PropertyId={PropertyId}",
                        newRows.Count, propertyId);

                    var savedPolicyRecords = await _persistenceService.SavePolicyAndTransmastRVAsync(
                        propertyId, financeYear, yearMasterId, newRows, totalRv,
                        educationTaxMaster?.Id, employmentTaxMaster?.Id);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation(
                        "Policy and TransmastRV rows saved for PropertyId={PropertyId}", propertyId);

                    // 13. Build response from in-memory data (no extra DB round-trip)
                    var occupancies = await _occupancyRepo.GetQueryable()
                        .Where(x => detailIds.Contains(x.PropertyDetailId) && x.IsActive && !x.MarkedForDeletion)
                        .ToListAsync();

                    var taxMasterCache = new TaxGetterCache<TaxMasterEntity>(
                        activeTaxes,
                        x => x.Id,
                        x => string.IsNullOrWhiteSpace(x.TaxNameAlias) ? x.TaxName : x.TaxNameAlias!);

                    var response = RateableValueResponseMapper.Map(
                        propertyId, financeYear, details, newRows, savedPolicyRecords,
                        floors, constructionTypes, typeOfUses, subTypeOfUses, subFloors,
                        renters, occupancies, taxMasterCache);

                    LogMetric("TaxCalculation.TotalTax", (double)response.TotalTax, new Dictionary<string, string>
                        { { "PropertyId", propertyId.ToString() } });
                    LogMetric("TaxCalculation.TotalRV", (double)response.TotalRateableValue, new Dictionary<string, string>
                        { { "PropertyId", propertyId.ToString() } });

                    _logger.LogInformation(
                        "RV calculation completed for PropertyId={PropertyId}, TotalTax={TotalTax}, " +
                        "TotalRV={TotalRV}, Duration={DurationMs}ms",
                        propertyId, response.TotalTax, response.TotalRateableValue,
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

        // ── Tax classification ───────────────────────────────────────────────────────────
        // Compare against PTIS.TaxCategoryMaster.CategoryCode (loaded via Include in
        // GetActiveTaxesAsync). Using CategoryCode avoids dependency on surrogate key values
        // that differ across environments.

        /// <summary>Returns true when the tax belongs to the Education category (CategoryCode = "EDU").</summary>
        private static bool IsEducationTax(TaxMasterEntity tax) =>
            string.Equals(
                tax.TaxCategoryMaster?.CategoryCode, "EDU",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>Returns true when the tax belongs to the Employment category (CategoryCode = "EMP").</summary>
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

        // ── Row builders ─────────────────────────────────────────────────────────────────

        private static PropertyTaxCalculationRVResultsEntity BuildSpecialTaxRow(
            PropertyTaxCalculationRVResultsEntity baseResult,
            TaxMasterEntity taxMaster,
            string propType,
            decimal percentage,
            decimal amount,
            bool isEducation,
            DateTime now)
        {
            bool isR = string.Equals(propType, "R", StringComparison.OrdinalIgnoreCase);
            bool isC = string.Equals(propType, "C", StringComparison.OrdinalIgnoreCase);

            return new PropertyTaxCalculationRVResultsEntity
            {
                PropertyId            = baseResult.PropertyId,
                PropertyDetailsId     = baseResult.PropertyDetailsId,
                MonthlyRate           = baseResult.MonthlyRate,
                YearlyRate            = baseResult.YearlyRate,
                YearlyRent            = baseResult.YearlyRent,
                Depreciation          = baseResult.Depreciation,
                DepreciationPer       = baseResult.DepreciationPer,
                AppliedOn             = baseResult.AppliedOn,
                AnnualRentalValue     = baseResult.AnnualRentalValue,
                Maintenance           = baseResult.Maintenance,
                RateableValue         = baseResult.RateableValue,
                TotalAreaSqMtr        = baseResult.TotalAreaSqMtr,
                RAreaSqMtr            = baseResult.RAreaSqMtr,
                CAreaSqlMtr           = baseResult.CAreaSqlMtr,
                TaxId                 = taxMaster.Id,
                TaxPercentage         = percentage,
                TaxAmount             = amount,
                REducationTax         = isEducation && isR ? amount : 0m,
                CEducationTax         = isEducation && isC ? amount : 0m,
                REducationTaxPercentage  = isEducation && isR ? percentage : 0m,
                CEducationTaxPercentage  = isEducation && isC ? percentage : 0m,
                REmploymentTax        = !isEducation && isR ? amount : 0m,
                CEmploymentTax        = !isEducation && isC ? amount : 0m,
                REmploymentTaxPercentage = !isEducation && isR ? percentage : 0m,
                CEmploymentTaxPercentage = !isEducation && isC ? percentage : 0m,
                IsActive              = true,
                MarkedForDeletion     = false,
                CreatedDate           = now,
                UpdatedDate           = now
            };
        }

        // ── Metrics ──────────────────────────────────────────────────────────────────────

        private void LogMetric(string metricName, double value, Dictionary<string, string>? properties = null)
        {
            _logger.LogInformation("[Metric] {MetricName} = {Value} {@Properties}",
                metricName, value, properties ?? new Dictionary<string, string>());
        }
    }
}
