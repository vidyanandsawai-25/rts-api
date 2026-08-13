using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyComparison;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services;

public class PropertyComparisonService : IPropertyComparisonService
{
    private const string TaxTotalCode = "TaxTotal";
    private readonly IRepository<PropertyMapDetailEntity, int> _mapDetailRepo;
    private readonly IRepository<PropertyDetailsOldEntity, int> _oldDetailsRepo;
    private readonly IRepository<PropertyDetailsEntity, int> _newDetailsRepo;
    private readonly IRepository<TransMastOldEntity, int> _oldTransMastRepo;
    private readonly IRepository<TransMastEntity, int> _transmastRepo;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepo;
    private readonly IRepository<PropertyEntity, int> _propertyMastRepo;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeMasterRepo;
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepo;
    private readonly IPolicyConfigurationService _policyConfigService;
    private readonly ILogger<PropertyComparisonService> _logger;

    public PropertyComparisonService(
        IRepository<PropertyMapDetailEntity, int> mapDetailRepo,
        IRepository<PropertyDetailsOldEntity, int> oldDetailsRepo,
        IRepository<PropertyDetailsEntity, int> newDetailsRepo,
        IRepository<TransMastOldEntity, int> oldTransMastRepo,
        IRepository<TransMastEntity, int> transmastRepo,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepo,
        IRepository<PropertyEntity, int> propertyMastRepo,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeMasterRepo,
        IRepository<TaxMasterEntity, int> taxMasterRepo,
        IPolicyConfigurationService policyConfigService,
        ILogger<PropertyComparisonService> logger)
    {
        _mapDetailRepo = mapDetailRepo;
        _oldDetailsRepo = oldDetailsRepo;
        _newDetailsRepo = newDetailsRepo;
        _oldTransMastRepo = oldTransMastRepo;
        _transmastRepo = transmastRepo;
        _propertyMastOldRepo = propertyMastOldRepo;
        _propertyMastRepo = propertyMastRepo;
        _propertyTypeMasterRepo = propertyTypeMasterRepo;
        _taxMasterRepo = taxMasterRepo;
        _policyConfigService = policyConfigService;
        _logger = logger;
    }

