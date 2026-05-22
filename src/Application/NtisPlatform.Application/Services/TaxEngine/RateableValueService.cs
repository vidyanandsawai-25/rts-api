using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly TaxMasterDataService _masterDataService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RateableValueService> _logger;

        public RateableValueService(
            IRepository<PropertyEntity, int> propertyRepo,
            IRepository<PropertyDetailsEntity, int> propertyDetailsRepo,
            IRepository<PropertyTaxCalculationRVResultsEntity, int> taxResultsRepo,
            IRepository<PolicyTaxDetailsEntity, int> policyTaxRepo,
            IRepository<RenterMastEntity, int> renterRepo,
            IRepository<PropertyOccupancyDetailsEntity, int> occupancyRepo,
            IRepository<PropertyMastOldEntity, int> oldPropertyRepo,
            TaxMasterDataService masterDataService,
            IUnitOfWork unitOfWork,
            ILogger<RateableValueService> logger)
        {
            _propertyRepo = propertyRepo;
            _propertyDetailsRepo = propertyDetailsRepo;
            _taxResultsRepo = taxResultsRepo;
            _policyTaxRepo = policyTaxRepo;
            _renterRepo = renterRepo;
            _occupancyRepo = occupancyRepo;
            _oldPropertyRepo = oldPropertyRepo;
            _masterDataService = masterDataService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RateableValueResponseDto> CalculateAndSaveAsync(int propertyId)
        {
            _logger.LogInformation("Starting RV tax calculation for PropertyId={PropertyId}", propertyId);

            // 1. Validation - Get property
            var property = await _propertyRepo.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                ?? throw new InvalidOperationException($"Property not found for PropertyId={propertyId}");

            // 2. Get property details
            var details = await _propertyDetailsRepo.GetQueryable()
                .Where(x => x.PropertyId == propertyId && x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync();

            CalculationValidator.CheckCondition(details.Any(), $"PropertyDetails not found for PropertyId={propertyId}");

            // 3. Load all master data
            _logger.LogDebug("Loading master data for PropertyId={PropertyId}, WardId={WardId}", propertyId, property.WardId);
            var typeOfUses = await _masterDataService.GetActiveTypeOfUsesAsync();
            var subTypeOfUses = await _masterDataService.GetActiveSubTypeOfUsesAsync();
            var floors = await _masterDataService.GetActiveFloorsAsync();
           //var suBfloors = await _masterDataService.GetActiveSubFloorsAsync(); commented for future use
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
            var baseResultsCache = new Dictionary<int, PropertyTaxCalculationRVResultsEntity>();
            
            foreach (var detail in details)
            {
                var baseResult = RateableValueCalculator.CalculateBaseValues(
                    detail,
                    financeYear,
                    property.TaxZoneId,
                    property.WardId,
                    typeOfUses,
                    rates,
                    depreciations,
                    yearRanges,
                    renters);
                
                baseResultsCache[detail.Id] = baseResult;
            }
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

                // Sum ALV for this propertyType using cached results
                var totalAlv = detailsOfType
                    .Sum(d => (decimal)(baseResultsCache[d.Id].AnnualRentalValue ?? 0d));

                // Education Tax
                if (educationTaxMaster != null)
                {
                    var slab = educationTaxSlabs.FirstOrDefault(x =>
                        IsSlabMatch(totalAlv, x.MinAmount, x.MaxAmount) &&
                        (string.IsNullOrWhiteSpace(x.Type) ||
                         string.Equals(x.Type, propType, StringComparison.OrdinalIgnoreCase)));

                    if (slab != null)
                    {
                        var pct = slab.Rate ?? 0m;
                        var amt = Math.Round(totalAlv * pct / 100m, 0, MidpointRounding.AwayFromZero);

                        // Note: Education tax is calculated at property-type level (aggregated ALV),
                        // but we create a row for each detail with the SAME amount. This enables
                        // per-detail reporting while the policy rows maintain the correct aggregate.
                        // Response mapper uses policy TaxTotal to avoid double-counting.
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
                        IsSlabMatch(totalAlv, x.MinAmount, x.MaxAmount) &&
                        (string.IsNullOrWhiteSpace(x.Type) ||
                         string.Equals(x.Type, propType, StringComparison.OrdinalIgnoreCase)));

                    if (slab != null)
                    {
                        var pct = slab.Rate ?? 0m;
                        var amt = Math.Round(totalAlv * pct / 100m, 0, MidpointRounding.AwayFromZero);

                        // Note: Employment tax is calculated at property-type level (aggregated ALV),
                        // but we create a row for each detail with the SAME amount. This enables
                        // per-detail reporting while the policy rows maintain the correct aggregate.
                        // Response mapper uses policy TaxTotal to avoid double-counting.
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

            //await ReplaceExistingResults(propertyId, newRows);
            _logger.LogInformation("Persisting {RowCount} tax calculation rows for PropertyId={PropertyId}",
                newRows.Count, propertyId);
            await SavePolicyRows(propertyId, GetFinanceYear(), newRows, educationTaxMaster?.Id, employmentTaxMaster?.Id);
            _logger.LogInformation("Policy rows saved successfully for PropertyId={PropertyId}", propertyId);

            // Response Mapping - Load policy rows
            var policyRows = await _policyTaxRepo.GetQueryable()
                .Include(x => x.TaxMaster)
                .Where(x => x.PropertyId == propertyId &&
                            x.PolicyYear == GetFinanceYear() &&
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
                renters,
                occupancies,
                taxMasterCache);

            _logger.LogInformation("RV tax calculation completed for PropertyId={PropertyId}, TotalTax={TotalTax}, TotalRV={TotalRV}",
                propertyId, response.TotalTax, response.TotalRateableValue);

            return response;
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

        private async Task ReplaceExistingResults(int propertyId, List<PropertyTaxCalculationRVResultsEntity> newRows)
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
            await _unitOfWork.SaveChangesAsync();
        }


        private async Task SavePolicyRows(
            int propertyId,
            int financeYear,
            List<PropertyTaxCalculationRVResultsEntity> detailRows,
            int? educationTaxId,
            int? employmentTaxId)
        {
            _logger.LogDebug("Saving policy rows for PropertyId={PropertyId}, FinanceYear={FinanceYear}",
                propertyId, financeYear);
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

            var totalRv = detailRows
                .GroupBy(x => x.PropertyDetailsId)
                .Sum(g => g.First().RateableValue ?? 0m);


            var groupedTaxes = detailRows

                .OrderBy(x => x.TaxId)
                .GroupBy(x => x.TaxId)
                               .Select(g =>
                               {
                                   var firstRow = g.FirstOrDefault();
                                   var taxId = g.Key;



                                   decimal taxAmount;
                                   // Apply MAX aggregation for education/employment taxes (avoids double-counting)
                                   // since these are calculated at property-type level but duplicated per detail.
                                   // Use SUM for all other taxes.
                                   bool isEducationOrEmployment = (educationTaxId.HasValue && taxId == educationTaxId.Value) ||
                                                                  (employmentTaxId.HasValue && taxId == employmentTaxId.Value);
                                   if (isEducationOrEmployment)
                                       taxAmount = g.Max(x => x.TaxAmount ?? 0m);
                                   else
                                       taxAmount = g.Sum(x => x.TaxAmount ?? 0m);

                                   return new PolicyTaxDetailsEntity
                                   {
                                       PropertyId = propertyId,
                                       PolicyCode = "NETTAX",
                                       PolicyDate = DateTime.Now,
                                       PolicyYear = (short)financeYear,
                                       PolicyRVorCVvalue = totalRv,
                                       TaxId = g.Key,
                                       TaxAmount = taxAmount,
                                       IsActive = true,
                                       MarkedForDeletion = false,
                                       MarkedForDeletionDate = null,
                                       CreatedDate = DateTime.Now,
                                       UpdatedDate = DateTime.Now
                                   };
                               })
                .ToList();

            await _policyTaxRepo.AddRangeAsync(groupedTaxes);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogDebug("Saved {PolicyRowCount} policy rows (replaced {OldRowCount} old rows)",
                groupedTaxes.Count, oldPolicyRows.Count);
        }



        private int GetFinanceYear()
        {
            var today = DateTime.Today;
            return today.Month >= 4 ? today.Year : today.Year - 1;
        }
    }
}