using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
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
        private readonly IRuleApplierService _ruleApplierService;
        private readonly IPropertyContextLoaderService _propertyContextLoaderService;
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
            IRuleApplierService ruleApplierService,
            IPropertyContextLoaderService propertyContextLoaderService,
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
            _ruleApplierService = ruleApplierService;
            _propertyContextLoaderService = propertyContextLoaderService;
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
                // 1. Load complete PropertyCalculationContext using loader service
                int financeYear = GetFinanceYear();
                var propertyContext = await _propertyContextLoaderService.LoadPropertyContextAsync(propertyId, financeYear);

                var property = propertyContext.Property;
                var propertyAssessment = propertyContext.PropertyAssessment;
                var details = propertyContext.Details.ToList();
                var renters = propertyContext.Renters.ToList();

                var hasLift             = propertyContext.Parameters.HasLift;
                var constructionYearValue = propertyContext.Parameters.ConstructionYearValue;

                // P3: Log property complexity metric
                LogMetric("Property.DetailCount", details.Count, new Dictionary<string, string>
                {
                    { "PropertyId", propertyId.ToString() }
                });

                // 2. Load all master data
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

                // 3. Load tax-related master data
                var yearRangeRVId = propertyContext.Parameters.YearRangeRVId;

                var taxPercentages = (await _masterDataService.GetActiveTaxPercentagesAsync())
                    .Where(x => x.YearRangeRVId == yearRangeRVId)
                    .ToList();
                var educationTaxSlabs = await _masterDataService.GetActiveEducationTaxSlabsAsync();
                var employmentTaxSlabs = await _masterDataService.GetActiveEmploymentTaxSlabsAsync();

                // 4. Pre-calculate base values for all details (cache to avoid redundant computation)
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
                    // ── RV Rule Engine: Adjust Rateable Value Rate ──────────────────────────────
                    decimal? ruleAdjustedRatePerUnit = null;
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

                        // ✅ Use RateableValueCalculator's helper method for consistent rate selection
                        decimal masterRatePerUnit = RateableValueCalculator.GetRatePerUnit(masterRate, policyOptions);

                        if (masterRatePerUnit > 0)
                        {
                            var ruleContext = new RuleApplierContext
                            {
                                Category    = "RV",
                                ValueKey    = "Rate",
                                InitialValue = masterRatePerUnit,
                                PropertyContext = propertyContext.CloneForDetail(detail, detailTypeOfUse)
                            };

                            ruleAdjustedRatePerUnit = await _ruleApplierService.ApplyRulesAsync(ruleContext);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[RuleEngine-RV] ⚠️ Skipping RV rule execution for PropertyDetailsId={DetailId}: " +
                                "masterRate is null or zero (TaxZone={TaxZone}, ConstructionType={ConstructionType}, " +
                                "TypeOfUseGroup={TypeOfUseGroup}, YearRange={YearRange}, Unit={Unit}). Using base rate calculation.",
                                detail.Id, property.TaxZoneId, detail.ConstructionTypeId,
                                detailTypeOfUse.TypeOfUseGroupId, financeYearRange.Id,
                                policyOptions.IsSqFeetUnit ? "sqft" : "sqm");
                        }
                    }
                    // ── End RV Rule Engine ───────────────────────────────────────────────────────
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
                    ruleAdjustedRatePerUnit,
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
                var occupancies = propertyContext.Occupancies.ToList();

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