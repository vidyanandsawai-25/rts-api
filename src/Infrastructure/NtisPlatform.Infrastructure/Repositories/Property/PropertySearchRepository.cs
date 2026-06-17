using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Enums;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>
/// Data-access implementation for the Property Search screen: the multi-criteria property
/// search (Quick Search / KYC Search / Values &amp; Dues) and the dashboard count statistics.
/// Pure querying only - no SaveChanges and no business messages.
/// </summary>
public class PropertySearchRepository : IPropertySearchRepository
{
    private readonly ApplicationDbContext _context;

    public PropertySearchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(int TotalCount, List<PropertySearchResponseDto> Items)> SearchPropertiesAsync(PropertySearchRequestDto searchRequest, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // Handle dashboard card filters
        if (searchRequest.DashboardFilter.HasValue)
        {
            switch (searchRequest.DashboardFilter.Value)
            {
                case DashboardFilterType.RegisteredProperty:
                    // Show all registered properties - no additional filter needed
                    break;

                case DashboardFilterType.GeoSequencing:
                    // Show properties where PropertyNo is present - will be applied below
                    break;

                case DashboardFilterType.Survey:
                case DashboardFilterType.DataProcessing:
                case DashboardFilterType.QualityAnalysis:
                case DashboardFilterType.AssessmentCompleted:
                    // These are work in progress - return empty result
                    return (0, new List<PropertySearchResponseDto>());
            }
        }

        // Handle property process filter (Type dropdown)
        if (searchRequest.PropertyProcessFilter.HasValue)
        {
            switch (searchRequest.PropertyProcessFilter.Value)
            {
                case PropertyProcessFilterType.SurveyCompleted:
                case PropertyProcessFilterType.DataEntryCompleted:
                case PropertyProcessFilterType.QCCompleted:
                case PropertyProcessFilterType.NoticeDistributed:
                    // All these are work in progress - return empty result
                    return (0, new List<PropertySearchResponseDto>());
            }
        }

        // Build the base query with all joins
        var query = from p in _context.PropertyMast.AsNoTracking()
                    where p.IsActive && !p.MarkedForDeletion

                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join z in _context.ZoneMaster.AsNoTracking() on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
                    from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

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
                        OldProperty = pmo,
                        Society = sd
                    };

        // Apply dashboard filter for geo-sequencing
        if (searchRequest.DashboardFilter == DashboardFilterType.GeoSequencing)
        {
            query = query.Where(x => !string.IsNullOrEmpty(x.Property.PropertyNo));
        }

        // Apply Quick Search filters
        if (searchRequest.PropertyTypeId.HasValue)
        {
            query = query.Where(x => x.Property.PropertyTypeId == searchRequest.PropertyTypeId.Value);
        }

        if (searchRequest.CategoryId.HasValue)
        {
            query = query.Where(x => x.Property.CategoryId == searchRequest.CategoryId.Value);
        }

        if (searchRequest.TypeOfUseId.HasValue)
        {
            // TypeOfUse is in PropertyDetails, need to check if any PropertyDetails has this TypeOfUseId
var propertyIdsWithTypeOfUse = _context.PropertyDetails
                .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.TypeOfUseId == searchRequest.TypeOfUseId.Value)
                .Select(pd => pd.PropertyId)
                .Distinct();

