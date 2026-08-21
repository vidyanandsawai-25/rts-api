using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Automation Dashboard card operations.
/// Handles main cards and workflow stage cards ONLY.
/// Stage-specific grids are handled by dedicated repositories.
/// </summary>
public class AutomationDashboardRepository : WorkflowStageBaseRepository, IAutomationDashboardRepository
{
    private const string TaxTotalCode = "TaxTotal";
    private const string TaxTotalName = "TaxTotal";
    private const string AssessedStatusName = "ASSESSED";
    private const string UnassessedStatusName = "UNASSESSED";
    private const string ApprovedByAcdStatus = "ApprovedByACD";
    private const string CalculationTypeRV = "RV";

    public AutomationDashboardRepository(ApplicationDbContext context) : base(context)
    {
    }

    #region Public API Methods

    public async Task<List<PropertyWorkflowStageMasterEntity>> ReadWorkflowStagesAsync(
        int? workflowStageId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PropertyWorkflowStageMasterEntity> stagesBaseQuery = _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (workflowStageId.HasValue)
            stagesBaseQuery = stagesBaseQuery.Where(s => s.Id == workflowStageId.Value);

        return await stagesBaseQuery
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, (int PropertyCount, int StructureCount, int UnitCount)>> ReadWorkflowStageCountsAsync(
        IEnumerable<int> stageIds,
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
    {
        var selectedStageIds = stageIds.Distinct().ToList();
        if (!selectedStageIds.Any())
            return new Dictionary<int, (int PropertyCount, int StructureCount, int UnitCount)>();
    
        var propertiesBaseQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        propertiesBaseQuery = ApplyMainGridPropertyTypeFilters(propertiesBaseQuery, searchRequest);

        return await (
            from d in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in propertiesBaseQuery on d.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            where selectedStageIds.Contains(d.WorkflowStageId)
                  && w.IsActive
                  && z.IsActive
                  && (searchRequest == null || !searchRequest.ZoneId.HasValue || w.ZoneId == searchRequest.ZoneId.Value)
                  && (searchRequest == null || !searchRequest.WardId.HasValue || p.WardId == searchRequest.WardId.Value)
            select new
            {
                d.WorkflowStageId,
                PropertyId = p.Id,
                p.PartitionNo
            }
        )
        .Distinct()
        .GroupBy(x => x.WorkflowStageId)
        .Select(g => new
        {
            WorkflowStageId = g.Key,
            PropertyCount = g.Count(),
            StructureCount = g.Count(x => x.PartitionNo == null || x.PartitionNo.Trim() == "")
        })
        .ToDictionaryAsync(
            x => x.WorkflowStageId,
            x => (x.PropertyCount, x.StructureCount, UnitCount: x.PropertyCount),
            cancellationToken);
    }

    public Task<List<int>> ReadCompletedWorkflowStageIdsAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        return (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join stage in _context.PropertyWorkflowStageMaster.AsNoTracking() on pwd.WorkflowStageId equals stage.Id
            where pwd.IsActive
                  && stage.IsActive
                  && pwd.PropertyId == propertyId
            select pwd.WorkflowStageId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, string?>> ReadWorkflowStageOfficerNamesAsync(
        IEnumerable<int> stageIds,
        CancellationToken cancellationToken = default)
    {
        var selectedStageIds = stageIds.Distinct().ToList();
        if (!selectedStageIds.Any())
            return new Dictionary<int, string?>();

        var stageOfficers = await (
            from stage in _context.PropertyWorkflowStageMaster.AsNoTracking()
            join user in _context.UserMasters.AsNoTracking() on stage.UserId equals (int?)user.Id into users
            from user in users.DefaultIfEmpty()
            where stage.IsActive && selectedStageIds.Contains(stage.Id)
            select new
            {
                StageId = stage.Id,
                FirstName = user == null ? null : user.FirstName,
                MiddleName = user == null ? null : user.MiddleName,
                LastName = user == null ? null : user.LastName
            })
            .ToListAsync(cancellationToken);

        return stageOfficers
            .ToDictionary(
                stage => stage.StageId,
                stage => FormatOfficerName(stage.FirstName, stage.MiddleName, stage.LastName));
    }

    private static string? FormatOfficerName(string? firstName, string? middleName, string? lastName)
    {
        var officerName = string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(name => !string.IsNullOrWhiteSpace(name)));

        return string.IsNullOrWhiteSpace(officerName) ? null : officerName;
    }

    private static IQueryable<PropertyEntity> ApplyDashboardFilters(IQueryable<PropertyEntity> query,PropertySearchRequestDto? request)
    {
        if (request is null) return query;

        if (request.PropertyAssessmentStatusId.HasValue)
            query = query.Where(p => p.PropertyAssessmentStatusId == request.PropertyAssessmentStatusId.Value);

        if (request.PropertyDescriptionId.HasValue)
            query = query.Where(p => p.PropertyTypeId == request.PropertyDescriptionId.Value);

        if (request.PropertyTypeId.HasValue)
            query = query.Where(p => p.PropertyTypeId == request.PropertyTypeId.Value);

        if (request.WardId.HasValue)
            query = query.Where(p => p.WardId == request.WardId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        return query;
    }

    private async Task<(int PropertyCount, int StructureCount, int UnitCount)> CountDashboardPropertiesAsync(IQueryable<PropertyEntity> query,CancellationToken cancellationToken)
    {
        var counts = await (
            from p in query
            join pc in _context.PropertyCategoryMaster on p.CategoryId equals pc.Id into categoryJoin
            from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
            group new { p, pc } by 1 into g
            select new
            {
                PropertyCount = g.Count(),
                UnitsOnlyCount = g.Count(x => x.pc != null
                                               //&& x.pc.PropertyCategoryName == ApartmentCategoryName
                                               && x.p.PartitionNo != null
                                               && x.p.PartitionNo.Trim() != "")
            }
        ).FirstOrDefaultAsync(cancellationToken);

        var propertyCount = counts?.PropertyCount ?? 0;
        var unitsOnlyCount = counts?.UnitsOnlyCount ?? 0;

        return (propertyCount, propertyCount - unitsOnlyCount, unitsOnlyCount);
    }

    public async Task<DashboardCardBreakdownDto> ReadPreviouslyRegisteredBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _context.PropertyMastOld
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PropertyCount = g.Count(),
                StructureCount = g.Count(p => p.OldPartitionNo == null || p.OldPartitionNo.Trim() == ""),
                Demand = g.Sum(p => p.OldTotalTax ?? 0d)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new DashboardCardBreakdownDto
        {
            PropertyCount = counts?.PropertyCount ?? 0,
            StructureCount = counts?.StructureCount ?? 0,
            UnitCount = counts?.PropertyCount ?? 0,
            Demand = counts == null ? 0m : Convert.ToDecimal(counts.Demand)
        };
    }

    public async Task<Dictionary<string, int>> ReadAssessmentStatusIdsAsync(CancellationToken cancellationToken = default)
    {
        var statusNames = new[] { AssessedStatusName, UnassessedStatusName };
        return await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive && statusNames.Contains(s.StatusName.ToUpper()))
            .Select(s => new { StatusName = s.StatusName.ToUpper(), s.Id })
            .ToDictionaryAsync(s => s.StatusName, s => s.Id, cancellationToken);
    }

    public async Task<DashboardCardBreakdownDto> ReadPropertyBreakdownByAssessmentStatusAsync(
        int statusId,
        PropertySearchRequestDto? searchRequest = null,
        bool includeDemand = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDashboardPropertyQuery(searchRequest);
        var statusQuery = query.Where(p => p.PropertyAssessmentStatusId == statusId);
        var (propertyCount, structureCount, unitCount) = await CountDashboardPropertiesAsync(statusQuery, cancellationToken);
        var demand = includeDemand ? await GetNewTaxTotalDemandAsync(statusQuery, cancellationToken) : 0m;

        return new DashboardCardBreakdownDto
        {
            PropertyCount = propertyCount,
            StructureCount = structureCount,
            UnitCount = unitCount,
            Demand = demand
        };
    }

    public async Task<DashboardCardBreakdownDto> ReadAcdApprovedPropertyBreakdownAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDashboardPropertyQuery(searchRequest);
        var approvedPropertyIds = _context.PropertySignatureDetails
            .AsNoTracking()
            .Where(signature => signature.IsActive && signature.SignStatus == ApprovedByAcdStatus)
            .Select(signature => signature.PropertyId)
            .Distinct();

        var approvedProperties = query.Where(property => approvedPropertyIds.Contains(property.Id));

        var counts = await approvedProperties
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PropertyCount = g.Count(),
                StructureCount = g.Count(property => property.PartitionNo == null || property.PartitionNo.Trim() == "")
            })
            .FirstOrDefaultAsync(cancellationToken);

        var currentDemand = await GetNewTaxTotalDemandAsync(approvedProperties, cancellationToken);

        return new DashboardCardBreakdownDto
        {
            PropertyCount = counts?.PropertyCount ?? 0,
            StructureCount = counts?.StructureCount ?? 0,
            UnitCount = counts?.PropertyCount ?? 0,
            Demand = currentDemand
        };
    }

    private async Task<decimal> GetNewTaxTotalDemandAsync(IQueryable<PropertyEntity> query, CancellationToken cancellationToken)
    {
        var propertyIds = query.Select(p => p.Id);

        var demand = await (
            from t in _context.TransMast.AsNoTracking()
            join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
            where propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion
                  && tax.IsActive && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
            select t.TaxAmount
        ).SumAsync(cancellationToken);

        return demand;
    }

    private async Task<decimal> GetOldTaxTotalDemandAsync(IQueryable<PropertyEntity> query, CancellationToken cancellationToken)
    {
        var propertyMastOldIds = query
            .Where(p => p.PropertyMastOldId != null)
            .Select(p => p.PropertyMastOldId!.Value);

        var demand = await (
            from t in _context.TransMastOld.AsNoTracking()
            join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
            where propertyMastOldIds.Contains(t.PropertyMastOldId) && t.IsActive && !t.MarkedForDeletion
                  && tax.IsActive && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
            select t.TaxAmount
        ).SumAsync(cancellationToken);

        return demand;
    }

    public Task<SubGridDataProjection> GetSubGridDataAsync(SubGridQueryParameters query,CancellationToken cancellationToken = default)
        => GetSubGridDataCoreAsync(
            query.ZoneId,
            query.WorkflowStageId,
            query.WardId,
            query.PropertyTypeId,
            query.PropertyTypeCategoryId,
            query.AssessmentTypeId,
            query.PropertyNo,
            query.OwnerName,
            query.Search,
            query.Structure,
            query.Unit,
            query.PendingStructure,
            query.PendingUnit,
            query.CompletedStructure,
            query.CompletedUnit,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

    public Task<SubGridDataProjection> GetSubGridDataAsync(WardSubGridQueryParameters query,CancellationToken cancellationToken = default)
        => GetSubGridDataCoreAsync(
            null,
            query.WorkflowStageId,
            query.WardId,
            query.PropertyTypeId,
            query.PropertyTypeCategoryId,
            query.AssessmentTypeId,
            query.PropertyNo,
            query.OwnerName,
            query.Search,
            query.Structure,
            query.Unit,
            query.PendingStructure,
            query.PendingUnit,
            query.CompletedStructure,
            query.CompletedUnit,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

    private async Task<SubGridDataProjection> GetSubGridDataCoreAsync(
        int? zoneId,
        int? workflowStageId,
        int? wardId,
        int? propertyTypeId,
        int? propertyTypeCategoryId,
        int? assessmentTypeId,
        string? propertyNo,
        string? ownerNameFilter,
        string? search,
        bool? structure,
        bool? unit,
        bool? pendingStructure,
        bool? pendingUnit,
        bool? completedStructure,
        bool? completedUnit,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

        var workflowStageName = "All Stages";
        if (workflowStageId is int selectedWorkflowStageId && selectedWorkflowStageId > 0)
        {
            var workflowStage = await _context.PropertyWorkflowStageMaster
                .AsNoTracking()
                .Where(s => s.IsActive && s.Id == selectedWorkflowStageId)
                .Select(s => new { s.Id, s.StageName })
                .FirstOrDefaultAsync(cancellationToken);

            if (workflowStage == null)
                return new SubGridDataProjection();

            workflowStageName = workflowStage.StageName;
        }

        var zoneName = "All Zones";
        var zoneNo = string.Empty;
        int? resolvedWardId = null;
        string? resolvedWardNo = null;

        if (wardId is int selectedWardId && selectedWardId > 0)
        {
            var ward = await (
                from w in _context.WardMaster.AsNoTracking()
                join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
                where w.IsActive
                      && z.IsActive
                      && w.Id == selectedWardId
                select new
                {
                    WardId = w.Id,
                    WardNo = w.WardNo,
                    ZoneId = z.Id,
                    ZoneName = z.Description ?? z.ZoneNo,
                    ZoneNo = z.ZoneNo
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (ward == null)
                return new SubGridDataProjection
                {
                    WorkflowStageId = workflowStageId.GetValueOrDefault(),
                    WorkflowStageName = workflowStageName
                };

            resolvedWardId = ward.WardId;
            resolvedWardNo = ward.WardNo;

            if (!zoneId.HasValue || zoneId.Value <= 0)
            {
                zoneId = ward.ZoneId;
                zoneName = ward.ZoneName;
                zoneNo = ward.ZoneNo;
            }
        }

        if (zoneId is int selectedZoneId && selectedZoneId > 0)
        {
            var zone = await _context.ZoneMaster
                .AsNoTracking()
                .Where(z => z.IsActive && z.Id == selectedZoneId)
                .Select(z => new { z.Id, ZoneName = z.Description ?? z.ZoneNo, z.ZoneNo })
                .FirstOrDefaultAsync(cancellationToken);

            if (zone == null)
                return new SubGridDataProjection
                {
                    WorkflowStageId = workflowStageId.GetValueOrDefault(),
                    WorkflowStageName = workflowStageName
                };

            zoneName = zone.ZoneName;
            zoneNo = zone.ZoneNo;
        }

        var basePropertyQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(pm => pm.IsActive && !pm.MarkedForDeletion);

        if (propertyTypeId is > 0)
            basePropertyQuery = basePropertyQuery.Where(p => p.PropertyTypeId == propertyTypeId.Value);

        if (propertyTypeCategoryId is > 0)
            basePropertyQuery = ApplyPropertyTypeCategoryFilter(basePropertyQuery, propertyTypeCategoryId.Value);

        if (assessmentTypeId is > 0)
            basePropertyQuery = basePropertyQuery.Where(p => p.PropertyAssessmentStatusId == assessmentTypeId.Value);

        var propertyQuery = from pm in basePropertyQuery
                            join wd in _context.WardMaster.AsNoTracking() on pm.WardId equals wd.Id
                            join z in _context.ZoneMaster.AsNoTracking() on wd.ZoneId equals z.Id
                            where wd.IsActive
                                  && z.IsActive
                            select new SubGridPropertyFilterProjection
                            {
                                PropertyId = pm.Id,
                                PropertyAssessmentStatusId = pm.PropertyAssessmentStatusId,
                                WardId = pm.WardId,
                                WardNo = wd.WardNo,
                                CategoryId = pm.CategoryId,
                                PropertyNo = pm.PropertyNo,
                                PartitionNo = pm.PartitionNo,
                                OwnerName = pm.OwnerName,
                                OccupierName = pm.OccupierName,
                                MobileNo = pm.MobileNo,
                                AlternateMobileNo = pm.AlternateMobileNo,
                                OccupierMobileNo = pm.OccupierMobileNo,
                                Address = pm.Address,
                                FlatOrShopName = pm.FlatOrShopName,
                                UPICId = pm.UPICId,
                                PropertyMastOldId = pm.PropertyMastOldId,
                                PropertyTypeId = pm.PropertyTypeId,
                                ZoneId = wd.ZoneId,
                                ZoneNo = z.ZoneNo,
                                ZoneName = z.Description ?? z.ZoneNo,
                                IsPropertyOpenPlot = pm.OpenPlot == true
                            };

        if (workflowStageId is int filterWorkflowStageId && filterWorkflowStageId > 0)
        {
            var hasDataEntryMetricFilter = IsDataEntryStage(workflowStageName)
                                           && (pendingStructure == true
                                               || pendingUnit == true
                                               || completedStructure == true
                                               || completedUnit == true);

            if (hasDataEntryMetricFilter)
            {
                propertyQuery = propertyQuery.Where(p =>
                    (pendingStructure == true
                     && !_context.PropertyWorkflowDetails.AsNoTracking()
                         .Any(pwd => pwd.WorkflowStageId == filterWorkflowStageId && pwd.PropertyId == p.PropertyId)
                     && (p.PartitionNo == null || p.PartitionNo.Trim() == ""))
                    || (pendingUnit == true
                        && !_context.PropertyWorkflowDetails.AsNoTracking()
                            .Any(pwd => pwd.WorkflowStageId == filterWorkflowStageId && pwd.PropertyId == p.PropertyId))
                    || (completedStructure == true
                        && _context.PropertyWorkflowDetails.AsNoTracking()
                            .Any(pwd => pwd.WorkflowStageId == filterWorkflowStageId && pwd.PropertyId == p.PropertyId)
                        && (p.PartitionNo == null || p.PartitionNo.Trim() == ""))
                    || (completedUnit == true
                        && _context.PropertyWorkflowDetails.AsNoTracking()
                            .Any(pwd => pwd.WorkflowStageId == filterWorkflowStageId && pwd.PropertyId == p.PropertyId)));
            }
            else
            {
                propertyQuery = propertyQuery.Where(p =>
                    _context.PropertyWorkflowDetails.AsNoTracking()
                        .Any(pwd => pwd.WorkflowStageId == filterWorkflowStageId && pwd.PropertyId == p.PropertyId));
            }
        }

        if (zoneId is int filterZoneId && filterZoneId > 0)
            propertyQuery = propertyQuery.Where(p => p.ZoneId == filterZoneId);

        if (wardId is int filterWardId && filterWardId > 0)
            propertyQuery = propertyQuery.Where(p => p.WardId == filterWardId);

        if (structure == true && unit != true)
            propertyQuery = propertyQuery.Where(p => p.PartitionNo == null || p.PartitionNo.Trim() == "");

        if (!string.IsNullOrWhiteSpace(propertyNo))
        {
            propertyQuery = ApplySubGridPropertyNoFilter(propertyQuery, propertyNo);
        }

        if (!string.IsNullOrWhiteSpace(ownerNameFilter))
        {
            var ownerName = ownerNameFilter.Trim();
            propertyQuery = propertyQuery.Where(p => p.OwnerName != null && p.OwnerName.Contains(ownerName));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            propertyQuery = IsFormattedSubGridPropertySearch(search)
                ? ApplySubGridFormattedPropertySearch(propertyQuery, search)
                : ApplySubGridGlobalSearch(
                    propertyQuery,
                    search,
                    hasLocationFilter: (zoneId is > 0) || (wardId is > 0));
        }

        var propertyIdsQuery = propertyQuery.Select(p => p.PropertyId);

        var totalCount = await propertyIdsQuery.Distinct().CountAsync(cancellationToken);

        if (totalCount == 0)
            return new SubGridDataProjection
            {
                WorkflowStageId = workflowStageId.GetValueOrDefault(),
                WorkflowStageName = workflowStageName,
                ZoneId = zoneId.GetValueOrDefault(),
                ZoneName = zoneName,
                ZoneNo = zoneNo,
                WardId = resolvedWardId,
                WardNo = resolvedWardNo,
                TotalCount = 0
            };

        var pagePropertyIds = await propertyIdsQuery
            .Distinct()
            .OrderBy(id => id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if ((!zoneId.HasValue || zoneId.Value <= 0) || !resolvedWardId.HasValue)
        {
            var resultContext = await propertyQuery
                .Select(p => new
                {
                    p.ZoneId,
                    p.ZoneName,
                    p.ZoneNo,
                    p.WardId,
                    p.WardNo
                })
                .Distinct()
                .Take(2)
                .ToListAsync(cancellationToken);

            if (resultContext.Count == 1)
            {
                var context = resultContext[0];
                zoneId = context.ZoneId;
                zoneName = context.ZoneName ?? zoneName;
                zoneNo = context.ZoneNo ?? zoneNo;
                resolvedWardId = context.WardId;
                resolvedWardNo = context.WardNo;
            }
        }

        return await FetchSubGridPropertyDetailsAsync(
            workflowStageId.GetValueOrDefault(),
            workflowStageName,
            zoneId.GetValueOrDefault(),
            zoneName,
            zoneNo,
            resolvedWardId,
            resolvedWardNo,
            totalCount,
            pagePropertyIds,
            cancellationToken);
    }

    private static IQueryable<SubGridPropertyFilterProjection> ApplySubGridPropertyNoFilter(IQueryable<SubGridPropertyFilterProjection> query,string propertyNo)
    {
        var normalizedPropertyNo = propertyNo.Trim();
        var parsed = ParseSubGridPropertyNo(normalizedPropertyNo);

        if (!string.IsNullOrWhiteSpace(parsed.WardNo) && !string.IsNullOrWhiteSpace(parsed.PropertyNo))
        {
            var wardNo = parsed.WardNo.ToUpper();
            var basePropertyNo = parsed.PropertyNo;

            if (!string.IsNullOrWhiteSpace(parsed.PartitionNo))
            {
                var partitionNo = parsed.PartitionNo;
                return query.Where(p =>
                    p.WardNo != null
                    && p.PropertyNo != null
                    && p.PartitionNo != null
                    && p.WardNo.ToUpper() == wardNo
                    && p.PropertyNo == basePropertyNo
                    && p.PartitionNo == partitionNo);
            }

            return query.Where(p =>
                p.WardNo != null
                && p.PropertyNo != null
                && p.WardNo.ToUpper() == wardNo
                && p.PropertyNo == basePropertyNo
                && (p.PartitionNo == null || p.PartitionNo.Trim() == ""));
        }

        return query.Where(p => p.PropertyNo != null && p.PropertyNo.Contains(normalizedPropertyNo));
    }

    private IQueryable<PropertyEntity> BuildDashboardPropertyQuery(PropertySearchRequestDto? searchRequest)
    {
        var query = _context.PropertyMast.AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion && p.PropertyNo != null && p.PropertyNo != "");

        return ApplyDashboardFilters(query, searchRequest);
    }

    private static IQueryable<SubGridPropertyFilterProjection> ApplySubGridFormattedPropertySearch(
        IQueryable<SubGridPropertyFilterProjection> query,
        string propertyNo)
    {
        var parsed = ParseSubGridPropertyNo(propertyNo.Trim());
        var wardNo = parsed.WardNo;
        var basePropertyNo = parsed.PropertyNo;

        if (string.IsNullOrWhiteSpace(wardNo) || string.IsNullOrWhiteSpace(basePropertyNo))
            return query;

        if (!string.IsNullOrWhiteSpace(parsed.PartitionNo))
        {
            var partitionNo = parsed.PartitionNo;
            return query.Where(p =>
                p.WardNo == wardNo
                && p.PropertyNo == basePropertyNo
                && p.PartitionNo == partitionNo);
        }

        return query.Where(p =>
            p.WardNo == wardNo
            && p.PropertyNo == basePropertyNo);
    }

    private IQueryable<SubGridPropertyFilterProjection> ApplySubGridGlobalSearch(
        IQueryable<SubGridPropertyFilterProjection> query,
        string search,
        bool hasLocationFilter)
    {
        var trimmedSearch = search.Trim();
        var numericSearch = GetDigitsOnly(trimmedSearch);

        if (numericSearch.Length >= 3 && IsNumericLikeSearch(trimmedSearch))
            return ApplyNumericSubGridGlobalSearch(query, trimmedSearch, numericSearch);

        var matchingCategoryIds = _context.PropertyCategoryMaster
            .AsNoTracking()
            .Where(category => category.IsActive
                               && category.PropertyCategoryName != null
                               && EF.Functions.Like(category.PropertyCategoryName, $"%{trimmedSearch}%"))
            .Select(category => category.Id);

        var matchingPropertyTypeIds = _context.PropertyTypeMasters
            .AsNoTracking()
            .Where(propertyType => propertyType.IsActive
                                   && ((propertyType.PropertyDescription != null && EF.Functions.Like(propertyType.PropertyDescription, $"%{trimmedSearch}%"))
                                       || (propertyType.Type != null && EF.Functions.Like(propertyType.Type, $"%{trimmedSearch}%"))))
            .Select(propertyType => propertyType.Id);

        var matchingAssessmentStatusIds = _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(status => status.IsActive && EF.Functions.Like(status.StatusName, $"%{trimmedSearch}%"))
            .Select(status => status.Id);

        var matchingSocietyPropertyIds = GetMatchingSocietyPropertyIds(trimmedSearch);
        var matchingDetailPropertyIds = GetMatchingPropertyDetailPropertyIds(trimmedSearch);
        var matchingOldPropertyIds = GetMatchingOldPropertyIds(trimmedSearch);
        var matchingPropertyIds = matchingSocietyPropertyIds
            .Union(matchingDetailPropertyIds)
            .Union(GetPropertyIdsMappedToOldIds(matchingOldPropertyIds));

        if (!hasLocationFilter)
        {
            var noLocationMatchingPropertyIds = GetMatchingPropertyIds(trimmedSearch)
                .Union(GetMatchingLocationPropertyIds(trimmedSearch))
                .Union(GetMatchingLookupPropertyIds(
                    matchingCategoryIds,
                    matchingPropertyTypeIds,
                    matchingAssessmentStatusIds))
                .Union(matchingPropertyIds);

            return query.Where(p => noLocationMatchingPropertyIds.Contains(p.PropertyId));
        }

        return query.Where(p =>
            EF.Functions.Like((p.WardNo ?? "") + "-" + (p.PropertyNo ?? "") + "-" + (p.PartitionNo ?? ""), $"%{trimmedSearch}%")
            || EF.Functions.Like((p.WardNo ?? "") + "-" + (p.PropertyNo ?? ""), $"%{trimmedSearch}%")
            || (p.WardNo != null && EF.Functions.Like(p.WardNo, $"%{trimmedSearch}%"))
            || (p.ZoneNo != null && EF.Functions.Like(p.ZoneNo, $"%{trimmedSearch}%"))
            || (p.ZoneName != null && EF.Functions.Like(p.ZoneName, $"%{trimmedSearch}%"))
            || (p.PropertyNo != null && EF.Functions.Like(p.PropertyNo, $"%{trimmedSearch}%"))
            || (p.PartitionNo != null && EF.Functions.Like(p.PartitionNo, $"%{trimmedSearch}%"))
            || (p.OwnerName != null && EF.Functions.Like(p.OwnerName, $"%{trimmedSearch}%"))
            || (p.OccupierName != null && EF.Functions.Like(p.OccupierName, $"%{trimmedSearch}%"))
            || (p.MobileNo != null && EF.Functions.Like(p.MobileNo, $"%{trimmedSearch}%"))
            || (p.AlternateMobileNo != null && EF.Functions.Like(p.AlternateMobileNo, $"%{trimmedSearch}%"))
            || (p.OccupierMobileNo != null && EF.Functions.Like(p.OccupierMobileNo, $"%{trimmedSearch}%"))
            || (p.Address != null && EF.Functions.Like(p.Address, $"%{trimmedSearch}%"))
            || (p.FlatOrShopName != null && EF.Functions.Like(p.FlatOrShopName, $"%{trimmedSearch}%"))
            || (p.UPICId != null && EF.Functions.Like(p.UPICId, $"%{trimmedSearch}%"))
            || (p.CategoryId.HasValue && matchingCategoryIds.Contains(p.CategoryId.Value))
            || (p.PropertyTypeId.HasValue && matchingPropertyTypeIds.Contains(p.PropertyTypeId.Value))
            || (p.PropertyAssessmentStatusId.HasValue && matchingAssessmentStatusIds.Contains(p.PropertyAssessmentStatusId.Value))
            || matchingPropertyIds.Contains(p.PropertyId));
    }

    private IQueryable<SubGridPropertyFilterProjection> ApplyNumericSubGridGlobalSearch(
        IQueryable<SubGridPropertyFilterProjection> query,
        string rawSearch,
        string numericSearch)
    {
        var matchingSocietyPropertyIds = GetMatchingSocietyPropertyIds(rawSearch, numericSearch);
        var matchingOldPropertyIds = GetMatchingOldPropertyIds(rawSearch, numericSearch);
        var matchingPropertyIds = GetMatchingPropertyIds(rawSearch, numericSearch)
            .Union(matchingSocietyPropertyIds)
            .Union(GetPropertyIdsMappedToOldIds(matchingOldPropertyIds));

        return query.Where(p =>
            ((p.WardNo ?? "") + "-" + (p.PropertyNo ?? "") + "-" + (p.PartitionNo ?? "")).Contains(rawSearch)
            || ((p.WardNo ?? "") + "-" + (p.PropertyNo ?? "")).Contains(rawSearch)
            || matchingPropertyIds.Contains(p.PropertyId));
    }

    private IQueryable<int> GetMatchingPropertyIds(string search)
        => _context.PropertyMast
            .AsNoTracking()
            .Where(property =>
                (property.PropertyNo != null && EF.Functions.Like(property.PropertyNo, $"%{search}%"))
                || (property.PartitionNo != null && EF.Functions.Like(property.PartitionNo, $"%{search}%"))
                || (property.OwnerName != null && EF.Functions.Like(property.OwnerName, $"%{search}%"))
                || (property.OccupierName != null && EF.Functions.Like(property.OccupierName, $"%{search}%"))
                || (property.MobileNo != null && EF.Functions.Like(property.MobileNo, $"%{search}%"))
                || (property.AlternateMobileNo != null && EF.Functions.Like(property.AlternateMobileNo, $"%{search}%"))
                || (property.OccupierMobileNo != null && EF.Functions.Like(property.OccupierMobileNo, $"%{search}%"))
                || (property.Address != null && EF.Functions.Like(property.Address, $"%{search}%"))
                || (property.FlatOrShopName != null && EF.Functions.Like(property.FlatOrShopName, $"%{search}%"))
                || (property.UPICId != null && EF.Functions.Like(property.UPICId, $"%{search}%")))
            .Select(property => property.Id);

    private IQueryable<int> GetMatchingLocationPropertyIds(string search)
        => from property in _context.PropertyMast.AsNoTracking()
           join ward in _context.WardMaster.AsNoTracking() on property.WardId equals ward.Id
           join zone in _context.ZoneMaster.AsNoTracking() on ward.ZoneId equals zone.Id
           where property.IsActive
                 && !property.MarkedForDeletion
                 && ward.IsActive
                 && zone.IsActive
                 && (EF.Functions.Like((ward.WardNo ?? "") + "-" + (property.PropertyNo ?? "") + "-" + (property.PartitionNo ?? ""), $"%{search}%")
                     || EF.Functions.Like((ward.WardNo ?? "") + "-" + (property.PropertyNo ?? ""), $"%{search}%")
                     || (ward.WardNo != null && EF.Functions.Like(ward.WardNo, $"%{search}%"))
                     || (zone.ZoneNo != null && EF.Functions.Like(zone.ZoneNo, $"%{search}%"))
                     || (zone.Description != null && EF.Functions.Like(zone.Description, $"%{search}%")))
           select property.Id;

    private IQueryable<int> GetMatchingLookupPropertyIds(
        IQueryable<int> categoryIds,
        IQueryable<int> propertyTypeIds,
        IQueryable<int> assessmentStatusIds)
        => _context.PropertyMast
            .AsNoTracking()
            .Where(property => property.IsActive
                               && !property.MarkedForDeletion
                               && ((property.CategoryId.HasValue && categoryIds.Contains(property.CategoryId.Value))
                                   || (property.PropertyTypeId.HasValue && propertyTypeIds.Contains(property.PropertyTypeId.Value))
                                   || (property.PropertyAssessmentStatusId.HasValue && assessmentStatusIds.Contains(property.PropertyAssessmentStatusId.Value))))
            .Select(property => property.Id);

    private IQueryable<int> GetMatchingPropertyIds(string rawSearch, string numericSearch)
        => _context.PropertyMast
            .AsNoTracking()
            .Where(property =>
                (property.PropertyNo != null && property.PropertyNo.Contains(rawSearch))
                || (property.PartitionNo != null && property.PartitionNo.Contains(rawSearch))
                || (property.UPICId != null && property.UPICId.Contains(rawSearch))
                || (property.MobileNo != null
                    && (property.MobileNo.Contains(rawSearch)
                        || property.MobileNo.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(numericSearch)))
                || (property.AlternateMobileNo != null
                    && (property.AlternateMobileNo.Contains(rawSearch)
                        || property.AlternateMobileNo.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(numericSearch)))
                || (property.OccupierMobileNo != null
                    && (property.OccupierMobileNo.Contains(rawSearch)
                        || property.OccupierMobileNo.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(numericSearch))))
            .Select(property => property.Id);

    private IQueryable<int> GetPropertyIdsMappedToOldIds(IQueryable<int> oldPropertyIds)
        => _context.PropertyMast
            .AsNoTracking()
            .Where(property => property.PropertyMastOldId.HasValue
                               && oldPropertyIds.Contains(property.PropertyMastOldId.Value))
            .Select(property => property.Id)
            .Union(_context.PropertyMapMasters
                .AsNoTracking()
                .Where(propertyMap => propertyMap.ParentPropertyMapId.HasValue
                                      && oldPropertyIds.Contains(propertyMap.ParentPropertyMapId.Value))
                .Select(propertyMap => propertyMap.Id));

    private IQueryable<int> GetMatchingSocietyPropertyIds(string search)
    {
        return (
            from society in _context.SocietyDetailsMast.AsNoTracking()
            join wing in _context.WingEntity.AsNoTracking().Where(wing => wing.IsActive)
                on society.WingId equals wing.Id into wingJoin
            from wing in wingJoin.DefaultIfEmpty()
            where society.PropertyId.HasValue
                  && society.IsActive
                  && !society.MarkedForDeletion
                  && ((society.WingName != null && EF.Functions.Like(society.WingName, $"%{search}%"))
                      || (society.SocietyName != null && EF.Functions.Like(society.SocietyName, $"%{search}%"))
                      || (society.SocietyNameEnglish != null && EF.Functions.Like(society.SocietyNameEnglish, $"%{search}%"))
                      || (society.BuilderName != null && EF.Functions.Like(society.BuilderName, $"%{search}%"))
                      || (society.BuilderNameEnglish != null && EF.Functions.Like(society.BuilderNameEnglish, $"%{search}%"))
                      || (society.ManagerMobileNo != null && EF.Functions.Like(society.ManagerMobileNo, $"%{search}%"))
                      || (society.SecretaryMobileNo != null && EF.Functions.Like(society.SecretaryMobileNo, $"%{search}%"))
                      || (society.BuilderMobileNo != null && EF.Functions.Like(society.BuilderMobileNo, $"%{search}%"))
                      || (wing != null && wing.WingNo != null && EF.Functions.Like(wing.WingNo, $"%{search}%")))
            select society.PropertyId!.Value
        ).Distinct();
    }

    private IQueryable<int> GetMatchingSocietyPropertyIds(string rawSearch, string numericSearch)
        => _context.SocietyDetailsMast
            .AsNoTracking()
            .Where(society => society.PropertyId.HasValue
                              && society.IsActive
                              && !society.MarkedForDeletion
                              && ((society.ManagerMobileNo != null
                                   && (society.ManagerMobileNo.Contains(rawSearch)
                                       || society.ManagerMobileNo.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(numericSearch)))
                                  || (society.SecretaryMobileNo != null
                                      && (society.SecretaryMobileNo.Contains(rawSearch)
                                          || society.SecretaryMobileNo.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(numericSearch)))
                                  || (society.BuilderMobileNo != null
                                      && (society.BuilderMobileNo.Contains(rawSearch)
                                          || society.BuilderMobileNo.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(numericSearch)))))
            .Select(society => society.PropertyId!.Value)
            .Distinct();

    private IQueryable<int> GetMatchingPropertyDetailPropertyIds(string search)
    {
        return (
            from detail in _context.PropertyDetails.AsNoTracking()
            join typeOfUse in _context.TypeOfUse.AsNoTracking().Where(typeOfUse => typeOfUse.IsActive)
                on detail.TypeOfUseId equals typeOfUse.Id into typeOfUseJoin
            from typeOfUse in typeOfUseJoin.DefaultIfEmpty()
            where detail.IsActive
                  && !detail.MarkedForDeletion
                  && ((detail.ConstructionYear != null && EF.Functions.Like(detail.ConstructionYear, $"%{search}%"))
                      || (detail.AssessmentYear != null && EF.Functions.Like(detail.AssessmentYear, $"%{search}%"))
                      || (typeOfUse != null
                          && ((typeOfUse.Description != null && EF.Functions.Like(typeOfUse.Description, $"%{search}%"))
                              || (typeOfUse.Type != null && EF.Functions.Like(typeOfUse.Type, $"%{search}%"))
                              || (typeOfUse.TypeOfUseCode != null && EF.Functions.Like(typeOfUse.TypeOfUseCode, $"%{search}%")))))
            select detail.PropertyId
        ).Distinct();
    }

    private IQueryable<int> GetMatchingOldPropertyIds(string search)
        => _context.PropertyMastOld
            .AsNoTracking()
            .Where(oldProperty =>
                EF.Functions.Like((oldProperty.OldWardNo ?? "") + "-" + (oldProperty.OldPropertyNo ?? "") + "-" + (oldProperty.OldPartitionNo ?? ""), $"%{search}%")
                || EF.Functions.Like((oldProperty.OldWardNo ?? "") + "-" + (oldProperty.OldPropertyNo ?? ""), $"%{search}%")
                || (oldProperty.OldWardNo != null && EF.Functions.Like(oldProperty.OldWardNo, $"%{search}%"))
                || (oldProperty.OldPropertyNo != null && EF.Functions.Like(oldProperty.OldPropertyNo, $"%{search}%"))
                || (oldProperty.OldPartitionNo != null && EF.Functions.Like(oldProperty.OldPartitionNo, $"%{search}%"))
                || (oldProperty.OldEgovNo != null && EF.Functions.Like(oldProperty.OldEgovNo, $"%{search}%"))
                || (oldProperty.OldMobileNo != null && EF.Functions.Like(oldProperty.OldMobileNo, $"%{search}%"))
                || (oldProperty.OldOwnerName != null && EF.Functions.Like(oldProperty.OldOwnerName, $"%{search}%"))
                || (oldProperty.OldOccupierName != null && EF.Functions.Like(oldProperty.OldOccupierName, $"%{search}%"))
                || (oldProperty.OldAddress != null && EF.Functions.Like(oldProperty.OldAddress, $"%{search}%"))
                || (oldProperty.OldUseType != null && EF.Functions.Like(oldProperty.OldUseType, $"%{search}%"))
                || (oldProperty.OldConstructionYear != null && EF.Functions.Like(oldProperty.OldConstructionYear, $"%{search}%")))
            .Select(oldProperty => oldProperty.Id);

    private IQueryable<int> GetMatchingOldPropertyIds(string rawSearch, string numericSearch)
        => _context.PropertyMastOld
            .AsNoTracking()
            .Where(oldProperty =>
                (oldProperty.OldMobileNo != null
                 && (oldProperty.OldMobileNo.Contains(rawSearch)
                     || oldProperty.OldMobileNo.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(numericSearch)))
                || (oldProperty.OldPropertyNo != null && oldProperty.OldPropertyNo.Contains(rawSearch))
                || (oldProperty.OldPartitionNo != null && oldProperty.OldPartitionNo.Contains(rawSearch))
                || (oldProperty.OldEgovNo != null && oldProperty.OldEgovNo.Contains(rawSearch)))
            .Select(oldProperty => oldProperty.Id);

    private static string GetDigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());

    private static bool IsNumericLikeSearch(string value)
        => value.All(c => char.IsDigit(c)
                          || char.IsWhiteSpace(c)
                          || c is '+' or '-' or '(' or ')');

    private static bool IsFormattedSubGridPropertySearch(string search)
    {
        var parsed = ParseSubGridPropertyNo(search.Trim());
        return !string.IsNullOrWhiteSpace(parsed.WardNo)
               && !string.IsNullOrWhiteSpace(parsed.PropertyNo)
               && parsed.PropertyNo.All(char.IsDigit)
               && (string.IsNullOrWhiteSpace(parsed.PartitionNo)
                   || parsed.PartitionNo.All(c => char.IsDigit(c) || c == '-'));
    }

    private static (string? WardNo, string? PropertyNo, string? PartitionNo) ParseSubGridPropertyNo(string propertyNo)
    {
        var parts = propertyNo
            .Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            >= 3 => (parts[0], parts[1], string.Join("-", parts.Skip(2))),
            2 => (parts[0], parts[1], null),
            _ => (null, propertyNo, null)
        };
    }

    private static bool IsDataEntryStage(string workflowStageName)
        => string.Equals(workflowStageName, "DataEntry", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Private Query Builders

    public async Task<SubGridDataProjection> GetPendingAssessmentPropsAsync(
        PendingAssessmentQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPageNumber, normalizedPageSize) = NormalizePaging(query.PageNumber, query.PageSize);

        var workflowStage = await _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .Where(s => s.IsActive && s.StageName == AssessmentStageName)
            .Select(s => new { s.Id, s.StageName })
            .FirstOrDefaultAsync(cancellationToken);

        if (workflowStage == null)
            return new SubGridDataProjection();

        var zoneName = "All Zones";
        var zoneNo = string.Empty;
        int? zoneId = query.ZoneId;
        int? wardId = query.WardId;
        string? wardNo = null;

        if (!string.IsNullOrWhiteSpace(query.ZoneNo) && (!zoneId.HasValue || zoneId.Value <= 0))
        {
            var zoneByNo = await _context.ZoneMaster
                .AsNoTracking()
                .Where(z => z.IsActive && z.ZoneNo == query.ZoneNo.Trim())
                .Select(z => new { z.Id, ZoneName = z.Description ?? z.ZoneNo, z.ZoneNo })
                .FirstOrDefaultAsync(cancellationToken);

            if (zoneByNo == null)
                return CreateEmptyPendingAssessmentSnapshot(workflowStage.Id, workflowStage.StageName);

            zoneId = zoneByNo.Id;
            zoneName = zoneByNo.ZoneName;
            zoneNo = zoneByNo.ZoneNo;
        }

        if (!string.IsNullOrWhiteSpace(query.WardNo) && (!wardId.HasValue || wardId.Value <= 0))
        {
            var wardByNo = await _context.WardMaster
                .AsNoTracking()
                .Where(w => w.IsActive && w.WardNo == query.WardNo.Trim())
                .Select(w => new { w.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (wardByNo == null)
                return CreateEmptyPendingAssessmentSnapshot(workflowStage.Id, workflowStage.StageName);

            wardId = wardByNo.Id;
        }

        if (wardId is int selectedWardId && selectedWardId > 0)
        {
            var ward = await (
                from w in _context.WardMaster.AsNoTracking()
                join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
                where w.IsActive && z.IsActive && w.Id == selectedWardId
                select new
                {
                    WardId = w.Id,
                    WardNo = w.WardNo,
                    ZoneId = z.Id,
                    ZoneName = z.Description ?? z.ZoneNo,
                    z.ZoneNo
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (ward == null)
                return CreateEmptyPendingAssessmentSnapshot(workflowStage.Id, workflowStage.StageName);

            wardId = ward.WardId;
            wardNo = ward.WardNo;

            if (!zoneId.HasValue || zoneId.Value <= 0)
            {
                zoneId = ward.ZoneId;
                zoneName = ward.ZoneName;
                zoneNo = ward.ZoneNo;
            }
        }

        if (zoneId is int selectedZoneId && selectedZoneId > 0)
        {
            var zone = await _context.ZoneMaster
                .AsNoTracking()
                .Where(z => z.IsActive && z.Id == selectedZoneId)
                .Select(z => new { z.Id, ZoneName = z.Description ?? z.ZoneNo, z.ZoneNo })
                .FirstOrDefaultAsync(cancellationToken);

            if (zone == null)
                return CreateEmptyPendingAssessmentSnapshot(workflowStage.Id, workflowStage.StageName);

            zoneName = zone.ZoneName;
            zoneNo = zone.ZoneNo;
        }

        var propertyQuery =
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join pm in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals pm.Id
            join w in _context.WardMaster.AsNoTracking() on pm.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            where pwd.WorkflowStageId == workflowStage.Id
                  && pwd.IsActive
                  && pm.IsActive
                  && !pm.MarkedForDeletion
                  && w.IsActive
                  && z.IsActive
                  && !_context.PropertySignatureDetails.AsNoTracking()
                      .Any(sig => sig.IsActive && sig.PropertyId == pm.Id)
            select new SubGridPropertyFilterProjection
            {
                PropertyId = pm.Id,
                PropertyAssessmentStatusId = pm.PropertyAssessmentStatusId,
                WardId = pm.WardId,
                WardNo = w.WardNo,
                PropertyTypeId = pm.PropertyTypeId,
                ZoneId = w.ZoneId,
                ZoneNo = z.ZoneNo,
                IsPropertyOpenPlot = pm.OpenPlot == true,
                PropertyNo = pm.PropertyNo,
                OwnerName = pm.OwnerName,
                OccupierName = pm.OccupierName,
                MobileNo = pm.MobileNo,
                Address = pm.Address,
                UPICId = pm.UPICId
            };

        if (zoneId is int filterZoneId && filterZoneId > 0)
            propertyQuery = propertyQuery.Where(p => p.ZoneId == filterZoneId);

        if (!string.IsNullOrWhiteSpace(query.ZoneNo))
        {
            var filterZoneNo = query.ZoneNo.Trim();
            propertyQuery = propertyQuery.Where(p => p.ZoneNo == filterZoneNo);
        }

        if (wardId is int filterWardId && filterWardId > 0)
            propertyQuery = propertyQuery.Where(p => p.WardId == filterWardId);

        if (!string.IsNullOrWhiteSpace(query.WardNo))
        {
            var filterWardNo = query.WardNo.Trim();
            propertyQuery = propertyQuery.Where(p => p.WardNo == filterWardNo);
        }

        if (query.PropertyTypeId is int propertyTypeId && propertyTypeId > 0)
            propertyQuery = propertyQuery.Where(p => p.PropertyTypeId == propertyTypeId);

        if (query.SurveyTypeId is int surveyTypeId && surveyTypeId > 0)
            propertyQuery = propertyQuery.Where(p => p.PropertyAssessmentStatusId == surveyTypeId);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            propertyQuery = propertyQuery.Where(p =>
                (p.PropertyNo != null && p.PropertyNo.Contains(searchTerm))
                || (p.OwnerName != null && p.OwnerName.Contains(searchTerm))
                || (p.OccupierName != null && p.OccupierName.Contains(searchTerm))
                || (p.MobileNo != null && p.MobileNo.Contains(searchTerm))
                || (p.Address != null && p.Address.Contains(searchTerm))
                || (p.UPICId != null && p.UPICId.Contains(searchTerm)));
        }

        var propertyIdsQuery = propertyQuery.Select(p => p.PropertyId).Distinct();

        var totalCount = await propertyIdsQuery.CountAsync(cancellationToken);
        var pagePropertyIds = await propertyIdsQuery
            .OrderBy(id => id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return await FetchSubGridPropertyDetailsAsync(
            workflowStage.Id,
            workflowStage.StageName,
            zoneId.GetValueOrDefault(),
            zoneName,
            zoneNo,
            wardId,
            wardNo,
            totalCount,
            pagePropertyIds,
            cancellationToken);
    }

    private static SubGridDataProjection CreateEmptyPendingAssessmentSnapshot(int workflowStageId, string workflowStageName)
        => new()
        {
            WorkflowStageId = workflowStageId,
            WorkflowStageName = workflowStageName,
            TotalCount = 0
        };

    private async Task<SubGridDataProjection> FetchSubGridPropertyDetailsAsync(
        int workflowStageId,
        string workflowStageName,
        int zoneId,
        string zoneName,
        string zoneNo,
        int? wardId,
        string? wardNo,
        int totalCount,
        List<int> pagePropertyIds,
        CancellationToken cancellationToken)
    {
        if (totalCount == 0 || pagePropertyIds.Count == 0)
            return new SubGridDataProjection
            {
                WorkflowStageId = workflowStageId,
                WorkflowStageName = workflowStageName,
                ZoneId = zoneId,
                ZoneName = zoneName,
                ZoneNo = zoneNo,
                WardId = wardId,
                WardNo = wardNo,
                TotalCount = totalCount
            };

        var propertyEntities = await QuerySubGridPropertiesWithIncludes(pagePropertyIds)
            .ToListAsync(cancellationToken);

        var categoryIds = propertyEntities
            .Where(p => p.CategoryId.HasValue)
            .Select(p => p.CategoryId!.Value)
            .Distinct()
            .ToList();
        var propertyTypeIds = propertyEntities
            .Where(p => p.PropertyTypeId.HasValue)
            .Select(p => p.PropertyTypeId!.Value)
            .Distinct()
            .ToList();

        var categoriesById = await _context.PropertyCategoryMaster
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.PropertyCategoryName, cancellationToken);
        var propertyTypesById = await _context.PropertyTypeMasters
            .AsNoTracking()
            .Where(t => propertyTypeIds.Contains(t.Id))
            .ToDictionaryAsync(
                t => t.Id,
                t => new { Description = t.PropertyDescription, Type = t.Type ?? "" },
                cancellationToken);

        var wingNameRows = await (
            from society in _context.SocietyDetailsMast.AsNoTracking()
            join wing in _context.WingEntity.AsNoTracking().Where(w => w.IsActive)
                on society.WingId equals wing.Id into wingJoin
            from wing in wingJoin.DefaultIfEmpty()
            where society.PropertyId.HasValue
                  && pagePropertyIds.Contains(society.PropertyId.Value)
                  && society.IsActive
                  && !society.MarkedForDeletion
            select new
            {
                PropertyId = society.PropertyId.Value,
                WingName = !string.IsNullOrWhiteSpace(society.WingName)
                    ? society.WingName
                    : wing != null ? wing.WingNo : null
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.WingName))
            .ToListAsync(cancellationToken);

        var wingNamesByPropertyId = wingNameRows
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.WingName!).First());

        var properties = propertyEntities.Select(pm =>
        {
            categoriesById.TryGetValue(pm.CategoryId ?? 0, out var categoryName);
            propertyTypesById.TryGetValue(pm.PropertyTypeId ?? 0, out var propertyType);
            wingNamesByPropertyId.TryGetValue(pm.Id, out var wingName);

            return new SubGridPropertyProjection
            {
                Id = pm.Id,
                WardId = pm.WardId,
                WardNo = pm.Ward?.WardNo ?? "",
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                CategoryName = categoryName ?? "",
                TypeDescription = propertyType?.Description ?? "",
                TypeName = propertyType?.Type ?? "",
                OwnerName = pm.OwnerName ?? "",
                OccupierName = pm.OccupierName ?? "",
                MobileNo = pm.MobileNo ?? "",
                Address = pm.Address ?? "",
                FlatOrShopName = pm.FlatOrShopName ?? "",
                WingName = wingName ?? "",
                AssessmentStatusName = pm.PropertyAssessmentStatus?.StatusName ?? ""
            };
        }).ToList();

        if (!properties.Any())
            return new SubGridDataProjection
            {
                WorkflowStageId = workflowStageId,
                WorkflowStageName = workflowStageName,
                ZoneId = zoneId,
                ZoneName = zoneName,
                ZoneNo = zoneNo,
                TotalCount = totalCount
            };

        var detailCounts = await _context.PropertyDetails
            .AsNoTracking()
            .Where(pd => pagePropertyIds.Contains(pd.PropertyId) && pd.IsActive && !pd.MarkedForDeletion)
            .GroupBy(pd => pd.PropertyId)
            .Select(g => new SubGridCountProjection { PropertyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var documents = await (
            from pp in _context.PropertyPhotos.AsNoTracking()
            join db in _context.DocumentBindings.AsNoTracking() on pp.DocumentBindingId equals (int?)db.Id
            join doc in _context.Documents.AsNoTracking() on db.DocumentId equals doc.Id
            where pagePropertyIds.Contains(pp.PropertyId)
                  && pp.IsActive
                  && !pp.MarkedForDeletion
            select new SubGridDocumentProjection
            {
                PropertyId = pp.PropertyId,
                DocumentGuid = doc.DocumentGuid.ToString()
            }
        )
        .Distinct()
        .ToListAsync(cancellationToken);

        var planDocuments = await (
            from pm in _context.PropertyMast.AsNoTracking()
            join bp in _context.BuildingPlanType.AsNoTracking()
                on new { pm.WardId, PropertyNo = pm.PropertyNo ?? "" }
                equals new { bp.WardId, PropertyNo = bp.PropertyNo ?? "" }
            join db in _context.DocumentBindings.AsNoTracking() on bp.DocumentBindingId equals (int?)db.Id
            join doc in _context.Documents.AsNoTracking() on db.DocumentId equals doc.Id
            where pagePropertyIds.Contains(pm.Id)
                  && bp.IsActive
                  && !bp.MarkedForDeletion
            select new SubGridDocumentProjection
            {
                PropertyId = pm.Id,
                DocumentGuid = doc.DocumentGuid.ToString()
            }
        )
        .Distinct()
        .ToListAsync(cancellationToken);

        var propertyMapData = await (
            from pmap in _context.PropertyMapMasters.AsNoTracking()
            where pagePropertyIds.Contains(pmap.Id)
            select new SubGridPropertyMapProjection
            {
                PropertyIdNew = pmap.Id,
                PropertyIdOld = pmap.ParentPropertyMapId
            }
        )
        .ToListAsync(cancellationToken);

        var propertyMapDict = propertyMapData
            .Where(x => x.PropertyIdOld.HasValue)
            .GroupBy(x => x.PropertyIdNew)
            .ToDictionary(g => g.Key, g => g.First().PropertyIdOld!.Value);

        var newPropertyDetails = await (
            from pm in _context.PropertyMast.AsNoTracking()
            join pd in _context.PropertyDetails.AsNoTracking() on pm.Id equals pd.PropertyId
            join tou in _context.TypeOfUse.AsNoTracking() on pd.TypeOfUseId equals tou.Id into touJoin
            from tou in touJoin.DefaultIfEmpty()
            where pagePropertyIds.Contains(pm.Id) && pd.IsActive && !pd.MarkedForDeletion
            select new SubGridNewPropertyDetailProjection
            {
                Id = pm.Id,
                Area = (decimal)(pd.BuiltupAreaSqMeter ?? 0),
                Use = tou.Description ?? ""
            }
        )
        .Distinct()
        .ToListAsync(cancellationToken);

        var newRvData = await (
            from tm in _context.TransMast.AsNoTracking()
            where pagePropertyIds.Contains(tm.PropertyId)
                  && tm.IsActive
                  && !tm.MarkedForDeletion
                  && tm.CalculationType == CalculationTypeRV
            select new SubGridTaxValueProjection
            {
                PropertyId = tm.PropertyId,
                Amount = tm.CalculationValue
            }
        )
        .ToListAsync(cancellationToken);

        var newCTaxData = await (
            from tm in _context.TransMast.AsNoTracking()
            join tax in _context.TaxMaster.AsNoTracking() on tm.TaxId equals tax.Id
            where pagePropertyIds.Contains(tm.PropertyId)
                  && tm.IsActive
                  && !tm.MarkedForDeletion
                  && tm.CalculationType == CalculationTypeRV
                  && tax.IsActive
                  && tax.TaxCode == TaxTotalCode
                  && tax.TaxName == TaxTotalName
            select new SubGridTaxValueProjection
            {
                PropertyId = tm.PropertyId,
                Amount = tm.TaxAmount
            }
        )
        .ToListAsync(cancellationToken);

        var newRTaxData = await (
            from tpd in _context.TaxPendingDetails.AsNoTracking()
            where pagePropertyIds.Contains(tpd.PropertyId)
            select new SubGridTaxValueProjection
            {
                PropertyId = tpd.PropertyId,
                Amount = tpd.PendingAmount ?? 0
            }
        )
        .ToListAsync(cancellationToken);

        var applyTaxesPropertyIds = await _context.ApplyTaxesMaster
            .AsNoTracking()
            .Where(at => pagePropertyIds.Contains(at.PropertyId) && at.IsActive && !at.MarkedForDeletion)
            .Select(at => at.PropertyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var assessmentDetails = await _context.PropertyMastDetails
            .AsNoTracking()
            .Where(pa => pagePropertyIds.Contains(pa.PropertyId) && pa.IsActive && !pa.MarkedForDeletion)
            .Select(pa => new SubGridAssessmentDetailProjection
            {
                PropertyId = pa.PropertyId,
                PartOCDate = pa.PartOCDate,
                ApplyTaxesFrom = pa.ApplyTaxesFrom
            })
            .ToListAsync(cancellationToken);

        var oldPropertyIds = propertyMapDict.Values.Distinct().ToList();

        var oldPropertyDetails = await (
            from pdo in _context.PropertyDetailsOld.AsNoTracking()
            join pmo in _context.PropertyMastOld.AsNoTracking() on pdo.PropertyMastOldId equals pmo.Id
            join tou in _context.TypeOfUse.AsNoTracking() on pdo.OldTypeOfUseId equals tou.Id into touJoin
            from tou in touJoin.DefaultIfEmpty()
            where oldPropertyIds.Contains(pmo.Id) && !pdo.MarkedForDeletion
            select new SubGridOldPropertyDetailProjection
            {
                Id = pmo.Id,
                Area = (decimal)(pdo.OldBuiltupAreaSqMeter ?? 0),
                Use = tou.Description ?? "",
                OldRV = pmo.OldRV ?? 0
            }
        )
        .Distinct()
        .ToListAsync(cancellationToken);

        var oldCTaxData = await (
            from tmo in _context.TransMastOld.AsNoTracking()
            join tax in _context.TaxMaster.AsNoTracking() on tmo.TaxId equals tax.Id
            where oldPropertyIds.Contains(tmo.PropertyMastOldId)
                  && tmo.IsActive
                  && !tmo.MarkedForDeletion
                  && tmo.CalculationType == CalculationTypeRV
                  && tax.IsActive
                  && tax.TaxCode == TaxTotalCode
                  && tax.TaxName == TaxTotalName
            select new SubGridTaxValueProjection
            {
                PropertyId = tmo.PropertyMastOldId,
                Amount = tmo.TaxAmount
            }
        )
        .ToListAsync(cancellationToken);

        var oldRTaxData = await (
            from tpdr in _context.TaxPendingDetailsRetro.AsNoTracking()
            where oldPropertyIds.Contains(tpdr.PropertyId)
            select new SubGridTaxValueProjection
            {
                PropertyId = tpdr.PropertyId,
                Amount = tpdr.PendingAmount ?? 0
            }
        )
        .ToListAsync(cancellationToken);

        return new SubGridDataProjection
        {
            WorkflowStageId = workflowStageId,
            WorkflowStageName = workflowStageName,
            ZoneId = zoneId,
            ZoneName = zoneName,
            ZoneNo = zoneNo,
            WardId = wardId,
            WardNo = wardNo,
            TotalCount = totalCount,
            Properties = properties,
            DetailCounts = detailCounts,
            Documents = documents,
            PlanDocuments = planDocuments,
            PropertyMaps = propertyMapData,
            NewDetails = newPropertyDetails,
            OldDetails = oldPropertyDetails,
            NewRvValues = newRvData,
            NewCurrentTaxes = newCTaxData,
            NewPendingTaxes = newRTaxData,
            OldCurrentTaxes = oldCTaxData,
            OldPendingTaxes = oldRTaxData,
            ApplyTaxesPropertyIds = applyTaxesPropertyIds,
            AssessmentDetails = assessmentDetails
        };
    }

    private IQueryable<PropertyEntity> QuerySubGridPropertiesWithIncludes(List<int> propertyIds)
        => _context.PropertyMast
            .AsNoTracking()
            .Include(p => p.Ward)
            .Include(p => p.PropertyAssessmentStatus)
            .Where(p => propertyIds.Contains(p.Id));

    #endregion

    #region Internal Projection Classes

    /// <summary>
    /// Internal projection for filtering sub-grid properties.
    /// </summary>
    private sealed class SubGridPropertyFilterProjection
    {
        public int PropertyId { get; set; }
        public int? PropertyAssessmentStatusId { get; set; }
        public int WardId { get; set; }
        public string? WardNo { get; set; }
        public int? CategoryId { get; set; }
        public int? PropertyTypeId { get; set; }
        public int ZoneId { get; set; }
        public string? ZoneNo { get; set; }
        public string? ZoneName { get; set; }
        public bool IsPropertyOpenPlot { get; set; }
        public string? PropertyNo { get; set; }
        public string? PartitionNo { get; set; }
        public string? OwnerName { get; set; }
        public string? OccupierName { get; set; }
        public string? MobileNo { get; set; }
        public string? AlternateMobileNo { get; set; }
        public string? OccupierMobileNo { get; set; }
        public string? Address { get; set; }
        public string? FlatOrShopName { get; set; }
        public string? UPICId { get; set; }
        public int? PropertyMastOldId { get; set; }
    }

    #endregion
}
