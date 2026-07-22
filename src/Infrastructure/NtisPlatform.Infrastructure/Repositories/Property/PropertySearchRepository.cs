using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>
/// Data-access implementation for the Property Search screen: the multi-criteria property
/// search (Quick Search / KYC Search) and the dashboard count statistics.
/// Pure querying only - no SaveChanges and no business messages.
/// </summary>
public class PropertySearchRepository : IPropertySearchRepository
{
    private const string ApartmentCategoryName = "Apartment";
    private const string TaxTotalCode = "TaxTotal";
    private const string TaxTotalName = "TaxTotal";
    private const string AssessedStatusName = "ASSESSED";
    private const string UnassessedStatusName = "UNASSESSED";

    private readonly ApplicationDbContext _context;
    private string[]? _cachedValuationMethods;

    public PropertySearchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets valid valuation methods from PolicyConfiguration (RV and CV only).
    /// Falls back to RV,CV if not configured in database.
    /// </summary>
    private async Task<string[]> GetValidValuationMethodsAsync(CancellationToken cancellationToken = default)
    {
        // Return cached value if available
        if (_cachedValuationMethods != null)
            return _cachedValuationMethods;

        try
        {
            var policy = await _context.PolicyConfiguration
                .AsNoTracking()
                .Where(p => p.IsActive && p.PolicyCode == "TaxCalculationMethod")
                .FirstOrDefaultAsync(cancellationToken);

            if (policy?.AllowedValues != null)
            {
                _cachedValuationMethods = policy.AllowedValues
                    .Split(',')
                    .Select(x => x.Trim().ToUpper())
                    .ToArray();
            }
            else
            {
                // Fallback to RV and CV only
                _cachedValuationMethods = new[] { "RV", "CV" };
            }
        }
        catch
        {
            // If error reading from database, use default values (RV and CV only)
            _cachedValuationMethods = new[] { "RV", "CV" };
        }

        return _cachedValuationMethods;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Public: grid search
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<(int TotalCount, List<PropertySearchResponseDto> Items)> SearchPropertiesAsync(
        PropertySearchRequestDto searchRequest,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Handle dashboard card filters
        if (searchRequest.DashboardFilter.HasValue)
        {
            switch (searchRequest.DashboardFilter.Value)
            {
                case DashboardFilterType.RegisteredProperty:
                case DashboardFilterType.GeoSequencing:
                    break;

                case DashboardFilterType.Survey:
                case DashboardFilterType.DataProcessing:
                case DashboardFilterType.QualityAnalysis:
                case DashboardFilterType.AssessmentCompleted:
                    return (0, new List<PropertySearchResponseDto>());
            }
        }

        if (searchRequest.PropertyProcessFilter.HasValue)
        {
            switch (searchRequest.PropertyProcessFilter.Value)
            {
                case PropertyProcessFilterType.SurveyCompleted:
                case PropertyProcessFilterType.DataEntryCompleted:
                case PropertyProcessFilterType.QCCompleted:
                case PropertyProcessFilterType.NoticeDistributed:
                    return (0, new List<PropertySearchResponseDto>());
            }
        }


        var query = from p in _context.PropertyMast.AsNoTracking()
                    where p.IsActive && !p.MarkedForDeletion

                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join z in _context.ZoneMaster.AsNoTracking() on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
                    from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into propertyTypeJoin
                    from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pmo in _context.PropertyMastOld.AsNoTracking() on p.PropertyMastOldId equals pmo.Id into oldJoin
                    from pmo in oldJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sd in _context.SocietyDetailsMast.AsNoTracking() on p.SocietyDetailId equals sd.Id into societyJoin
                    from sd in societyJoin.Where(x => x.IsActive && !x.MarkedForDeletion).DefaultIfEmpty()

                    select new
                    {
                        Property = p,
                        Ward = w,
                        Zone = z,
                        Category = pc,
                        PropertyType = pt,
                        OldProperty = pmo,
                        Society = sd
                    };

        // Exclude incomplete entries that have no Zone/Ward and no PropertyNo/OldPropertyNo
        query = query.Where(x =>
            (x.Ward != null || x.Zone != null) &&
            (!string.IsNullOrEmpty(x.Property.PropertyNo) || (x.OldProperty != null && !string.IsNullOrEmpty(x.OldProperty.OldPropertyNo)))
        );

        if (searchRequest.DashboardFilter == DashboardFilterType.GeoSequencing)
            query = query.Where(x => !string.IsNullOrEmpty(x.Property.PropertyNo));

        // ── Common top-row filters ───────────────────────────────────────────
        if (searchRequest.PropertyAssessmentStatusId.HasValue)
            query = query.Where(x => x.Property.PropertyAssessmentStatusId == searchRequest.PropertyAssessmentStatusId.Value);

        if (searchRequest.PropertyDescriptionId.HasValue)
            query = query.Where(x => x.Property.PropertyTypeId == searchRequest.PropertyDescriptionId.Value);

        if (searchRequest.ZoneId.HasValue)
            query = query.Where(x => x.Zone != null && x.Zone.Id == searchRequest.ZoneId.Value);

        if (searchRequest.WardId.HasValue)
            query = query.Where(x => x.Property.WardId == searchRequest.WardId.Value);

        // ── Quick Search filters ─────────────────────────────────────────────
        if (searchRequest.PropertyTypeId.HasValue)
            query = query.Where(x => x.Property.PropertyTypeId == searchRequest.PropertyTypeId.Value);

        if (searchRequest.CategoryId.HasValue)
            query = query.Where(x => x.Property.CategoryId == searchRequest.CategoryId.Value);

        if (searchRequest.TypeOfUseId.HasValue)
        {
            var propertyIdsWithTypeOfUse = _context.PropertyDetails
                .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.TypeOfUseId == searchRequest.TypeOfUseId.Value)
                .Select(pd => pd.PropertyId)
                .Distinct();

            query = query.Where(x => propertyIdsWithTypeOfUse.Contains(x.Property.Id));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom) && !string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
        {
            var fromStr = searchRequest.PropertyNoFrom.Trim();
            var toStr = searchRequest.PropertyNoTo.Trim();
            bool fromIsNum = long.TryParse(fromStr, out _);
            bool toIsNum = long.TryParse(toStr, out _);

            if (fromIsNum && toIsNum)
            {
                int fromLen = fromStr.Length;
                int toLen = toStr.Length;

                if (fromLen == toLen)
                {
                    query = query.Where(x => x.Property.PropertyNo != null &&
                                             x.Property.PropertyNo.Length == fromLen &&
                                             string.Compare(x.Property.PropertyNo, fromStr) >= 0 &&
                                             string.Compare(x.Property.PropertyNo, toStr) <= 0);
                }
                else
                {
                    query = query.Where(x => x.Property.PropertyNo != null &&
                                             x.Property.PropertyNo.Length >= fromLen &&
                                             x.Property.PropertyNo.Length <= toLen &&
                                             (x.Property.PropertyNo.Length > fromLen || string.Compare(x.Property.PropertyNo, fromStr) >= 0) &&
                                             (x.Property.PropertyNo.Length < toLen || string.Compare(x.Property.PropertyNo, toStr) <= 0));
                }
            }
            else
            {
                query = query.Where(x => x.Property.PropertyNo != null &&
                                         string.Compare(x.Property.PropertyNo, fromStr) >= 0 &&
                                         string.Compare(x.Property.PropertyNo, toStr) <= 0);
            }
        }
        else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom))
        {
            var propNoFrom = searchRequest.PropertyNoFrom.Trim();
            query = query.Where(x => x.Property.PropertyNo != null && x.Property.PropertyNo == propNoFrom);
        }
        else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
        {
            var toStr = searchRequest.PropertyNoTo.Trim();
            bool toIsNum = long.TryParse(toStr, out _);

            if (toIsNum)
            {
                int toLen = toStr.Length;
                query = query.Where(x => x.Property.PropertyNo != null &&
                                         (x.Property.PropertyNo.Length < toLen ||
                                          (x.Property.PropertyNo.Length == toLen && string.Compare(x.Property.PropertyNo, toStr) <= 0)));
            }
            else
            {
                query = query.Where(x => x.Property.PropertyNo != null &&
                                         string.Compare(x.Property.PropertyNo, toStr) <= 0);
            }
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OldPropertyNo))
            query = query.Where(x => x.OldProperty != null &&
                                     x.OldProperty.OldPropertyNo != null &&
                                     x.OldProperty.OldPropertyNo.Contains(searchRequest.OldPropertyNo));

        if (!string.IsNullOrWhiteSpace(searchRequest.UPICId))
            query = query.Where(x => x.Property.UPICId != null && x.Property.UPICId.Contains(searchRequest.UPICId));

        if (!string.IsNullOrWhiteSpace(searchRequest.CSN))
            query = query.Where(x => x.Property.CSN != null && x.Property.CSN.Contains(searchRequest.CSN));

        if (!string.IsNullOrWhiteSpace(searchRequest.SubZoneNo))
            query = query.Where(x => x.Property.SubZoneNo != null && x.Property.SubZoneNo.Contains(searchRequest.SubZoneNo));

        if (!string.IsNullOrWhiteSpace(searchRequest.PlotNo))
            query = query.Where(x => x.Property.PlotNo != null && x.Property.PlotNo.Contains(searchRequest.PlotNo));

        // ── KYC Search filters ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(searchRequest.MobileNo))
        {
            query = query.Where(x =>
                (x.Property.MobileNo != null && x.Property.MobileNo.Contains(searchRequest.MobileNo)) ||
                (x.Property.AlternateMobileNo != null && x.Property.AlternateMobileNo.Contains(searchRequest.MobileNo)) ||
                (x.Property.OccupierMobileNo != null && x.Property.OccupierMobileNo.Contains(searchRequest.MobileNo)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OwnerName))
        {
            query = query.Where(x =>
                (x.Property.OwnerName != null && x.Property.OwnerName.Contains(searchRequest.OwnerName)) ||
                (x.Property.OwnerNameEnglish != null && x.Property.OwnerNameEnglish.Contains(searchRequest.OwnerName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OccupierName))
        {
            query = query.Where(x =>
                (x.Property.OccupierName != null && x.Property.OccupierName.Contains(searchRequest.OccupierName)) ||
                (x.Property.OccupierNameEnglish != null && x.Property.OccupierNameEnglish.Contains(searchRequest.OccupierName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.FlatOrShopName))
        {
            query = query.Where(x =>
                (x.Property.FlatOrShopName != null && x.Property.FlatOrShopName.Contains(searchRequest.FlatOrShopName)) ||
                (x.Property.FlatOrShopNo != null && x.Property.FlatOrShopNo.Contains(searchRequest.FlatOrShopName)) ||
                (x.Property.FlatOrShopNameEnglish != null && x.Property.FlatOrShopNameEnglish.Contains(searchRequest.FlatOrShopName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.SocietyName))
        {
            query = query.Where(x =>
                (x.Society != null && x.Society.SocietyName != null && x.Society.SocietyName.Contains(searchRequest.SocietyName)) ||
                (x.Society != null && x.Society.SocietyNameEnglish != null && x.Society.SocietyNameEnglish.Contains(searchRequest.SocietyName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.Address))
        {
            query = query.Where(x =>
                (x.Property.Address != null && x.Property.Address.Contains(searchRequest.Address)) ||
                (x.Property.AddressEnglish != null && x.Property.AddressEnglish.Contains(searchRequest.Address)));
        }

        // Values & Dues filters - filter by RV, CV, or Total Tax from PolicyConfiguration
        if (!string.IsNullOrWhiteSpace(searchRequest.ValuationMethod) && !string.IsNullOrWhiteSpace(searchRequest.FilterType))
        {
            var valuationMethod = searchRequest.ValuationMethod.Trim().ToUpper();
            var filterType = searchRequest.FilterType.Trim();
            var validMethods = await GetValidValuationMethodsAsync(cancellationToken);

            if (validMethods.Contains(valuationMethod) && searchRequest.AmountValue.HasValue)
            {
                var amount = searchRequest.AmountValue.Value;

                // Handle RV or CV filtering (from TransMast with CalculationType field)
                if (valuationMethod == "RV" || valuationMethod == "CV")
                {
                    var rvOrCv = valuationMethod;

                    // Get matching property IDs from TransMast
                    var matchingPropertyIds = _context.TransMast
                        .AsNoTracking()
                        .Where(t => t.IsActive && !t.MarkedForDeletion && t.CalculationType == rvOrCv)
                        .GroupBy(t => t.PropertyId)
                        .Select(g => new { PropertyId = g.Key, Value = g.Max(x => x.CalculationValue) })
                        .Where(x =>
                            (filterType.Equals("Exact Value", StringComparison.OrdinalIgnoreCase) && x.Value >= amount * 0.99m && x.Value <= amount * 1.01m) ||
                            (filterType.Equals("More Than", StringComparison.OrdinalIgnoreCase) && x.Value > amount) ||
                            (filterType.Equals("Less Than", StringComparison.OrdinalIgnoreCase) && x.Value < amount) ||
                            (filterType.Equals("Between", StringComparison.OrdinalIgnoreCase) && searchRequest.AmountTo.HasValue && x.Value >= amount && x.Value <= searchRequest.AmountTo.Value))
                        .Select(x => x.PropertyId);

                    query = query.Where(x => matchingPropertyIds.Contains(x.Property.Id));
                }
            }
        }

        // Exclude apartment units from grid results: show only structures/main properties,
        // UNLESS the user is explicitly searching by specific text search criteria (UPICId, Address, Owner, etc.)
        bool isSpecificSearch = !string.IsNullOrWhiteSpace(searchRequest.UPICId) ||
                                !string.IsNullOrWhiteSpace(searchRequest.Address) ||
                                !string.IsNullOrWhiteSpace(searchRequest.MobileNo) ||
                                !string.IsNullOrWhiteSpace(searchRequest.OwnerName) ||
                                !string.IsNullOrWhiteSpace(searchRequest.OccupierName) ||
                                !string.IsNullOrWhiteSpace(searchRequest.FlatOrShopName) ||
                                !string.IsNullOrWhiteSpace(searchRequest.SocietyName) ||
                                !string.IsNullOrWhiteSpace(searchRequest.OldPropertyNo) ||
                                !string.IsNullOrWhiteSpace(searchRequest.CSN) ||
                                !string.IsNullOrWhiteSpace(searchRequest.PlotNo) ||
                                !string.IsNullOrWhiteSpace(searchRequest.SubZoneNo) ||
                                !string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom) ||
                                !string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo);

        if (!isSpecificSearch)
        {
            query = query.Where(x => x.Category == null ||
                                    x.Category.PropertyCategoryName != ApartmentCategoryName ||
                                    (x.Category.PropertyCategoryName == ApartmentCategoryName &&
                                     (string.IsNullOrEmpty(x.Property.PartitionNo))));
        }

        var isTopNFilter = !string.IsNullOrWhiteSpace(searchRequest.FilterType)
            && searchRequest.FilterType.Trim().Equals("Top", StringComparison.OrdinalIgnoreCase)
            && searchRequest.TopCount.HasValue
            && searchRequest.TopCount.Value > 0;

        // For Top N filter, we need valuation values before paging
        Dictionary<int, decimal>? rvDictionary = null;
        Dictionary<int, decimal>? cvDictionary = null;
        Dictionary<int, decimal>? totalTaxDictionary = null;

        if (isTopNFilter)
        {
            var allPropertyIds = await query.Select(x => x.Property.Id).ToListAsync(cancellationToken);

            // Get RV values
            var rvValues = await _context.TransMast
                .AsNoTracking()
                .Where(t =>
                    allPropertyIds.Contains(t.PropertyId)
                    && t.IsActive
                    && !t.MarkedForDeletion
                    && t.CalculationType == "RV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    RateableValue = g.Max(x => x.CalculationValue)
                })
                .ToListAsync(cancellationToken);

            // Get CV values
            var cvValues = await _context.TransMast
                .AsNoTracking()
                .Where(t =>
                    allPropertyIds.Contains(t.PropertyId)
                    && t.IsActive
                    && !t.MarkedForDeletion
                    && t.CalculationType == "CV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    CapitalValue = g.Max(x => x.CalculationValue)
                })
                .ToListAsync(cancellationToken);

            // Get TaxTotal values
            var totalTaxAmounts = await (
                from t in _context.TransMast.AsNoTracking()
                join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
                where allPropertyIds.Contains(t.PropertyId)
                      && t.IsActive
                      && !t.MarkedForDeletion
                      && tax.IsActive
                      && tax.TaxCode == TaxTotalCode
                      && tax.TaxName == TaxTotalName
                group t by t.PropertyId into g
                select new { PropertyId = g.Key, TotalTax = g.Sum(x => x.TaxAmount) }
            ).ToListAsync(cancellationToken);

            rvDictionary = rvValues.ToDictionary(x => x.PropertyId, x => x.RateableValue);
            cvDictionary = cvValues.ToDictionary(x => x.PropertyId, x => x.CapitalValue);
            totalTaxDictionary = totalTaxAmounts.ToDictionary(x => x.PropertyId, x => x.TotalTax);

            // Apply Top N sorting BEFORE pagination
            var valuationMethod = searchRequest.ValuationMethod?.Trim().ToUpper();
            List<int> sortedIds;
            if (valuationMethod == "RV")
                sortedIds = rvDictionary.OrderByDescending(x => x.Value).Take(searchRequest.TopCount.Value).Select(x => x.Key).ToList();
            else if (valuationMethod == "CV")
                sortedIds = cvDictionary.OrderByDescending(x => x.Value).Take(searchRequest.TopCount.Value).Select(x => x.Key).ToList();
            else if (valuationMethod == "TOTAL TAX")
                sortedIds = totalTaxDictionary.OrderByDescending(x => x.Value).Take(searchRequest.TopCount.Value).Select(x => x.Key).ToList();
            else
                sortedIds = await query.Select(x => x.Property.Id).ToListAsync(cancellationToken);

            query = query.Where(x => sortedIds.Contains(x.Property.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = query.OrderBy(x => x.Property.Id);

        var isUnpaged = pageSize == -1;
        var skip = isUnpaged ? 0 : (pageNumber - 1) * pageSize;

        var propertyResults = await (isUnpaged ? orderedQuery : orderedQuery.Skip(skip).Take(pageSize)).ToListAsync(cancellationToken);

        if (!propertyResults.Any())
            return (totalCount, new List<PropertySearchResponseDto>());

        var propertyIds = propertyResults.Select(x => x.Property.Id).ToList();

        // Load valuation values if not already loaded for Top N filter
        if (!isTopNFilter)
        {
            // RV (Rateable Value) from TransMast where CalculationType = 'RV'
            var rvValues = await _context.TransMast
                .AsNoTracking()
                .Where(t =>
                    propertyIds.Contains(t.PropertyId)
                    && t.IsActive
                    && !t.MarkedForDeletion
                    && t.CalculationType == "RV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    RateableValue = g.Max(x => x.CalculationValue)
                })
                .ToListAsync(cancellationToken);

            // CV (Capital Value) from TransMast where CalculationType = 'CV'
            var cvValues = await _context.TransMast
                .AsNoTracking()
                .Where(t =>
                    propertyIds.Contains(t.PropertyId)
                    && t.IsActive
                    && !t.MarkedForDeletion
                    && t.CalculationType == "CV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    CapitalValue = g.Max(x => x.CalculationValue)
                })
                .ToListAsync(cancellationToken);

            // TaxTotal from TransMast joined with TaxMaster (TaxCode='TaxTotal', TaxName='TaxTotal')
            // Sum all TaxAmounts across all finance years for the property
            var totalTaxAmounts = await (
                from t in _context.TransMast.AsNoTracking()
                join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
                where propertyIds.Contains(t.PropertyId)
                      && t.IsActive
                      && !t.MarkedForDeletion
                      && tax.IsActive
                      && tax.TaxCode == TaxTotalCode
                      && tax.TaxName == TaxTotalName
                group t by t.PropertyId into g
                select new { PropertyId = g.Key, TotalTax = g.Sum(x => x.TaxAmount) }
            ).ToListAsync(cancellationToken);

            rvDictionary = rvValues.ToDictionary(x => x.PropertyId, x => x.RateableValue);
            cvDictionary = cvValues.ToDictionary(x => x.PropertyId, x => x.CapitalValue);
            totalTaxDictionary = totalTaxAmounts.ToDictionary(x => x.PropertyId, x => x.TotalTax);
        }

        // Pre-calculate unit counts for apartment main properties (empty PartitionNo)
        var apartmentMainProperties = propertyResults
            .Where(x => x.Category?.PropertyCategoryName == ApartmentCategoryName &&
                       string.IsNullOrEmpty(x.Property.PartitionNo))
            .Select(x => new { x.Property.PropertyNo, x.Property.WardId })
            .Distinct()
            .ToList();

        var mainPropNos = apartmentMainProperties.Select(a => a.PropertyNo).Distinct().ToList();
        var mainWardIds = apartmentMainProperties.Select(a => a.WardId).Distinct().ToList();

        var unitCountsList = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion &&
                       mainPropNos.Contains(p.PropertyNo) &&
                       mainWardIds.Contains(p.WardId) &&
                       !string.IsNullOrEmpty(p.PartitionNo))
            .GroupBy(p => new { p.PropertyNo, p.WardId })
            .Select(g => new { g.Key.PropertyNo, g.Key.WardId, UnitCount = g.Count() })
            .ToListAsync(cancellationToken);

        var unitCountsDictionary = unitCountsList.ToDictionary(
            x => $"{x.PropertyNo}_{x.WardId}",
            x => x.UnitCount
        );

        var result = propertyResults.Select(pr =>
        {
            decimal? rv = null;
            decimal? cv = null;
            decimal? totalTax = null;
            int? childUnitCount = null;

            if (rvDictionary != null && rvDictionary.TryGetValue(pr.Property.Id, out var rvValue))
                rv = rvValue;
            if (cvDictionary != null && cvDictionary.TryGetValue(pr.Property.Id, out var cvValue))
                cv = cvValue;
            if (totalTaxDictionary != null && totalTaxDictionary.TryGetValue(pr.Property.Id, out var totalTaxValue))
                totalTax = totalTaxValue;

            // Add child unit count for apartment main properties
            if (pr.Category?.PropertyCategoryName == ApartmentCategoryName &&
                string.IsNullOrEmpty(pr.Property.PartitionNo))
            {
                var key = $"{pr.Property.PropertyNo}_{pr.Property.WardId}";
                if (unitCountsDictionary.TryGetValue(key, out var unitCount))
                {
                    childUnitCount = unitCount;
                }
                else
                {
                    childUnitCount = 0;
                }
            }

            return new PropertySearchResponseDto
            {
                PropertyId = pr.Property.Id,
                UPICId = pr.Property.UPICId,
                ZoneName = pr.Zone?.Description,
                WardName = pr.Ward?.WardNo,
                PropertyNo = pr.Property.PropertyNo,
                PartitionNo = pr.Property.PartitionNo,
                OldPropertyNo = pr.OldProperty?.OldPropertyNo,
                CitySurveyNo = pr.Property.CSN,
                PlotNo = pr.Property.PlotNo,
                WingFlatNo = pr.Property.FlatOrShopNo,
                CategoryName = pr.Category?.PropertyCategoryName,
                PropertyDescription = pr.PropertyType?.PropertyDescription,
                Mobile = pr.Property.MobileNo,
                PropertyHolderName = pr.Property.OwnerName ?? pr.Property.OwnerNameEnglish,
                OccupierName = pr.Property.OccupierName ?? pr.Property.OccupierNameEnglish,
                ShopBuildingName = pr.Property.FlatOrShopName ?? pr.Property.FlatOrShopNameEnglish,
                SocietyName = pr.Society?.SocietyName ?? pr.Society?.SocietyNameEnglish,
                Address = pr.Property.Address ?? pr.Property.AddressEnglish,
                RV = rv,
                CV = cv,
                TotalTax = totalTax,
                ChildUnitCount = childUnitCount
            };
        }).ToList();

        return (totalCount, result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Public: dashboard main cards
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<MainCardsResponseDto> GetMainCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion && p.PropertyNo != null && p.PropertyNo != "");

        baseQuery = ApplyDashboardFilters(baseQuery, searchRequest);

        var previouslyRegistered = await CalculatePreviouslyRegisteredAsync(baseQuery, cancellationToken);
        var assessed = await CalculateByAssessmentStatusAsync(baseQuery, AssessedStatusName, true, cancellationToken);
        var unassessed = await CalculateByAssessmentStatusAsync(baseQuery, UnassessedStatusName, false, cancellationToken);
        var additionalRevenue = await CalculateAdditionalRevenueAsync(baseQuery, cancellationToken);

        return new MainCardsResponseDto
        {
            PreviouslyRegistered = previouslyRegistered,
            AssessmentApproved = new AssessmentApprovedDto
            {
                Assessed = assessed,
                Unassessed = unassessed
            },
            AdditionalRevenueGenerated = additionalRevenue
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Public: workflow cards
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PropertyWorkflowStageMasterEntity> stagesBaseQuery = _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .Where(s => s.IsActive);

        // When a specific stage is requested, restrict to that stage only
        if (searchRequest?.WorkflowStageId.HasValue == true)
            stagesBaseQuery = stagesBaseQuery.Where(s => s.Id == searchRequest.WorkflowStageId.Value);

        var stages = await stagesBaseQuery
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);

        var cards = new List<WorkflowStageCardDto>(stages.Count);

        foreach (var stage in stages)
        {
            var propertiesQuery = _context.PropertyWorkflowDetails
                .AsNoTracking()
                .Where(d => d.IsActive && d.WorkflowStageId == stage.Id &&
                            d.Property.IsActive && !d.Property.MarkedForDeletion &&
                            d.Property.PropertyNo != null && d.Property.PropertyNo != "")
                .Select(d => d.Property)
                .Distinct();

            propertiesQuery = ApplyDashboardFilters(propertiesQuery, searchRequest);

            var (propertyCount, structureCount, unitCount) = await CountPropertiesAsync(propertiesQuery, cancellationToken);

            cards.Add(new WorkflowStageCardDto
            {
                StageName = stage.StageName,
                PropertyCount = propertyCount,
                StructureCount = structureCount,
                UnitCount = unitCount
            });
        }

        return cards;
    }

    public async Task<ApartmentUnitListResponseDto> GetApartmentUnitListAsync(
        int propertyId,
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
    {
        PropertyEntity? parentProperty = null;

        if (propertyId > 0)
        {
            parentProperty = await _context.PropertyMast
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);
        }
        else if (searchRequest != null)
        {
            if (!string.IsNullOrWhiteSpace(searchRequest.UPICId))
            {
                parentProperty = await _context.PropertyMast
                    .AsNoTracking()
.FirstOrDefaultAsync(p => p.UPICId != null && p.UPICId == searchRequest.UPICId!.Trim() && p.IsActive && !p.MarkedForDeletion, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom))
            {
                parentProperty = await _context.PropertyMast
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PropertyNo == searchRequest.PropertyNoFrom && p.IsActive && !p.MarkedForDeletion, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
            {
                parentProperty = await _context.PropertyMast
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PropertyNo == searchRequest.PropertyNoTo && p.IsActive && !p.MarkedForDeletion, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(searchRequest.OldPropertyNo))
            {
                var oldProp = await _context.PropertyMastOld
                    .AsNoTracking()
.FirstOrDefaultAsync(o => o.OldPropertyNo != null && o.OldPropertyNo == searchRequest.OldPropertyNo!.Trim() && o.IsActive && !o.MarkedForDeletion, cancellationToken);
                if (oldProp != null)
                {
                    parentProperty = await _context.PropertyMast
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PropertyMastOldId == oldProp.Id && p.IsActive && !p.MarkedForDeletion, cancellationToken);
                }
            }
        }

        if (parentProperty == null)
            return new ApartmentUnitListResponseDto { Items = new List<PropertySearchResponseDto>(), TotalCount = 0 };

        var childrenQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion && p.PropertyNo == parentProperty.PropertyNo && p.WardId == parentProperty.WardId && p.Id != parentProperty.Id);

        if (string.IsNullOrEmpty(parentProperty.PartitionNo))
        {
            childrenQuery = childrenQuery.Where(p => p.PartitionNo != null && p.PartitionNo != "");
        }
        else
        {
            childrenQuery = childrenQuery.Where(p => p.PartitionNo == parentProperty.PartitionNo);
        }

        var query = from p in childrenQuery
                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join z in _context.ZoneMaster.AsNoTracking() on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
                    from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into propertyTypeJoin
                    from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pmo in _context.PropertyMastOld.AsNoTracking() on p.PropertyMastOldId equals pmo.Id into oldJoin
                    from pmo in oldJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sd in _context.SocietyDetailsMast.AsNoTracking() on p.SocietyDetailId equals sd.Id into societyJoin
                    from sd in societyJoin.Where(x => x.IsActive && !x.MarkedForDeletion).DefaultIfEmpty()

                    select new
                    {
                        Property = p,
                        Ward = w,
                        Zone = z,
                        Category = pc,
                        PropertyType = pt,
                        OldProperty = pmo,
                        Society = sd
                    };

        // Apply all grid filters if searchRequest is provided
        if (searchRequest != null)
        {
            // Property Type filter
            if (searchRequest.PropertyTypeId.HasValue)
            {
                query = query.Where(x => x.Property.PropertyTypeId == searchRequest.PropertyTypeId);
            }

            // Type of Use filter
            if (searchRequest.TypeOfUseId.HasValue)
            {
                var propertyIdsWithTypeOfUse = _context.PropertyDetails
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.TypeOfUseId == searchRequest.TypeOfUseId.Value)
                    .Select(pd => pd.PropertyId)
                    .Distinct();

                query = query.Where(x => propertyIdsWithTypeOfUse.Contains(x.Property.Id));
            }

            // Zone filter
            if (searchRequest.ZoneId.HasValue)
            {
                query = query.Where(x => x.Zone != null && x.Zone.Id == searchRequest.ZoneId);
            }

            // Ward filter
            if (searchRequest.WardId.HasValue)
            {
                query = query.Where(x => x.Ward != null && x.Ward.Id == searchRequest.WardId);
            }

            // Category filter
            if (searchRequest.CategoryId.HasValue)
            {
                query = query.Where(x => x.Property.CategoryId == searchRequest.CategoryId);
            }

            // Property No range filter
            if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom) && !string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
            {
                var fromStr = searchRequest.PropertyNoFrom.Trim();
                var toStr = searchRequest.PropertyNoTo.Trim();
                bool fromIsNum = long.TryParse(fromStr, out _);
                bool toIsNum = long.TryParse(toStr, out _);

                if (fromIsNum && toIsNum)
                {
                    int fromLen = fromStr.Length;
                    int toLen = toStr.Length;

                    if (fromLen == toLen)
                    {
                        query = query.Where(x => x.Property.PropertyNo != null &&
                                                 x.Property.PropertyNo.Length == fromLen &&
                                                 string.Compare(x.Property.PropertyNo, fromStr) >= 0 &&
                                                 string.Compare(x.Property.PropertyNo, toStr) <= 0);
                    }
                    else
                    {
                        query = query.Where(x => x.Property.PropertyNo != null &&
                                                 x.Property.PropertyNo.Length >= fromLen &&
                                                 x.Property.PropertyNo.Length <= toLen &&
                                                 (x.Property.PropertyNo.Length > fromLen || string.Compare(x.Property.PropertyNo, fromStr) >= 0) &&
                                                 (x.Property.PropertyNo.Length < toLen || string.Compare(x.Property.PropertyNo, toStr) <= 0));
                    }
                }
                else
                {
                    query = query.Where(x => x.Property.PropertyNo != null &&
                                             string.Compare(x.Property.PropertyNo, fromStr) >= 0 &&
                                             string.Compare(x.Property.PropertyNo, toStr) <= 0);
                }
            }
            else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom))
            {
                var propNoFrom = searchRequest.PropertyNoFrom.Trim();
                query = query.Where(x => x.Property.PropertyNo != null && x.Property.PropertyNo == propNoFrom);
            }
            else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
            {
                var toStr = searchRequest.PropertyNoTo.Trim();
                bool toIsNum = long.TryParse(toStr, out _);

                if (toIsNum)
                {
                    int toLen = toStr.Length;
                    query = query.Where(x => x.Property.PropertyNo != null &&
                                             (x.Property.PropertyNo.Length < toLen ||
                                              (x.Property.PropertyNo.Length == toLen && string.Compare(x.Property.PropertyNo, toStr) <= 0)));
                }
                else
                {
                    query = query.Where(x => x.Property.PropertyNo != null &&
                                             string.Compare(x.Property.PropertyNo, toStr) <= 0);
                }
            }

            // Old Property No filter
            if (!string.IsNullOrWhiteSpace(searchRequest.OldPropertyNo))
            {
                query = query.Where(x => x.OldProperty != null && x.OldProperty.OldPropertyNo != null && x.OldProperty.OldPropertyNo.Contains(searchRequest.OldPropertyNo));
            }

            // UPIC Id filter
            if (!string.IsNullOrWhiteSpace(searchRequest.UPICId))
            {
                query = query.Where(x => x.Property.UPICId != null && x.Property.UPICId.Contains(searchRequest.UPICId));
            }

            // CSN (City Survey No) filter
            if (!string.IsNullOrWhiteSpace(searchRequest.CSN))
            {
                query = query.Where(x => x.Property.CSN != null && x.Property.CSN.Contains(searchRequest.CSN));
            }

            // SubZone No filter
            if (!string.IsNullOrWhiteSpace(searchRequest.SubZoneNo))
            {
                query = query.Where(x => x.Property.SubZoneNo != null && x.Property.SubZoneNo.Contains(searchRequest.SubZoneNo));
            }

            // Plot No filter
            if (!string.IsNullOrWhiteSpace(searchRequest.PlotNo))
            {
                query = query.Where(x => x.Property.PlotNo != null && x.Property.PlotNo.Contains(searchRequest.PlotNo));
            }

            // Property Assessment Status filter
            if (searchRequest.PropertyAssessmentStatusId.HasValue)
            {
                query = query.Where(x => x.Property.PropertyAssessmentStatusId == searchRequest.PropertyAssessmentStatusId);
            }

            // Property Description filter
            if (searchRequest.PropertyDescriptionId.HasValue)
            {
                query = query.Where(x => x.Property.PropertyTypeId == searchRequest.PropertyDescriptionId);
            }

            // Mobile No filter
            if (!string.IsNullOrWhiteSpace(searchRequest.MobileNo))
            {
                query = query.Where(x => x.Property.MobileNo != null && x.Property.MobileNo.Contains(searchRequest.MobileNo));
            }

            // Owner Name filter
            if (!string.IsNullOrWhiteSpace(searchRequest.OwnerName))
            {
                query = query.Where(x =>
                    (x.Property.OwnerName != null && x.Property.OwnerName.Contains(searchRequest.OwnerName)) ||
                    (x.Property.OwnerNameEnglish != null && x.Property.OwnerNameEnglish.Contains(searchRequest.OwnerName)));
            }

            // Occupier Name filter
            if (!string.IsNullOrWhiteSpace(searchRequest.OccupierName))
            {
                query = query.Where(x =>
                    (x.Property.OccupierName != null && x.Property.OccupierName.Contains(searchRequest.OccupierName)) ||
                    (x.Property.OccupierNameEnglish != null && x.Property.OccupierNameEnglish.Contains(searchRequest.OccupierName)));
            }

            // Flat or Shop Name filter
            if (!string.IsNullOrWhiteSpace(searchRequest.FlatOrShopName))
            {
                query = query.Where(x =>
                    (x.Property.FlatOrShopName != null && x.Property.FlatOrShopName.Contains(searchRequest.FlatOrShopName)) ||
                    (x.Property.FlatOrShopNameEnglish != null && x.Property.FlatOrShopNameEnglish.Contains(searchRequest.FlatOrShopName)));
            }

            // Society Name filter
            if (!string.IsNullOrWhiteSpace(searchRequest.SocietyName))
            {
                query = query.Where(x =>
                    (x.Society != null && x.Society.SocietyName != null && x.Society.SocietyName.Contains(searchRequest.SocietyName)) ||
                    (x.Society != null && x.Society.SocietyNameEnglish != null && x.Society.SocietyNameEnglish.Contains(searchRequest.SocietyName)));
            }

            // Address filter
            if (!string.IsNullOrWhiteSpace(searchRequest.Address))
            {
                query = query.Where(x =>
                    (x.Property.Address != null && x.Property.Address.Contains(searchRequest.Address)) ||
                    (x.Property.AddressEnglish != null && x.Property.AddressEnglish.Contains(searchRequest.Address)));
            }

            // Values & Dues filters - filter by RV, CV, or Total Tax from PolicyConfiguration
        }

        var propertyResults = await query.OrderBy(x => x.Property.PartitionNo).ToListAsync(cancellationToken);

        if (!propertyResults.Any())
            return new ApartmentUnitListResponseDto
            {
                Items = new List<PropertySearchResponseDto>(),
                TotalCount = 0
            };

        var propertyIds = propertyResults.Select(x => x.Property.Id).ToList();

        var rvValues = await _context.TransMast
            .Where(t => propertyIds.Contains(t.PropertyId) && t.CalculationType == "RV" && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                RateableValue = g.OrderByDescending(x => x.Id).Select(x => (decimal?)x.CalculationValue).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var cvValues = await _context.TransMast
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion && t.CalculationType == "CV")
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                CapitalValue = g.OrderByDescending(x => x.Id).Select(x => (decimal?)x.CalculationValue).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var totalTaxAmounts = await (
            from t in _context.TransMast.AsNoTracking()
            join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
            where propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion
                  && tax.IsActive && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
            group t by t.PropertyId into g
            select new { PropertyId = g.Key, TotalTax = g.Sum(x => x.TaxAmount) }
        ).ToListAsync(cancellationToken);

        var rvDictionary = rvValues.ToDictionary(x => x.PropertyId, x => x.RateableValue);
        var cvDictionary = cvValues.ToDictionary(x => x.PropertyId, x => x.CapitalValue);
        var totalTaxDictionary = totalTaxAmounts.ToDictionary(x => x.PropertyId, x => x.TotalTax);

        // Apply Values & Dues Top-N filtering if requested
        if (!string.IsNullOrWhiteSpace(searchRequest?.ValuationMethod) &&
            !string.IsNullOrWhiteSpace(searchRequest?.FilterType) &&
            searchRequest.FilterType.Trim().Equals("Top", StringComparison.OrdinalIgnoreCase) &&
            searchRequest.TopCount.HasValue && searchRequest.TopCount.Value > 0)
        {
            var valuationMethod = searchRequest.ValuationMethod.Trim().ToUpper();
            var validMethods = await GetValidValuationMethodsAsync(cancellationToken);

            if (validMethods.Contains(valuationMethod) && (valuationMethod == "RV" || valuationMethod == "CV"))
            {
                var topPropertyIds = valuationMethod == "RV"
                    ? rvDictionary.OrderByDescending(x => x.Value).Take(searchRequest.TopCount.Value).Select(x => x.Key).ToList()
                    : cvDictionary.OrderByDescending(x => x.Value).Take(searchRequest.TopCount.Value).Select(x => x.Key).ToList();
                propertyResults = propertyResults.Where(x => topPropertyIds.Contains(x.Property.Id)).ToList();
            }
            else if (validMethods.Contains(valuationMethod) && valuationMethod == "TOTAL TAX")
            {
                var topPropertyIds = totalTaxDictionary.OrderByDescending(x => x.Value).Take(searchRequest.TopCount.Value).Select(x => x.Key).ToList();
                propertyResults = propertyResults.Where(x => topPropertyIds.Contains(x.Property.Id)).ToList();
            }
        }

        var items = propertyResults.Select(pr =>
        {
            rvDictionary.TryGetValue(pr.Property.Id, out var rv);
            cvDictionary.TryGetValue(pr.Property.Id, out var cv);
            totalTaxDictionary.TryGetValue(pr.Property.Id, out var totalTax);

            return new PropertySearchResponseDto
            {
                PropertyId = pr.Property.Id,
                UPICId = pr.Property.UPICId,
                ZoneName = pr.Zone?.Description,
                WardName = pr.Ward?.WardNo,
                PropertyNo = pr.Property.PropertyNo,
                PartitionNo = pr.Property.PartitionNo,
                OldPropertyNo = pr.OldProperty?.OldPropertyNo,
                CitySurveyNo = pr.Property.CSN,
                PlotNo = pr.Property.PlotNo,
                WingFlatNo = pr.Property.FlatOrShopNo,
                CategoryName = pr.Category?.PropertyCategoryName,
                PropertyDescription = pr.PropertyType?.PropertyDescription,
                Mobile = pr.Property.MobileNo,
                PropertyHolderName = pr.Property.OwnerName ?? pr.Property.OwnerNameEnglish,
                OccupierName = pr.Property.OccupierName ?? pr.Property.OccupierNameEnglish,
                ShopBuildingName = pr.Property.FlatOrShopName ?? pr.Property.FlatOrShopNameEnglish,
                SocietyName = pr.Society?.SocietyName ?? pr.Society?.SocietyNameEnglish,
                Address = pr.Property.Address ?? pr.Property.AddressEnglish,
                RV = rv,
                CV = cv,
                TotalTax = totalTax
            };
        }).ToList();

        return new ApartmentUnitListResponseDto
        {
            Items = items,
            TotalCount = items.Count  // All properties displayed as units
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Public: search by category (Zone/Ward/Building/Property-range scoped search)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Row shape shared by <see cref="SearchByCategoryAsync"/> and <see cref="GetPropertyIdsByCategoryAsync"/> -
    /// a named type (rather than an anonymous type) so the filtered query built by
    /// <see cref="BuildCategoryFilteredQueryAsync"/> can be returned across method boundaries.
    /// </summary>
    private sealed class CategoryJoinRow
    {
        public PropertyEntity Property { get; init; } = null!;
        public WardEntity? Ward { get; init; }
        public ZoneEntity? Zone { get; init; }
        public PropertyTypeMasterEntity? PropertyType { get; init; }
        public PropertyCategoryEntity? Category { get; init; }
    }

    /// <summary>
    /// Builds the joined, category-scoped, filtered (but unsorted/unpaged) query shared by
    /// <see cref="SearchByCategoryAsync"/> (full response DTO, sorted, paged) and
    /// <see cref="GetPropertyIdsByCategoryAsync"/> (bare PropertyIds, for bulk actions) - so the
    /// SearchCategory switch and the PartType/PropertyCategoryName/PropertyAssessmentStatusId/
    /// IsWing/SearchTerm filters are defined exactly once.
    /// </summary>
    private async Task<(IQueryable<CategoryJoinRow> Query, HashSet<string> WingNumbers)> BuildCategoryFilteredQueryAsync(
        PropertySearchByCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = from p in _context.PropertyMast.AsNoTracking()
                    where p.IsActive && !p.MarkedForDeletion

                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join z in _context.ZoneMaster.AsNoTracking() on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into propertyTypeJoin
                    from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
                    from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    select new CategoryJoinRow
                    {
                        Property = p,
                        Ward = w,
                        Zone = z,
                        PropertyType = pt,
                        Category = pc
                    };

        // Category-specific scoping is applied server-side. FromToProperty's exact alpha/numeric
        // partition-boundary comparison can't be translated to SQL (see the private helpers below)
        // and is applied in-memory - but the PropertyNo bound itself CAN be pushed to SQL first
        // (via the same length-then-string trick used for the natural sort below), so only the
        // properties actually within [FromPropertyNo, ToPropertyNo] are ever materialized, instead
        // of the whole ward. This coarse bound is inclusive/a superset of the true match set (it
        // doesn't yet account for the partition tie-break at the boundary PropertyNo), which the
        // in-memory filter below narrows down to the exact result.
        switch (request.SearchCategory)
        {
            case PropertySearchCategory.ZoneWise:
                query = query.Where(x => x.Zone != null && x.Zone.Id == request.ZoneId);
                break;

            case PropertySearchCategory.WardWise:
                query = query.Where(x => x.Property.WardId == request.WardId);
                break;

            case PropertySearchCategory.BuildingWise:
                query = query.Where(x => x.Property.WardId == request.WardId && x.Property.PropertyNo == request.PropertyNo);
                if (!string.IsNullOrWhiteSpace(request.PartitionNo))
                    query = query.Where(x => x.Property.PartitionNo == request.PartitionNo);
                break;

            case PropertySearchCategory.FromToProperty:
                query = query.Where(x => x.Property.WardId == request.WardId);

                var (coarseFromPropertyNo, _) = ParsePropertyToken(request.PropertyFrom);
                if (coarseFromPropertyNo.HasValue)
                {
                    var fromPropertyNoStr = coarseFromPropertyNo.Value.ToString();
                    query = query.Where(x => x.Property.PropertyNo != null &&
                        (x.Property.PropertyNo.Length > fromPropertyNoStr.Length ||
                         (x.Property.PropertyNo.Length == fromPropertyNoStr.Length && string.Compare(x.Property.PropertyNo, fromPropertyNoStr) >= 0)));
                }

                if (!string.IsNullOrWhiteSpace(request.PropertyTo))
                {
                    var (coarseToPropertyNo, _) = ParsePropertyToken(request.PropertyTo);
                    if (coarseToPropertyNo.HasValue)
                    {
                        var toPropertyNoStr = coarseToPropertyNo.Value.ToString();
                        query = query.Where(x => x.Property.PropertyNo != null &&
                            (x.Property.PropertyNo.Length < toPropertyNoStr.Length ||
                             (x.Property.PropertyNo.Length == toPropertyNoStr.Length && string.Compare(x.Property.PropertyNo, toPropertyNoStr) <= 0)));
                    }
                }
                break;
        }

        // IsWing mirrors the source SQL's unfiltered EXISTS(SELECT 1 FROM PTIS.WingMaster ...) -
        // no IsActive filter is applied here to stay faithful to that behavior. Loaded upfront so
        // it can back both the IsWing filter below and the IsWing response column.
        var wingNumbers = new HashSet<string>(await _context.WingEntity.Select(w => w.WingNo).ToListAsync(cancellationToken));

        // Additional optional filters, independent of SearchCategory. Each accepts a
        // comma-separated list of values (parsed via FilterExpressionBuilder.Csv) and matches
        // any of them (SQL IN).
        var partTypes = FilterExpressionBuilder.Csv(request.PartType);
        if (partTypes.Count > 0)
            query = query.Where(x => x.PropertyType != null && x.PropertyType.PartType != null && partTypes.Contains(x.PropertyType.PartType));

        var categoryNames = FilterExpressionBuilder.Csv(request.PropertyCategoryName);
        if (categoryNames.Count > 0)
            query = query.Where(x => x.Category != null && x.Category.PropertyCategoryName != null && categoryNames.Contains(x.Category.PropertyCategoryName));

        var statusIds = FilterExpressionBuilder.Csv(request.PropertyAssessmentStatusId)
            .Select(s => int.TryParse(s, out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
        if (statusIds.Count > 0)
            query = query.Where(x => x.Property.PropertyAssessmentStatusId.HasValue && statusIds.Contains(x.Property.PropertyAssessmentStatusId.Value));

        if (request.IsWing.HasValue)
        {
            query = request.IsWing.Value
                ? query.Where(x => x.Property.PartitionNo != null && wingNumbers.Contains(x.Property.PartitionNo))
                : query.Where(x => x.Property.PartitionNo == null || !wingNumbers.Contains(x.Property.PartitionNo));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(x =>
                (x.Ward != null && x.Ward.WardNo != null && x.Ward.WardNo.Contains(searchTerm)) ||
                (x.Property.PropertyNo != null && x.Property.PropertyNo.Contains(searchTerm)) ||
                (x.Property.PartitionNo != null && x.Property.PartitionNo.Contains(searchTerm)));
        }

        return (query, wingNumbers);
    }

    public async Task<(int TotalCount, List<PropertySearchByCategoryResponseDto> Items)> SearchByCategoryAsync(
        PropertySearchByCategoryRequestDto request,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (query, wingNumbers) = await BuildCategoryFilteredQueryAsync(request, cancellationToken);

        var isUnpaged = pageSize == -1;

        if (request.SearchCategory == PropertySearchCategory.FromToProperty)
        {
            // Ward-bounded candidate set only (never a whole zone/table) - filtered, sorted and
            // paged in memory because the partition-range comparison isn't SQL-translatable.
            var rows = await query.ToListAsync(cancellationToken);

            var (fromPropertyNo, fromPartition, toPropertyNo, toPartition, hasPropertyTo) =
                ParseFromToPropertyBounds(request.PropertyFrom, request.PropertyTo);

            rows = rows.Where(x =>
            {
                if (!int.TryParse(x.Property.PropertyNo, out var propertyNoInt))
                    return false;

                var (alpha, numeric) = SplitPartition(x.Property.PartitionNo);

                var lowerBoundMet = propertyNoInt > fromPropertyNo ||
                    (propertyNoInt == fromPropertyNo && MeetsLowerPartitionBound(alpha, numeric, fromPartition));

                var upperBoundMet = !hasPropertyTo ||
                    propertyNoInt < toPropertyNo ||
                    (propertyNoInt == toPropertyNo && MeetsUpperPartitionBound(alpha, numeric, toPartition));

                return lowerBoundMet && upperBoundMet;
            }).ToList();

            var mapped = rows
                .Select(x => MapToResponseDto(x.Property, x.Ward, x.Zone, x.PropertyType, x.Category, wingNumbers))
                .ToList();

            var ordered = mapped
                .OrderBy(x => TryParseInt(x.PropertyNo))
                .ThenBy(x => x.PropertyNo, StringComparer.Ordinal)
                .ThenBy(x => string.IsNullOrEmpty(x.PartitionNo) ? 0 : 1)
                .ThenBy(x => SplitPartition(x.PartitionNo).AlphaPart, StringComparer.Ordinal)
                .ThenBy(x => SplitPartition(x.PartitionNo).NumericPart)
                .ToList();

            var totalCount = ordered.Count;
            var skip = isUnpaged ? 0 : (pageNumber - 1) * pageSize;
            var items = isUnpaged ? ordered : ordered.Skip(skip).Take(pageSize).ToList();

            return (totalCount, items);
        }
        else
        {
            // ZoneWise/WardWise/BuildingWise scopes can span many properties (a zone can hold
            // thousands), so sorting and paging happen in SQL Server via OrderByNatural/ThenByNatural
            // (PATINDEX-backed, see AlphanumericSortExtensions) - only the requested page is ever
            // materialized, instead of loading and sorting the entire matching set in memory.
            var totalCount = await query.CountAsync(cancellationToken);

            var orderedQuery = query
                .OrderBy(x => x.Property.PropertyNo == null ? 0 : x.Property.PropertyNo.Length)
                .ThenBy(x => x.Property.PropertyNo)
                .ThenBy(x => string.IsNullOrEmpty(x.Property.PartitionNo) ? 0 : 1)
                .ThenByNatural(x => x.Property.PartitionNo);

            var pageQuery = isUnpaged ? orderedQuery : orderedQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var pageRows = await pageQuery.ToListAsync(cancellationToken);

            var items = pageRows
                .Select(x => MapToResponseDto(x.Property, x.Ward, x.Zone, x.PropertyType, x.Category, wingNumbers))
                .ToList();

            return (totalCount, items);
        }
    }

    /// <summary>
    /// Resolves the bare PropertyIds matching a SearchCategory scope - for bulk actions (e.g.
    /// bulk lock/unlock) that need "every property matching this scope" without the response-DTO
    /// mapping, wing lookup, or natural-sort ordering that <see cref="SearchByCategoryAsync"/> pays
    /// for on every row. Reuses the exact same category-switch and optional filters via
    /// <see cref="BuildCategoryFilteredQueryAsync"/>, so results are always consistent between the
    /// two methods.
    /// </summary>
    public async Task<List<int>> GetPropertyIdsByCategoryAsync(
        PropertySearchByCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (query, _) = await BuildCategoryFilteredQueryAsync(request, cancellationToken);

        if (request.SearchCategory == PropertySearchCategory.FromToProperty)
        {
            // Ward-and-PropertyNo-bounded candidate set only (see BuildCategoryFilteredQueryAsync) -
            // the exact partition-boundary comparison still isn't SQL-translatable, so it's applied
            // in-memory here, same as SearchByCategoryAsync.
            var rows = await query
                .Select(x => new { x.Property.Id, x.Property.PropertyNo, x.Property.PartitionNo })
                .ToListAsync(cancellationToken);

            var (fromPropertyNo, fromPartition, toPropertyNo, toPartition, hasPropertyTo) =
                ParseFromToPropertyBounds(request.PropertyFrom, request.PropertyTo);

            return rows.Where(x =>
            {
                if (!int.TryParse(x.PropertyNo, out var propertyNoInt))
                    return false;

                var (alpha, numeric) = SplitPartition(x.PartitionNo);

                var lowerBoundMet = propertyNoInt > fromPropertyNo ||
                    (propertyNoInt == fromPropertyNo && MeetsLowerPartitionBound(alpha, numeric, fromPartition));

                var upperBoundMet = !hasPropertyTo ||
                    propertyNoInt < toPropertyNo ||
                    (propertyNoInt == toPropertyNo && MeetsUpperPartitionBound(alpha, numeric, toPartition));

                return lowerBoundMet && upperBoundMet;
            }).Select(x => x.Id).ToList();
        }

        return await query.Select(x => x.Property.Id).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Splits a "PropertyNo[-PartitionNo]" token (e.g. "1-A9") on the first '-', mirroring the
    /// source SQL's <c>CHARINDEX('-', @token+'-')</c> trick.
    /// </summary>
    private static (int? PropertyNo, string? Partition) ParsePropertyToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return (null, null);

        var dashIndex = token.IndexOf('-');
        string propertyPart;
        string? partitionPart;

        if (dashIndex < 0)
        {
            propertyPart = token;
            partitionPart = null;
        }
        else
        {
            propertyPart = token.Substring(0, dashIndex);
            partitionPart = dashIndex + 1 < token.Length ? token.Substring(dashIndex + 1) : null;
        }

        var propertyNo = int.TryParse(propertyPart, out var parsed) ? (int?)parsed : null;
        return (propertyNo, partitionPart);
    }

    /// <summary>
    /// Parses the FromToProperty range bounds and coerces them to non-null ints before any
    /// caller compares them against a row's PropertyNo - PropertyFrom/PropertyTo are validated
    /// upstream (<see cref="Application.Services.Property.PropertySearchService"/>) to always
    /// resolve to a numeric PropertyNo, but comparing a bare int against an int? lifts to a
    /// nullable comparison that silently evaluates to false instead of surfacing a bug, so that
    /// invariant is enforced here instead of relied upon implicitly. ToPropertyNo defaults to 0
    /// when PropertyTo is absent; callers must guard its use with HasPropertyTo.
    /// </summary>
    private static (int FromPropertyNo, string? FromPartition, int ToPropertyNo, string? ToPartition, bool HasPropertyTo)
        ParseFromToPropertyBounds(string? propertyFrom, string? propertyTo)
    {
        var (fromPropertyNo, fromPartition) = ParsePropertyToken(propertyFrom);
        var (toPropertyNo, toPartition) = ParsePropertyToken(propertyTo);
        var hasPropertyTo = !string.IsNullOrWhiteSpace(propertyTo);

        if (fromPropertyNo is null)
            throw new InvalidOperationException($"PropertyFrom '{propertyFrom}' must resolve to a numeric property number.");
        if (hasPropertyTo && toPropertyNo is null)
            throw new InvalidOperationException($"PropertyTo '{propertyTo}' must resolve to a numeric property number.");

        return (fromPropertyNo.Value, fromPartition, toPropertyNo ?? 0, toPartition, hasPropertyTo);
    }

    /// <summary>
    /// Splits a PartitionNo into its leading alpha prefix and trailing numeric suffix
    /// (e.g. "A9" -> ("A", 9)), mirroring the source SQL's
    /// <c>PATINDEX('%[0-9]%', PartitionNo+'0')</c> first-digit-index trick.
    /// </summary>
    private static (string AlphaPart, int NumericPart) SplitPartition(string? partitionNo)
    {
        var value = partitionNo ?? string.Empty;
        var digitIndex = -1;

        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsDigit(value[i]))
            {
                digitIndex = i;
                break;
            }
        }

        if (digitIndex < 0)
            return (value, 0);

        var alpha = value.Substring(0, digitIndex);
        var numeric = int.TryParse(value.Substring(digitIndex), out var n) ? n : 0;
        return (alpha, numeric);
    }

    /// <summary>
    /// Row's (alpha, numeric) partition is at or above the From boundary: alpha prefix sorts
    /// after the From partition's alpha prefix, or matches it with an equal-or-higher numeric part.
    /// </summary>
    private static bool MeetsLowerPartitionBound(string alpha, int numeric, string? fromPartition)
    {
        var (fromAlpha, fromNumeric) = SplitPartition(fromPartition);
        var alphaCompare = string.CompareOrdinal(alpha, fromAlpha);
        return alphaCompare > 0 || (alphaCompare == 0 && numeric >= fromNumeric);
    }

    /// <summary>
    /// Row's (alpha, numeric) partition is at or below the To boundary - the mirror of
    /// <see cref="MeetsLowerPartitionBound"/>.
    /// </summary>
    private static bool MeetsUpperPartitionBound(string alpha, int numeric, string? toPartition)
    {
        var (toAlpha, toNumeric) = SplitPartition(toPartition);
        var alphaCompare = string.CompareOrdinal(alpha, toAlpha);
        return alphaCompare < 0 || (alphaCompare == 0 && numeric <= toNumeric);
    }

    private static int? TryParseInt(string? value) => int.TryParse(value, out var n) ? n : null;

    private static PropertySearchByCategoryResponseDto MapToResponseDto(
        PropertyEntity property,
        WardEntity? ward,
        ZoneEntity? zone,
        PropertyTypeMasterEntity? propertyType,
        PropertyCategoryEntity? category,
        HashSet<string> wingNumbers)
    {
        return new PropertySearchByCategoryResponseDto
        {
            PropertyId = property.Id,
            TaxZoneId = property.TaxZoneId,
            ZoneId = zone?.Id,
            ZoneNo = zone?.ZoneNo,
            WardId = property.WardId,
            WardNo = ward?.WardNo,
            PropertyNo = property.PropertyNo,
            PartitionNo = property.PartitionNo ?? string.Empty,
            MobileNo = property.MobileNo,
            UPICId = property.UPICId,
            PropertyTypeId = property.PropertyTypeId,
            PartType = propertyType?.PartType,
            CategoryId = property.CategoryId,
            PropertyCategoryName = category?.PropertyCategoryName,
            IsWing = !string.IsNullOrEmpty(property.PartitionNo) && wingNumbers.Contains(property.PartitionNo),
            PropertyAssessmentStatusId = property.PropertyAssessmentStatusId ?? 0
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Public: legacy dashboard stats
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var allProperties = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        var registeredCount = await allProperties.CountAsync(cancellationToken);
        var geoSequencingCount = await allProperties.Where(p => !string.IsNullOrEmpty(p.PropertyNo)).CountAsync(cancellationToken);

        return new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = registeredCount,
            GeoSequencingPropertyCount = geoSequencingCount,
            SurveyPropertyCount = 0,
            DataProcessingPropertyCount = 0,
            QualityAnalysisPropertyCount = 0,
            AssessmentCompletedPropertyCount = 0
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Private: filter helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies common dashboard filter parameters to a PropertyMast query.
    /// Centralised so every card and workflow-stage query uses the same predicates.
    /// </summary>
    private static IQueryable<PropertyEntity> ApplyDashboardFilters(
        IQueryable<PropertyEntity> query,
        PropertySearchRequestDto? request)
    {
        if (request is null) return query;

        if (request.PropertyAssessmentStatusId.HasValue)
            query = query.Where(p => p.PropertyAssessmentStatusId == request.PropertyAssessmentStatusId.Value);

        if (request.PropertyDescriptionId.HasValue)
            query = query.Where(p => p.PropertyTypeId == request.PropertyDescriptionId.Value);

        if (request.PropertyTypeId.HasValue)
            query = query.Where(p => p.PropertyTypeId == request.PropertyTypeId.Value);

        if (request.ZoneId.HasValue)
            query = query.Where(p => p.TaxZoneId == request.ZoneId.Value);

        if (request.WardId.HasValue)
            query = query.Where(p => p.WardId == request.WardId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        return query;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Private: count helpers (DB-side, no in-memory materialisation)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Counts total, structure, and unit properties from a query entirely in the DB.
    /// Structure: category != 'Apartment' OR (Apartment AND PartitionNo is null/empty).
    /// Unit:      category == 'Apartment' AND PartitionNo is not null/empty.
    /// </summary>
    private async Task<(int PropertyCount, int StructureCount, int UnitCount)> CountPropertiesAsync(
        IQueryable<PropertyEntity> query,
        CancellationToken cancellationToken)
    {
        var propertyCount = await query.CountAsync(cancellationToken);

        // Count Units = Apartment with non-empty PartitionNo only
        // IMPORTANT: Trim PartitionNo to handle whitespace
        var unitsOnlyCount = await (
            from p in query
            join pc in _context.PropertyCategoryMaster on p.CategoryId equals pc.Id into categoryJoin
            from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
            where pc != null
                  && pc.PropertyCategoryName == ApartmentCategoryName
                  && p.PartitionNo != null
                  && p.PartitionNo.Trim() != ""
            select p.Id
        ).CountAsync(cancellationToken);

        // Structure = All properties EXCEPT Units (Apartment + empty/null PartitionNo + Individual/Industry/Plot)
        var structureCount = propertyCount - unitsOnlyCount;

        // Unit = All properties (Structures + Units both included)
        // Since all properties are units, UnitCount = PropertyCount
        var unitCount = propertyCount;

        return (propertyCount, structureCount, unitCount);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Private: demand helpers (DB-side)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sum of TaxTotal demand from PTIS.TransMast + PTIS.TaxMaster for the given property-id set.
    /// Only rows where TaxMaster.TaxCode='TaxTotal' AND TaxMaster.TaxName='TaxTotal' are included.
    /// </summary>
    private async Task<decimal> GetNewTaxTotalDemandAsync(
        IQueryable<int> propertyIdQuery,
        CancellationToken cancellationToken)
    {
        var sum = await (
            from pid in propertyIdQuery
            join t in _context.TransMast on pid equals t.PropertyId
            join tax in _context.TaxMaster on t.TaxId equals tax.Id
            where t.IsActive && !t.MarkedForDeletion
                  && tax.IsActive && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
            select (decimal?)t.TaxAmount
        ).SumAsync(cancellationToken);

        return sum ?? 0m;
    }

    /// <summary>
    /// Sum of TaxTotal demand from PTIS.TransMastOld + PTIS.TaxMaster for properties that
    /// have a linked PropertyMastOld record (via PropertyMast.PropertyMastOldId).
    /// Only rows where TaxMaster.TaxCode='TaxTotal' AND TaxMaster.TaxName='TaxTotal' are included.
    /// </summary>
    private async Task<decimal> GetOldTaxTotalDemandAsync(
        IQueryable<PropertyEntity> query,
        CancellationToken cancellationToken)
    {
        var sum = await (
            from p in query
            where p.PropertyMastOldId != null
            join tmo in _context.TransMastOld on p.PropertyMastOldId equals tmo.PropertyMastOldId
            join tax in _context.TaxMaster on tmo.TaxId equals tax.Id
            where tmo.IsActive && !tmo.MarkedForDeletion
                  && tax.IsActive && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
            select (decimal?)tmo.TaxAmount
        ).SumAsync(cancellationToken);

        return sum ?? 0m;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Private: card calculation methods
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Previously Registered = current active properties that have a linked old record.
    /// Demand comes from PTIS.TransMastOld (TaxTotal) via PropertyMastOld.
    /// </summary>
    private async Task<DashboardCardBreakdownDto> CalculatePreviouslyRegisteredAsync(
        IQueryable<PropertyEntity> query,
        CancellationToken cancellationToken)
    {
        var prevQuery = query.Where(p => p.PropertyMastOldId != null);

        var (propertyCount, structureCount, unitCount) = await CountPropertiesAsync(prevQuery, cancellationToken);
        var demand = await GetOldTaxTotalDemandAsync(prevQuery, cancellationToken);

        return new DashboardCardBreakdownDto
        {
            PropertyCount = propertyCount,
            StructureCount = structureCount,
            UnitCount = unitCount,
            Demand = demand
        };
    }

    /// <summary>
    /// Calculates counts and demand for properties whose assessment status name matches
    /// the given <paramref name="statusName"/>.
    /// When <paramref name="includeDemand"/> is true, sums TaxTotal from PTIS.TransMast.
    /// </summary>
    private async Task<DashboardCardBreakdownDto> CalculateByAssessmentStatusAsync(
        IQueryable<PropertyEntity> query,
        string statusName,
        bool includeDemand,
        CancellationToken cancellationToken)
    {
        var statusIds = await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive && s.StatusName.ToUpper() == statusName.ToUpper())
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var filteredQuery = statusIds.Count > 0
            ? query.Where(p => p.PropertyAssessmentStatusId.HasValue &&
                               statusIds.Contains(p.PropertyAssessmentStatusId.Value))
            : query.Where(_ => false);

        var (propertyCount, structureCount, unitCount) = await CountPropertiesAsync(filteredQuery, cancellationToken);

        var demand = 0m;
        if (includeDemand && propertyCount > 0)
            demand = await GetNewTaxTotalDemandAsync(filteredQuery.Select(p => p.Id), cancellationToken);

        return new DashboardCardBreakdownDto
        {
            PropertyCount = propertyCount,
            StructureCount = structureCount,
            UnitCount = unitCount,
            Demand = demand
        };
    }

    /// <summary>
    /// Additional Revenue = NewTaxTotal (TransMast) minus OldTaxTotal (TransMastOld).
    /// Only cases where new demand exceeds old demand are counted and summed.
    /// </summary>
    private async Task<DashboardCardBreakdownDto> CalculateAdditionalRevenueAsync(
        IQueryable<PropertyEntity> query,
        CancellationToken cancellationToken)
    {
        // Properties that have both old and new demand records
        var revenueQuery = query.Where(p => p.PropertyMastOldId != null);

        var (propertyCount, structureCount, unitCount) = await CountPropertiesAsync(revenueQuery, cancellationToken);

        var newDemand = await GetNewTaxTotalDemandAsync(revenueQuery.Select(p => p.Id), cancellationToken);
        var oldDemand = await GetOldTaxTotalDemandAsync(revenueQuery, cancellationToken);

        var additionalRevenue = newDemand - oldDemand;

        return new DashboardCardBreakdownDto
        {
            PropertyCount = propertyCount,
            StructureCount = structureCount,
            UnitCount = unitCount,
            Demand = additionalRevenue > 0 ? additionalRevenue : 0m
        };
    }
}