            query = query.Where(x => propertyIdsWithTypeOfUse.Contains(x.Property.Id));
        }

        if (searchRequest.ZoneId.HasValue)
        {
            query = query.Where(x => x.Zone != null && x.Zone.Id == searchRequest.ZoneId.Value);
        }

        if (searchRequest.WardId.HasValue)
        {
            query = query.Where(x => x.Property.WardId == searchRequest.WardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom) && !string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
        {
            query = query.Where(x => x.Property.PropertyNo != null &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoFrom) >= 0 &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoTo) <= 0);
        }
        else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom))
        {
            query = query.Where(x => x.Property.PropertyNo != null &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoFrom) >= 0);
        }
        else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
        {
            query = query.Where(x => x.Property.PropertyNo != null &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoTo) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OldPropertyNo))
        {
            query = query.Where(x => x.OldProperty != null &&
                                   x.OldProperty.OldPropertyNo != null &&
                                   x.OldProperty.OldPropertyNo.Contains(searchRequest.OldPropertyNo));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.UPICId))
        {
            query = query.Where(x => x.Property.UPICId != null &&
                                   x.Property.UPICId.Contains(searchRequest.UPICId));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.CSN))
        {
            query = query.Where(x => x.Property.CSN != null &&
                                   x.Property.CSN.Contains(searchRequest.CSN));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.SubZoneNo))
        {
            query = query.Where(x => x.Property.SubZoneNo != null &&
                                   x.Property.SubZoneNo.Contains(searchRequest.SubZoneNo));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.PlotNo))
        {
            query = query.Where(x => x.Property.PlotNo != null &&
                                   x.Property.PlotNo.Contains(searchRequest.PlotNo));
        }

        if (searchRequest.PropertyAssessmentStatusId.HasValue)
        {
            query = query.Where(x => x.Property.PropertyAssessmentStatusId == searchRequest.PropertyAssessmentStatusId.Value);
        }

        // Apply KYC Search filters
        if (!string.IsNullOrWhiteSpace(searchRequest.MobileNo))
        {
            query = query.Where(x => (x.Property.MobileNo != null && x.Property.MobileNo.Contains(searchRequest.MobileNo)) ||
                                   (x.Property.AlternateMobileNo != null && x.Property.AlternateMobileNo.Contains(searchRequest.MobileNo)) ||
                                   (x.Property.OccupierMobileNo != null && x.Property.OccupierMobileNo.Contains(searchRequest.MobileNo)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OwnerName))
        {
            query = query.Where(x => (x.Property.OwnerName != null && x.Property.OwnerName.Contains(searchRequest.OwnerName)) ||
                                   (x.Property.OwnerNameEnglish != null && x.Property.OwnerNameEnglish.Contains(searchRequest.OwnerName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OccupierName))
        {
            query = query.Where(x => (x.Property.OccupierName != null && x.Property.OccupierName.Contains(searchRequest.OccupierName)) ||
                                   (x.Property.OccupierNameEnglish != null && x.Property.OccupierNameEnglish.Contains(searchRequest.OccupierName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.FlatOrShopName))
        {
            query = query.Where(x => (x.Property.FlatOrShopName != null && x.Property.FlatOrShopName.Contains(searchRequest.FlatOrShopName)) ||
                                   (x.Property.FlatOrShopNo != null && x.Property.FlatOrShopNo.Contains(searchRequest.FlatOrShopName)) ||
                                   (x.Property.FlatOrShopNameEnglish != null && x.Property.FlatOrShopNameEnglish.Contains(searchRequest.FlatOrShopName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.SocietyName))
        {
            query = query.Where(x => (x.Society != null && x.Society.SocietyName != null && x.Society.SocietyName.Contains(searchRequest.SocietyName)) ||
                                   (x.Society != null && x.Society.SocietyNameEnglish != null && x.Society.SocietyNameEnglish.Contains(searchRequest.SocietyName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.Address))
        {
            query = query.Where(x => (x.Property.Address != null && x.Property.Address.Contains(searchRequest.Address)) ||
                                   (x.Property.AddressEnglish != null && x.Property.AddressEnglish.Contains(searchRequest.Address)));
        }

        // Apply Values & Dues Search Filters
        if (searchRequest.RVorCV != null && searchRequest.RVorCV.Trim().Length > 0)
        {
            var rvOrCv = searchRequest.RVorCV.Trim().ToUpper();

            query = query.Where(x => _context.TransMast.Any(t => t.PropertyId == x.Property.Id && t.IsActive && !t.MarkedForDeletion &&
                    t.RVorCV == rvOrCv));
        }

        // Track if we're using Top operator for ordering later
        var isTopOperator = false;
        Dictionary<int, decimal>? topTaxLookup = null;

        if (!string.IsNullOrWhiteSpace(searchRequest.AmountFilterOperator))
        {
            var opTrimmed = searchRequest.AmountFilterOperator.Trim();

            if (!Enum.TryParse<FilterOperator>(opTrimmed, ignoreCase: true, out var op) ||
                !Enum.IsDefined(typeof(FilterOperator), op))
            {
                return (0, new List<PropertySearchResponseDto>());
            }

            // Validate that only supported operators for tax filtering are used
            if (op != FilterOperator.Equals &&
                op != FilterOperator.GreaterThan &&
                op != FilterOperator.LessThan &&
                op != FilterOperator.Between &&
                op != FilterOperator.Top)
            {
                // Unsupported operator - return empty result instead of silently ignoring
                return (0, new List<PropertySearchResponseDto>());
            }

            var applyAmountFilter = true;

            var taxQuery = _context.TransMast
                .Where(t => t.IsActive && !t.MarkedForDeletion)
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    TotalTax = g.Sum(x => x.TaxAmount)
                });

            if (op == FilterOperator.Top)
            {
                // For Top operator, get properties with highest total tax
                applyAmountFilter = false;
                isTopOperator = true;

                // Validate TopCount
                if (!searchRequest.TopCount.HasValue || searchRequest.TopCount.Value <= 0)
                {
                    // Invalid TopCount - return empty result
                    return (0, new List<PropertySearchResponseDto>());
                }

                var topCount = searchRequest.TopCount.Value;
                var topTaxData = await taxQuery
                    .OrderByDescending(t => t.TotalTax)
                    .Take(topCount)
                    .ToListAsync(cancellationToken);

                var topPropertyIds = topTaxData.Select(t => t.PropertyId).ToList();

                // Store tax amounts for ordering later
                topTaxLookup = topTaxData.ToDictionary(t => t.PropertyId, t => t.TotalTax);

                query = query.Where(x => topPropertyIds.Contains(x.Property.Id));
            }
            else if (searchRequest.AmountValue.HasValue)
            {
                var amount = searchRequest.AmountValue.Value;

                if (op == FilterOperator.Equals)
                {
                    taxQuery = taxQuery.Where(t => t.TotalTax == amount);
                }
                else if (op == FilterOperator.GreaterThan)
                {
                    taxQuery = taxQuery.Where(t => t.TotalTax > amount);
                }
                else if (op == FilterOperator.LessThan)
                {
                    taxQuery = taxQuery.Where(t => t.TotalTax < amount);
                }
                else if (op == FilterOperator.Between && searchRequest.AmountTo.HasValue)
                {
                    var toAmount = searchRequest.AmountTo.Value;

                    taxQuery = taxQuery.Where(t =>
                        t.TotalTax >= amount &&
                        t.TotalTax <= toAmount);
                }
                else
                {
                    applyAmountFilter = false;
                }

                if (applyAmountFilter)
                {
                    query = query.Where(x =>
                        taxQuery.Any(t => t.PropertyId == x.Property.Id));
                }
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering before pagination
        // For Top operator, we must apply the TotalTax ordering before paging to keep page slices correct.
        var orderedQuery = query.OrderBy(x => x.Property.Id);

        // Apply pagination
        var isUnpaged = pageSize == -1;
        var skip = isUnpaged ? 0 : (pageNumber - 1) * pageSize;

        var propertyResults = (isTopOperator && topTaxLookup != null)
            ? await orderedQuery.ToListAsync(cancellationToken)
            : await (isUnpaged ? orderedQuery : orderedQuery.Skip(skip).Take(pageSize)).ToListAsync(cancellationToken);

        if (!propertyResults.Any())
        {
            return (totalCount, new List<PropertySearchResponseDto>());
        }

        if (isTopOperator && topTaxLookup != null)
        {
            propertyResults = propertyResults
                .OrderByDescending(x => topTaxLookup.TryGetValue(x.Property.Id, out var tax) ? tax : 0m)
                .ToList();

            if (!isUnpaged)
                propertyResults = propertyResults.Skip(skip).Take(pageSize).ToList();
        }

        var propertyIds = propertyResults.Select(x => x.Property.Id).ToList();

        // Get RV (Rateable Value) from TransMastRV table - get latest value per property
        var rvValues = await _context.TransMastRV
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                RateableValue = g.OrderByDescending(x => x.Id).Select(x => x.RateableValue).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Get CV (Capital Value) from TransMastCV table - get latest value per property
        var cvValues = await _context.TransMastCV
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                CapitalValue = g.OrderByDescending(x => x.Id).Select(x => x.CapitalValue).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Get Total Tax from TransMast table - sum all tax amounts per property
        var totalTaxAmounts = await _context.TransMast
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                TotalTax = g.Sum(x => x.TaxAmount)
            })
            .ToListAsync(cancellationToken);

        // Convert to dictionaries for O(1) lookup performance
        var rvDictionary = rvValues.ToDictionary(x => x.PropertyId, x => x.RateableValue);
        var cvDictionary = cvValues.ToDictionary(x => x.PropertyId, x => x.CapitalValue);
        var totalTaxDictionary = totalTaxAmounts.ToDictionary(x => x.PropertyId, x => x.TotalTax);

        // Map to response DTOs
        var result = propertyResults.Select(pr =>
        {
            rvDictionary.TryGetValue(pr.Property.Id, out var rv);
            cvDictionary.TryGetValue(pr.Property.Id, out var cv);
            totalTaxDictionary.TryGetValue(pr.Property.Id, out var totalTax);

            return new PropertySearchResponseDto
            {
                PropertyId = pr.Property.Id,
                UPICId = pr.Property.UPICId,
                ZoneName = pr.Zone?.ZoneNo,
                WardName = pr.Ward?.WardNo,
                PropertyNo = pr.Property.PropertyNo,
                PartitionNo = pr.Property.PartitionNo,
                OldPropertyNo = pr.OldProperty?.OldPropertyNo,
                CitySurveyNo = pr.Property.CSN,
                PlotNo = pr.Property.PlotNo,
                WingFlatNo = pr.Property.FlatOrShopNo,
                CategoryName = pr.Category?.PropertyCategoryName,
                PropertyDescription = pr.Property.Type,
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

        return (totalCount, result);
    }

    public async Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        // Get all active properties — read-only count query.
        var allProperties = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        // 1. Registered Property Count: All properties present in PropertyMast
        var registeredCount = await allProperties.CountAsync(cancellationToken);

        // 2. Geo Sequencing Property Count: Properties where PropertyNo is present
        var geoSequencingCount = await allProperties
            .Where(p => !string.IsNullOrEmpty(p.PropertyNo))
            .CountAsync(cancellationToken);

        // 3. Survey Property Count: Currently 0 (Work in Progress)
        var surveyCount = 0;

        // 4. Data Processing Property Count: Currently 0 (Work in Progress)
        var dataProcessingCount = 0;

        // 5. Quality Analysis Property Count: Currently 0 (Work in Progress)
        var qualityAnalysisCount = 0;

        // 6. Assessment Completed Property Count: Currently 0 (Work in Progress)
        var assessmentCompletedCount = 0;

        return new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = registeredCount,
            GeoSequencingPropertyCount = geoSequencingCount,
            SurveyPropertyCount = surveyCount,
            DataProcessingPropertyCount = dataProcessingCount,
            QualityAnalysisPropertyCount = qualityAnalysisCount,
            AssessmentCompletedPropertyCount = assessmentCompletedCount
        };
    }
}
