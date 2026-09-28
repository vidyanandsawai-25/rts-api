using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.Globalization;

namespace NtisPlatform.Application.Services;

public class WingWiseDetailsService : IWingWiseDetailsService
{
    private const string TaxTotalCode = "TaxTotal";
    private const string AmenityPartType = "Amenity";
    private const string ResidentialPropertyType = "Residential";
    private const string CommercialPropertyType = "Commercial";

    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<FloorEntity, int> _floorRepository;
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsMastRepository;
    private readonly IRepository<WingEntity, int> _wingRepository;
    private readonly IRepository<TaxMasterEntity, int> _taxRepository;
    private readonly IRepository<TransMastEntity, int> _transMastRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxDetailsRepository;

    public WingWiseDetailsService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<FloorEntity, int> floorRepository,
        IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        IRepository<WingEntity, int> wingRepository,
        IRepository<TaxMasterEntity, int> taxRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PolicyTaxDetailsEntity, int> policyTaxDetailsRepository)
    {
        _propertyRepository = propertyRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _wardRepository = wardRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _floorRepository = floorRepository;
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _wingRepository = wingRepository;
        _taxRepository = taxRepository;
        _transMastRepository = transMastRepository;
        _yearMasterRepository = yearMasterRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _policyTaxDetailsRepository = policyTaxDetailsRepository;
    }

    public async Task<WingWiseDetailsResponseDto> GetWingWiseDetailsAsync(WingWiseDetailsQueryParameters query, CancellationToken cancellationToken = default)
    {
        if (!query.WardId.HasValue || string.IsNullOrWhiteSpace(query.PropertyNo))
            return Empty(query);

        var normalizedPropertyNo = query.PropertyNo.Trim();
        var properties = await GetScopedPropertiesAsync(query.WardId.Value, normalizedPropertyNo, cancellationToken);
        if (properties.Count == 0)
            return Empty(query.WardId.Value, normalizedPropertyNo);

        var propertyIds = properties.Select(p => p.PropertyId).ToList();
        var wardNo = await GetWardNoAsync(query.WardId.Value, cancellationToken);
        var totalTaxId = await GetTaxTotalIdAsync(cancellationToken);
        var areaByProperty = await GetAreaByPropertyAsync(propertyIds, cancellationToken);
        var floorCodeById = await GetFloorCodeByIdAsync(areaByProperty.Values, cancellationToken);

        // Old-property values (OldTotalTax + OldRV) come from the same PropertyMapDetail → PropertyMastOld
        // join, so fetch them once and share across demand (Old Demand) and RV (Previous RV).
        var oldPropertyByProperty = await GetOldPropertyValuesAsync(propertyIds, cancellationToken);

        var demandByProperty = totalTaxId.HasValue
            ? await GetDemandByPropertyAsync(propertyIds, totalTaxId.Value, oldPropertyByProperty, cancellationToken)
            : new Dictionary<int, PropertyDemand>();

        var rvByProperty = await GetRvByPropertyAsync(propertyIds, oldPropertyByProperty, cancellationToken);

        var wings = BuildWingDetails(properties, areaByProperty, floorCodeById, demandByProperty, rvByProperty);
        return BuildResponse(properties[0].PropertyId, normalizedPropertyNo, wardNo, wings);
    }

    private async Task<string> GetWardNoAsync(int wardId, CancellationToken cancellationToken)
    {
        return await _wardRepository.GetQueryable().AsNoTracking()
            .Where(w => w.IsActive && w.Id == wardId)
            .Select(w => w.WardNo)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
    }

    private async Task<List<PropertyScopeRow>> GetScopedPropertiesAsync(int wardId,string propertyNo,CancellationToken cancellationToken)
    {
        var propertyQuery = _propertyRepository.GetQueryable().AsNoTracking()
            .Where(pm => pm.IsActive
                && !pm.MarkedForDeletion
                && pm.WardId == wardId
                && pm.PropertyNo == propertyNo
                && pm.WingDetailId.HasValue);

        var typeQuery = _propertyTypeRepository.GetQueryable().AsNoTracking()
            .Where(ptm => ptm.IsActive);

        var wingDetailsQuery = _wingDetailsMastRepository.GetQueryable().AsNoTracking()
            .Where(wd => wd.IsActive && !wd.MarkedForDeletion);

        var wingQuery = _wingRepository.GetQueryable().AsNoTracking()
            .Where(wm => wm.IsActive);

        return await (
            from pm in propertyQuery
            join ptm in typeQuery on pm.PropertyTypeId equals ptm.Id into ptmJoin
            from ptm in ptmJoin.DefaultIfEmpty()
            join wd in wingDetailsQuery on pm.WingDetailId!.Value equals wd.Id
            join wm in wingQuery on wd.WingMasterId equals wm.Id
            select new PropertyScopeRow
            {
                PropertyId = pm.Id,
                PartitionNo = pm.PartitionNo,
                PropertyTypeId = pm.PropertyTypeId,
                PropertyTypeName = ptm != null ? ptm.PropertyDescription : string.Empty,
                PartType = ptm != null ? ptm.PartType : null,
                WingDetailId = wd.Id,
                WingMasterId = wd.WingMasterId,
                WingId = wm.Id,
                WingNo = wm.WingNo,
                WingName = wd.WingName,
                SocietyDetailsMastId = wd.SocietyDetailsMastId
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<int?> GetTaxTotalIdAsync(CancellationToken cancellationToken)
    {
        return await _taxRepository.GetQueryable().AsNoTracking()
            .Where(t => t.IsActive && t.TaxCode == TaxTotalCode)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<int, PropertyArea>> GetAreaByPropertyAsync(IReadOnlyCollection<int> propertyIds,CancellationToken cancellationToken)
    {
        return await _propertyDetailsRepository.GetQueryable().AsNoTracking()
            .Where(pd => propertyIds.Contains(pd.PropertyId)
                && pd.IsActive
                && !pd.MarkedForDeletion)
            .GroupBy(pd => pd.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                MinFloorId = g.Min(pd => pd.FloorId),
                MaxFloorId = g.Max(pd => pd.FloorId),
                AreaSqFeet = g.Sum(pd => (decimal?)(pd.CarpetAreaSqFeet ?? 0d)) ?? 0m
            })
            .ToDictionaryAsync(
                x => x.PropertyId,
                x => new PropertyArea(x.MinFloorId, x.MaxFloorId, x.AreaSqFeet),
                cancellationToken);
    }

    private async Task<Dictionary<int, string>> GetFloorCodeByIdAsync(IEnumerable<PropertyArea> propertyAreas,CancellationToken cancellationToken)
    {
        var floorIds = propertyAreas
            .SelectMany(area => new[] { area.MinFloorId, area.MaxFloorId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (floorIds.Count == 0)
            return new Dictionary<int, string>();

        return await _floorRepository.GetQueryable().AsNoTracking()
            .Where(floor => floor.IsActive && floorIds.Contains(floor.Id))
            .Select(floor => new
            {
                floor.Id,
                FloorCode = floor.FloorCode ?? floor.Description ?? string.Empty
            })
            .ToDictionaryAsync(x => x.Id, x => x.FloorCode, cancellationToken);
    }

    private async Task<Dictionary<int, OldPropertyValues>> GetOldPropertyValuesAsync(IReadOnlyCollection<int> propertyIds, CancellationToken cancellationToken)
    {
        // Single PropertyMapDetail (new → old) → PropertyMastOld join, aggregated per property.
        // Shared by Old Demand (OldTotalTax) and Previous RV (OldRV) to avoid a duplicate join.
        return await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
            .Where(map => map.PropertyIdNew.HasValue
                && propertyIds.Contains(map.PropertyIdNew.Value)
                && map.PropertyIdOld.HasValue)
            .Join(
                _propertyMastOldRepository.GetQueryable().AsNoTracking(),
                map => map.PropertyIdOld!.Value,
                old => old.Id,
                (map, old) => new { PropertyId = map.PropertyIdNew!.Value, old.OldTotalTax, old.OldRV })
            .GroupBy(x => x.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                OldTotalTax = g.Sum(x => (decimal?)x.OldTotalTax) ?? 0m,
                OldRV = g.Sum(x => (decimal?)x.OldRV) ?? 0m
            })
            .ToDictionaryAsync(
                x => x.PropertyId,
                x => new OldPropertyValues(x.OldTotalTax, x.OldRV),
                cancellationToken);
    }

    private async Task<Dictionary<int, PropertyDemand>> GetDemandByPropertyAsync(
        IReadOnlyCollection<int> propertyIds,
        int totalTaxId,
        IReadOnlyDictionary<int, OldPropertyValues> oldPropertyByProperty,
        CancellationToken cancellationToken)
    {
        var currentFinanceYear = await _yearMasterRepository.GetQueryable().AsNoTracking()
            .Select(y => (int?)y.Year)
            .MaxAsync(cancellationToken);

        // Current + Retro demand are derived from the same TransMast set, so scan it once and
        // split the two amounts with conditional aggregates (avoids a second round-trip):
        //   Current = current finance year, TAXTOTAL only.
        //   Retro   = prior finance years whose policy is flagged IsRetroDemand.
        var transDemand = await _transMastRepository.GetQueryable().AsNoTracking()
            .Where(tm => propertyIds.Contains(tm.PropertyId)
                && tm.IsActive
                && !tm.MarkedForDeletion
                && tm.TaxId == totalTaxId
                && (tm.FinanceYearId == currentFinanceYear
                    || (tm.PolicyCodeMaster != null && tm.PolicyCodeMaster.IsRetroDemand)))
            .GroupBy(tm => tm.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                Current = g.Sum(tm => tm.FinanceYearId == currentFinanceYear ? tm.TaxAmount : 0m),
                Retro = g.Sum(tm => tm.FinanceYearId != currentFinanceYear
                    && tm.PolicyCodeMaster != null
                    && tm.PolicyCodeMaster.IsRetroDemand ? tm.TaxAmount : 0m)
            })
            .ToDictionaryAsync(x => x.PropertyId, cancellationToken);

        return propertyIds.ToDictionary(
            propertyId => propertyId,
            propertyId =>
            {
                transDemand.TryGetValue(propertyId, out var trans);
                return new PropertyDemand(
                    oldPropertyByProperty.GetValueOrDefault(propertyId).OldTotalTax,
                    trans?.Current ?? 0m,
                    trans?.Retro ?? 0m);
            });
    }

    private async Task<Dictionary<int, PropertyRv>> GetRvByPropertyAsync(
        IReadOnlyCollection<int> propertyIds,
        IReadOnlyDictionary<int, OldPropertyValues> oldPropertyByProperty,
        CancellationToken cancellationToken)
    {
        // Revised RV: TOP 1 current PolicyTaxDetails.CalculationValue per property.
        var revisedRvByProperty = await _policyTaxDetailsRepository.GetQueryable().AsNoTracking()
            .Where(ptd => propertyIds.Contains(ptd.PropertyId)
                && ptd.IsCurrent
                && !ptd.MarkedForDeletion)
            .GroupBy(ptd => ptd.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                Amount = g.OrderByDescending(ptd => ptd.Id)
                    .Select(ptd => ptd.CalculationValue)
                    .FirstOrDefault() ?? 0m
            })
            .ToDictionaryAsync(x => x.PropertyId, x => x.Amount, cancellationToken);

        // Previous RV reuses the shared PropertyMapDetail → PropertyMastOld aggregate (OldRV).
        return propertyIds.ToDictionary(
            propertyId => propertyId,
            propertyId => new PropertyRv(
                oldPropertyByProperty.GetValueOrDefault(propertyId).OldRV,
                revisedRvByProperty.GetValueOrDefault(propertyId)));
    }

    private static List<WingWiseDetailDto> BuildWingDetails(
        IReadOnlyList<PropertyScopeRow> properties,
        IReadOnlyDictionary<int, PropertyArea> areaByProperty,
        IReadOnlyDictionary<int, string> floorCodeById,
        IReadOnlyDictionary<int, PropertyDemand> demandByProperty,
        IReadOnlyDictionary<int, PropertyRv> rvByProperty)
    {
        return properties
            .GroupBy(p => new
            {
                p.WingDetailId,
                p.WingMasterId,
                p.WingId,
                p.WingNo,
                p.WingName,
                p.SocietyDetailsMastId
            })
            .Select(wingGroup =>
            {
                var propertyTypeTotals = BuildPropertyTypeTotals(wingGroup, areaByProperty, demandByProperty);
                var oldDemand = propertyTypeTotals.Sum(x => x.OldDemand);
                var currentDemand = propertyTypeTotals.Sum(x => x.CurrentDemand);
                var retroDemand = propertyTypeTotals.Sum(x => x.RetroDemand);
                var totalDemand = currentDemand + retroDemand;
                var collectionAmount = 0m;
                var revenueImpact = propertyTypeTotals.Sum(x => x.TotalRevenue);
                var exemptionAppliedAmount = propertyTypeTotals
                    .Where(x => string.Equals(x.Name, AmenityPartType, StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalDemand);
                var wingPropertyIds = wingGroup.Select(x => x.PropertyId).Distinct().ToList();
                var previousRv = wingPropertyIds.Sum(id => rvByProperty.GetValueOrDefault(id).PreviousRV);
                var revisedRv = wingPropertyIds.Sum(id => rvByProperty.GetValueOrDefault(id).RevisedRV);
                var differenceRv = revisedRv - previousRv;
                var affectedUnits = GetAffectedUnits(wingGroup, rvByProperty);

                return new WingWiseDetailDto
                {
                    WingMasterId = wingGroup.Key.WingMasterId,
                    WingDetailId = wingGroup.Key.WingDetailId,
                    WingId = wingGroup.Key.WingId,
                    SocietyId = wingGroup.Key.SocietyDetailsMastId,
                    WingNo = wingGroup.Key.WingNo,
                    WingName = wingGroup.Key.WingName,
                    PropertyCount = wingPropertyIds.Count,
                    FloorRange = GetFloorRange(wingPropertyIds, areaByProperty, floorCodeById),
                    TotalArea = wingPropertyIds.Sum(id => areaByProperty.GetValueOrDefault(id).AreaSqFeet),
                    CollectionPercentage = Percentage(collectionAmount, totalDemand),
                    OldDemand = FormatDemand(oldDemand),
                    CurrentDemand = FormatDemand(currentDemand),
                    RetroDemand = FormatDemand(retroDemand),
                    TotalDemand = FormatDemand(totalDemand),
                    RevenueImpact = FormatDemand(revenueImpact),
                    RevenueImpactPercentage = Percentage(revenueImpact, oldDemand),
                    RevenueImpactDetails =
                    [
                        new RevenueImpactDetailDto
                        {
                            PreviousRV = FormatDemand(previousRv),
                            RevisedRV = FormatDemand(revisedRv),
                            DifferenceRV = FormatDemand(differenceRv),
                            AffectedUnits = affectedUnits
                        }
                    ],
                    ExemptionAppliedAmount = FormatDemand(exemptionAppliedAmount),
                    ExemptedPropertyCount = wingGroup
                        .Where(IsAmenity)
                        .Select(x => x.PropertyId)
                        .Distinct()
                        .Count(),
                    PropertyTypes = propertyTypeTotals.Select(ToPropertyTypeDto).ToList()
                };
            })
            .OrderBy(x => x.WingNo)
            .ThenBy(x => x.WingName)
            .ToList();
    }

    private static int GetAffectedUnits(
        IEnumerable<PropertyScopeRow> wingProperties,
        IReadOnlyDictionary<int, PropertyRv> rvByProperty)
    {
        return wingProperties
            .GroupBy(p => p.PartitionNo ?? string.Empty)
            .Count(partition => partition
                .Select(p => p.PropertyId)
                .Distinct()
                .Any(id =>
                {
                    var rv = rvByProperty.GetValueOrDefault(id);
                    return rv.PreviousRV != rv.RevisedRV;
                }));
    }

    private static List<PropertyTypeTotal> BuildPropertyTypeTotals(
        IEnumerable<PropertyScopeRow> wingProperties,
        IReadOnlyDictionary<int, PropertyArea> areaByProperty,
        IReadOnlyDictionary<int, PropertyDemand> demandByProperty)
    {
        var groupedData = wingProperties
            .GroupBy(GetPropertyTypeBucket)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var propertyIds = g.Select(x => x.PropertyId).Distinct().ToList();
                    var oldDemand = propertyIds.Sum(id => demandByProperty.GetValueOrDefault(id).OldDemand);
                    var currentDemand = propertyIds.Sum(id => demandByProperty.GetValueOrDefault(id).CurrentDemand);
                    var retroDemand = propertyIds.Sum(id => demandByProperty.GetValueOrDefault(id).RetroDemand);
                    var totalDemand = currentDemand + retroDemand;

                    return new PropertyTypeTotal(
                        g.Key,
                        propertyIds.Count,
                        propertyIds.Sum(id => areaByProperty.GetValueOrDefault(id).AreaSqFeet),
                        oldDemand,
                        currentDemand,
                        retroDemand,
                        totalDemand,
                        totalDemand - oldDemand);
                },
                StringComparer.OrdinalIgnoreCase);

        return PropertyTypeBuckets
            .Select(bucket =>
            {
                return groupedData.TryGetValue(bucket, out var total)
                    ? total
                    : new PropertyTypeTotal(bucket, 0, 0m, 0m, 0m, 0m, 0m, 0m);
            })
            .ToList();
    }

    private static WingWisePropertyTypeDetailDto ToPropertyTypeDto(PropertyTypeTotal total) => new()
    {
        PropertyTypeName = total.Name,
        PropertyCount = total.PropertyCount,
        TotalArea = total.AreaSqFeet,
        OldDemand = FormatDemand(total.OldDemand),
        CurrentDemand = FormatDemand(total.CurrentDemand),
        RetroDemand = FormatDemand(total.RetroDemand),
        TotalDemand = FormatDemand(total.TotalDemand),
        TotalRevenue = FormatDemand(total.TotalRevenue)
    };

    private static string GetFloorRange(IEnumerable<int> propertyIds,IReadOnlyDictionary<int, PropertyArea> areaByProperty,IReadOnlyDictionary<int, string> floorCodeById)
    {
        var propertyAreas = propertyIds
            .Select(id => areaByProperty.GetValueOrDefault(id))
            .ToList();
        var minFloorId = propertyAreas
            .Where(area => area.MinFloorId.HasValue)
            .Min(area => area.MinFloorId);
        var maxFloorId = propertyAreas
            .Where(area => area.MaxFloorId.HasValue)
            .Max(area => area.MaxFloorId);

        if (!minFloorId.HasValue || !maxFloorId.HasValue)
            return string.Empty;

        var minFloorCode = floorCodeById.GetValueOrDefault(minFloorId.Value) ?? string.Empty;
        var maxFloorCode = floorCodeById.GetValueOrDefault(maxFloorId.Value) ?? string.Empty;

        return string.IsNullOrWhiteSpace(minFloorCode) || string.IsNullOrWhiteSpace(maxFloorCode)
            ? string.Empty
            : $"{minFloorCode}-{maxFloorCode}";
    }

    private static WingWiseDetailsResponseDto BuildResponse(int propertyId,string propertyNo,string wardNo,IReadOnlyList<WingWiseDetailDto> wings) => new()
    {
            PropertyId = propertyId,
            PropertyNo = propertyNo,
            WardNo = wardNo,
            SocietyId = wings.FirstOrDefault(w => w.SocietyId.HasValue && w.SocietyId.Value > 0)?.SocietyId,
            Wings = wings
    };

    private static bool IsAmenity(PropertyScopeRow property) =>
        string.Equals(property.PartType, AmenityPartType, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> PropertyTypeBuckets { get; } =
    [
        ResidentialPropertyType,
        CommercialPropertyType,
        AmenityPartType
    ];

    private static string GetPropertyTypeBucket(PropertyScopeRow property)
    {
        if (IsAmenity(property))
            return AmenityPartType;

        if (ContainsIgnoreCase(property.PartType, ResidentialPropertyType)
            || ContainsIgnoreCase(property.PropertyTypeName, ResidentialPropertyType)
            || string.Equals(property.PartType, "R", StringComparison.OrdinalIgnoreCase))
            return ResidentialPropertyType;

        return CommercialPropertyType;
    }

    private static bool ContainsIgnoreCase(string? value, string expected) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(expected, StringComparison.OrdinalIgnoreCase);

    private static decimal Percentage(decimal value, decimal total) =>
        total == 0m ? 0m : Math.Round(value * 100m / total, 2, MidpointRounding.AwayFromZero);

    private static string FormatDemand(decimal amount)
    {
        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var absoluteAmount = Math.Abs(roundedAmount);
        var sign = roundedAmount < 0m ? "-" : string.Empty;

        if (absoluteAmount >= 10000000m)
            return sign + FormatScaledAmount(absoluteAmount / 10000000m, "Crore");

        if (absoluteAmount >= 100000m)
            return sign + FormatScaledAmount(absoluteAmount / 100000m, "Lakh");

        if (absoluteAmount >= 1000m)
            return sign + FormatScaledAmount(absoluteAmount / 1000m, "K");

        return sign + FormatNumber(absoluteAmount);
    }

    private static string FormatScaledAmount(decimal value, string suffix) =>
        string.Equals(suffix, "K", StringComparison.Ordinal)
            ? $"{FormatNumber(value)}{suffix}"
            : $"{FormatNumber(value)} {suffix}";

    private static string FormatNumber(decimal value) =>
        decimal.Truncate(value) == value
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static WingWiseDetailsResponseDto Empty(WingWiseDetailsQueryParameters query) =>
        Empty(query.WardId ?? 0, query.PropertyNo?.Trim() ?? string.Empty);

    private static WingWiseDetailsResponseDto Empty(int wardId, string propertyNo) => new()
    {
        PropertyNo = propertyNo
    };

    private class PropertyScopeRow
    {
        public int PropertyId { get; init; }
        public string? PartitionNo { get; init; }
        public int? PropertyTypeId { get; init; }
        public string PropertyTypeName { get; init; } = string.Empty;
        public string? PartType { get; init; }
        public int WingDetailId { get; init; }
        public int WingMasterId { get; init; }
        public int WingId { get; init; }
        public string WingNo { get; init; } = string.Empty;
        public string? WingName { get; init; }
        public int SocietyDetailsMastId { get; init; }
    }

    private readonly record struct PropertyArea(int? MinFloorId, int? MaxFloorId, decimal AreaSqFeet);

    private readonly record struct PropertyDemand(decimal OldDemand, decimal CurrentDemand, decimal RetroDemand);

    private readonly record struct PropertyRv(decimal PreviousRV, decimal RevisedRV);

    private readonly record struct OldPropertyValues(decimal OldTotalTax, decimal OldRV);

    private readonly record struct PropertyTypeTotal(
        string Name,
        int PropertyCount,
        decimal AreaSqFeet,
        decimal OldDemand,
        decimal CurrentDemand,
        decimal RetroDemand,
        decimal TotalDemand,
        decimal TotalRevenue);
}
