using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Enums;
using NtisPlatform.Core.Entities;
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

                // Handle RV or CV filtering (from TransMast with RVorCV field)
                if (valuationMethod == "RV" || valuationMethod == "CV")
                {
                    var rvOrCv = valuationMethod;

                    // Get matching property IDs from TransMast
                    var matchingPropertyIds = _context.TransMast
                        .AsNoTracking()
                        .Where(t => t.IsActive && !t.MarkedForDeletion && t.RVorCV == rvOrCv)
                        .GroupBy(t => t.PropertyId)
                        .Select(g => new { PropertyId = g.Key, Value = g.Max(x => x.RVorCVValue) })
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

        // Exclude apartment units from grid results: show only structures/main properties
        query = query.Where(x => x.Category == null ||
                                x.Category.PropertyCategoryName != ApartmentCategoryName ||
                                (x.Category.PropertyCategoryName == ApartmentCategoryName &&
                                 (string.IsNullOrEmpty(x.Property.PartitionNo))));

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
                    && t.RVorCV == "RV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    RateableValue = g.Max(x => x.RVorCVValue)
                })
                .ToListAsync(cancellationToken);

            // Get CV values
            var cvValues = await _context.TransMast
                .AsNoTracking()
                .Where(t =>
                    allPropertyIds.Contains(t.PropertyId)
                    && t.IsActive
                    && !t.MarkedForDeletion
                    && t.RVorCV == "CV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    CapitalValue = g.Max(x => x.RVorCVValue)
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
            // RV (Rateable Value) from TransMast where RVorCV = 'RV'
            var rvValues = await _context.TransMast
                .AsNoTracking()
                .Where(t =>
                    propertyIds.Contains(t.PropertyId)
                    && t.IsActive
                    && !t.MarkedForDeletion
                    && t.RVorCV == "RV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    RateableValue = g.Max(x => x.RVorCVValue)
                })
                .ToListAsync(cancellationToken);

            // CV (Capital Value) from TransMast where RVorCV = 'CV'
            var cvValues = await _context.TransMast
                .AsNoTracking()
                .Where(t =>
                    propertyIds.Contains(t.PropertyId)
                    && t.IsActive
                    && !t.MarkedForDeletion
                    && t.RVorCV == "CV")
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    CapitalValue = g.Max(x => x.RVorCVValue)
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
            .Select(x => new { x.Property.PropertyNo, x.Property.Id })
            .Distinct()
            .ToList();

        var unitCountsQuery = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion &&
                       apartmentMainProperties.Select(a => a.PropertyNo).Contains(p.PropertyNo) &&
                       !string.IsNullOrEmpty(p.PartitionNo))
            .GroupBy(p => p.PropertyNo)
            .Select(g => new { PropertyNo = g.Key, UnitCount = g.Count() })
            .ToDictionaryAsync(x => x.PropertyNo, x => x.UnitCount, cancellationToken);

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
                string.IsNullOrEmpty(pr.Property.PartitionNo) &&
                unitCountsQuery.TryGetValue(pr.Property.PropertyNo, out var unitCount))
            {
                childUnitCount = unitCount;
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
        var parentProperty = await _context.PropertyMast
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (parentProperty == null)
            return new ApartmentUnitListResponseDto { Items = new List<PropertySearchResponseDto>(), TotalCount = 0 };

        var childrenQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion && p.PropertyNo == parentProperty.PropertyNo && p.Id != propertyId);

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

            // Type of Use filter (filters by PropertyDescription, same as PropertyTypeId)
            if (searchRequest.TypeOfUseId.HasValue)
            {
                query = query.Where(x => x.PropertyType != null && x.PropertyType.Id == searchRequest.TypeOfUseId);
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

            // Property No From filter
            if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom))
            {
                query = query.Where(x => string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoFrom) >= 0);
            }

            // Property No To filter
            if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
            {
                query = query.Where(x => string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoTo) <= 0);
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

        var rvValues = await _context.TransMastRV
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                RateableValue = g.OrderByDescending(x => x.Id).Select(x => x.RateableValue).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var cvValues = await _context.TransMastCV
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                CapitalValue = g.OrderByDescending(x => x.Id).Select(x => x.CapitalValue).FirstOrDefault()
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