    public async Task<PropertyComparisonDto> ComparePropertiesAsync(int newPropertyId)
    {
        _logger.LogInformation(
            "Comparing properties: NewPropertyId={NewPropertyId}",
            newPropertyId);

        try
        {
            // Get all mapping details for the new property
            var mapDetails = await _mapDetailRepo.GetQueryable()
                .Where(x => x.PropertyIdNew == newPropertyId && x.Status == "ACTIVE" && x.IsCurrent && x.IsActive)
                .ToListAsync();

            mapDetails ??= new List<PropertyMapDetailEntity>();

            if (mapDetails.Count == 0)
            {
                _logger.LogWarning(
                    "No property mapping found for NewPropertyId={NewPropertyId}; returning new property data only",
                    newPropertyId);
            }

            var oldPropertyIds = mapDetails
                .Where(x => x.PropertyIdOld.HasValue)
                .Select(x => x.PropertyIdOld.Value)
                .Distinct()
                .ToList();

            if (mapDetails.Count > 0 && oldPropertyIds.Count == 0)
            {
                _logger.LogWarning(
                    "No old property IDs found for NewPropertyId={NewPropertyId}; returning new property data only",
                    newPropertyId);
            }

            var isMerge = oldPropertyIds.Count > 1;

            var comparison = new PropertyComparisonDto
            {
                OldPropertyIds = string.Join(", ", oldPropertyIds),
                NewPropertyId = newPropertyId
            };

            // Get area unit from policy configuration
            var areaUnit = await _policyConfigService.GetPolicyValueAsync("AreaUnit", "SqMeter");
            comparison.Area.Unit = areaUnit;

            // Get old property details - for MERGE, get all old properties; for ONE_TO_ONE, get single property
            var oldDetails = await _oldDetailsRepo.GetQueryable()
                .Where(x => oldPropertyIds.Contains(x.PropertyMastOldId) && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync();

            // Get new property details
            var newDetails = await _newDetailsRepo.GetQueryable()
                .Where(x => x.PropertyId == newPropertyId && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync();

            // Calculate Area (sum all old properties if MERGE)
            comparison.Area.Old = GetOldAreaSum(oldDetails, areaUnit);
            comparison.Area.New = GetNewAreaSum(newDetails, areaUnit);

            // Check for change of use based on PropertyTypeId (for MERGE, check all old properties)
            await PopulateChangeOfUseAsync(comparison, oldPropertyIds, newPropertyId);

            // Get RV, ALV, and Tax from TransMast tables (latest finance year only, sum all old properties if MERGE)
            // First get max finance year for old properties
            var maxOldFinanceYear = await _oldTransMastRepo.GetQueryable()
                .Where(x => oldPropertyIds.Contains(x.PropertyMastOldId) && x.CalculationType == "RV" && x.IsActive && !x.MarkedForDeletion)
                .MaxAsync(x => (int?)x.FinanceYearId) ?? 0;

            var oldTransMasts = maxOldFinanceYear > 0
                ? await _oldTransMastRepo.GetQueryable()
                    .Where(x => oldPropertyIds.Contains(x.PropertyMastOldId) && x.CalculationType == "RV" && x.FinanceYearId == maxOldFinanceYear && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync()
                : new List<TransMastOldEntity>();

            var newTransMast = await _transmastRepo.GetQueryable()
                .Where(x => x.PropertyId == newPropertyId && x.CalculationType == "RV" && x.IsActive && !x.MarkedForDeletion)
                .OrderByDescending(x => x.FinanceYearId)
                .FirstOrDefaultAsync();

            // For MERGE, sum RV and ALV from all old properties (latest year only); for ONE_TO_ONE, use single property
            // Group by PropertyMastOldId to avoid double-counting when multiple tax heads exist per property
            var oldRV = oldTransMasts.Count > 0
                ? oldTransMasts
                    .GroupBy(x => x.PropertyMastOldId)
                    .Sum(g => g.FirstOrDefault()?.CalculationValue ?? 0)
                : 0;
            var oldALV = oldTransMasts.Count > 0
                ? oldTransMasts
                    .GroupBy(x => x.PropertyMastOldId)
                    .Sum(g => g.FirstOrDefault()?.CalculationAnnualValue ?? 0)
                : 0;

            comparison.RV.Old = oldRV;
            comparison.RV.New = newTransMast?.CalculationValue ?? 0;

            comparison.ALV.Old = oldALV;
            comparison.ALV.New = newTransMast?.CalculationAnnualValue ?? 0;

            // Get Tax (TaxTotal by TaxCode, latest finance year, sum all old properties if MERGE)
            var taxTotalId = await _taxMasterRepo.GetQueryable()
                .Where(x => x.TaxCode == TaxTotalCode && x.IsActive)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (taxTotalId == 0)
            {
                _logger.LogWarning("TaxTotal record not found with TaxCode={TaxTotalCode}", TaxTotalCode);
                comparison.Tax.Old = 0;
                comparison.Tax.New = 0;
            }
            else
            {
                // Get max finance year for old tax records
                var maxOldTaxFinanceYear = await _oldTransMastRepo.GetQueryable()
                    .Where(x => oldPropertyIds.Contains(x.PropertyMastOldId) && x.TaxId == taxTotalId && x.IsActive && !x.MarkedForDeletion)
                    .MaxAsync(x => (int?)x.FinanceYearId) ?? 0;

                var oldTaxRecords = await (maxOldTaxFinanceYear > 0
                    ? _oldTransMastRepo.GetQueryable()
                        .Where(x => oldPropertyIds.Contains(x.PropertyMastOldId) && x.TaxId == taxTotalId && x.FinanceYearId == maxOldTaxFinanceYear && x.IsActive && !x.MarkedForDeletion)
                        .Select(x => new { x.PropertyMastOldId, x.TaxAmount })
                    : _oldTransMastRepo.GetQueryable()
                        .Where(x => false)
                        .Select(x => new { x.PropertyMastOldId, x.TaxAmount }))
                    .ToListAsync();

                var newTaxTotal = await _transmastRepo.GetQueryable()
                    .Where(x => x.PropertyId == newPropertyId && x.TaxId == taxTotalId && x.IsActive && !x.MarkedForDeletion)
                    .OrderByDescending(x => x.FinanceYearId)
                    .Select(x => x.TaxAmount)
                    .FirstOrDefaultAsync();

                // Sum tax for all old properties (latest year only)
                var oldTaxTotal = oldTaxRecords.Count > 0 ? oldTaxRecords.Sum(x => x.TaxAmount) : 0;

                comparison.Tax.Old = oldTaxTotal;
                comparison.Tax.New = newTaxTotal;
            }

            _logger.LogInformation(
                "Property comparison completed: NewPropertyId={NewPropertyId}, MappingType={MappingType}, OldPropertyCount={OldPropertyCount}, OldRV={OldRV}, NewRV={NewRV}, OldTax={OldTax}, NewTax={NewTax}",
                newPropertyId, isMerge ? "MERGE" : "ONE_TO_ONE", oldPropertyIds.Count, comparison.RV.Old, comparison.RV.New, comparison.Tax.Old, comparison.Tax.New);

            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing properties: NewPropertyId={NewPropertyId}",
                newPropertyId);
            throw;
        }
    }

    private decimal GetOldAreaSum(System.Collections.Generic.List<PropertyDetailsOldEntity> details, string areaUnit)
    {
        if (details == null || details.Count == 0)
            return 0;

        return areaUnit.Equals("SqMeter", StringComparison.OrdinalIgnoreCase)
            ? (decimal)(details.Sum(d => d.OldBuiltupAreaSqMeter ?? 0d))
            : (decimal)(details.Sum(d => d.OldBuiltupAreaSqFeet ?? 0d));
    }

    private decimal GetNewAreaSum(System.Collections.Generic.List<PropertyDetailsEntity> details, string areaUnit)
    {
        if (details == null || details.Count == 0)
            return 0;

        return areaUnit.Equals("SqMeter", StringComparison.OrdinalIgnoreCase)
            ? (decimal)(details.Sum(d => d.BuiltupAreaSqMeter ?? 0d))
            : (decimal)(details.Sum(d => d.BuiltupAreaSqFeet ?? 0d));
    }

    private async Task PopulateChangeOfUseAsync(PropertyComparisonDto comparison, List<int> oldPropertyIds, int newPropertyId)
    {
        try
        {
            // Get new property and its use category (populated regardless of whether an old mapping exists)
            var newProperty = await _propertyMastRepo.GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == newPropertyId);

            string newUse = string.Empty;
            if (newProperty?.PropertyTypeId.HasValue == true)
            {
                var newPropertyType = await _propertyTypeMasterRepo.GetQueryable()
                    .Where(x => x.Id == newProperty.PropertyTypeId)
                    .Select(x => x.Type)
                    .FirstOrDefaultAsync();

                newUse = PropertyTypeToUseMapper.GetUseCategory(newPropertyType);
            }

            comparison.ChangeOfUse.NewUse = newUse;

            if (oldPropertyIds == null || oldPropertyIds.Count == 0)
            {
                comparison.ChangeOfUse.HasChanged = false;
                return;
            }

            // Get old properties
            var oldProperties = await _propertyMastOldRepo.GetQueryable()
                .Where(x => oldPropertyIds.Contains(x.Id))
                .ToListAsync();

            if (oldProperties.Count == 0 || string.IsNullOrEmpty(newUse))
            {
                comparison.ChangeOfUse.HasChanged = false;
                return;
            }

            // Get property types for all old properties
            var oldPropertyTypeIds = oldProperties
                .Where(x => x.OldPropertyTypeId.HasValue)
                .Select(x => x.OldPropertyTypeId.Value)
                .Distinct()
                .ToList();

            if (oldPropertyTypeIds.Count == 0)
            {
                comparison.ChangeOfUse.HasChanged = false;
                return;
            }

            var oldPropertyTypes = await _propertyTypeMasterRepo.GetQueryable()
                .Where(x => oldPropertyTypeIds.Contains(x.Id))
                .Select(x => x.Type)
                .ToListAsync();

            // Get distinct use categories from all old properties
            var oldUseCategories = oldPropertyTypes
                .Select(type => PropertyTypeToUseMapper.GetUseCategory(type))
                .Distinct()
                .ToList();

            // Determine old use: if all same use, show that; if mixed, show "Mixed"
            var oldUse = oldUseCategories.Count == 1
                ? oldUseCategories.First()
                : "Mixed";

            comparison.ChangeOfUse.OldUse = oldUse;
            comparison.ChangeOfUse.HasChanged = !string.Equals(oldUse, newUse, StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Change of use detected: OldUse={OldUse} (from {PropertyCount} properties), NewUse={NewUse}, HasChanged={HasChanged}",
                oldUse, oldProperties.Count, newUse, comparison.ChangeOfUse.HasChanged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating change of use data for NewPropertyId={NewPropertyId}",
                newPropertyId);
            comparison.ChangeOfUse.HasChanged = false;
        }
    }
}
