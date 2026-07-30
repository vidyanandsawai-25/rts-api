using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Geo-Sequencing stage database reads.
/// </summary>
public class GeoSequencingStageRepository : WorkflowStageBaseRepository, IGeoSequencingStageRepository
{
    private static readonly string[] AssessmentStatusNames =
    {
        "ASSESSED", "UNASSESSED", "PARTIALLY_ASSESSED", "UNDER_UNASSESSED"
    };

    public GeoSequencingStageRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Checks whether the workflow stage exists and is active.
    /// </summary>
    public async Task<bool> StageExistsAsync(int workflowStageId, CancellationToken cancellationToken = default)
        => await _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .AnyAsync(s => s.IsActive && s.Id == workflowStageId, cancellationToken);

    /// <summary>
    /// Reads active zones, optionally filtered by zone id.
    /// </summary>
    public Task<List<(int ZoneId, string ZoneName, string ZoneNo)>> ReadZonesAsync(
        int? zoneId,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = zoneId.HasValue ? new PropertySearchRequestDto { ZoneId = zoneId } : null;
        return GetZonesAsync(searchRequest, cancellationToken);
    }

    /// <summary>
    /// Reads one active zone by id.
    /// </summary>
    public Task<(int ZoneId, string ZoneName, string ZoneNo)> ReadZoneAsync(int zoneId, CancellationToken cancellationToken = default)
        => GetZoneAsync(zoneId, cancellationToken);

    /// <summary>
    /// Reads active wards for one zone.
    /// </summary>
    public Task<List<(int WardId, string WardNo)>> ReadWardsInZoneAsync(int zoneId, CancellationToken cancellationToken = default)
        => GetWardsInZoneAsync(zoneId, cancellationToken);

    /// <summary>
    /// Reads workflow stage properties for selected zones.
    /// </summary>
    public async Task<List<GeoSequencingStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        if (workflowStageId == 0 || !zoneIds.Any())
            return new List<GeoSequencingStagePropertyProjection>();

        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        return await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
            from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
            join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into propertyTypeJoin
            from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()
            where pwd.WorkflowStageId == workflowStageId
                  && w.IsActive
                  && zoneIds.Contains(w.ZoneId)
            select new GeoSequencingStagePropertyProjection
            {
                PropertyId = p.Id,
                WardId = p.WardId,
                ZoneId = w.ZoneId,
                PartitionNo = p.PartitionNo,
                PropertyTypeCode = pt == null ? null : pt.Type,
                PropertyCategoryName = pc == null ? null : pc.PropertyCategoryName,
                AssessmentStatusId = p.PropertyAssessmentStatusId
            })
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads registered property counts grouped by zone.
    /// </summary>
    public async Task<Dictionary<int, int>> ReadRegisteredCountsByZoneAsync(
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        if (!zoneIds.Any())
            return new Dictionary<int, int>();

        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking()
                .Where(p => p.IsActive
                         && !p.MarkedForDeletion
                         && p.PropertyNo != null
                         && p.PropertyNo != ""),
            searchRequest);

        return await (
            from p in properties
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            where w.IsActive
                  && zoneIds.Contains(w.ZoneId)
            group p by w.ZoneId into g
            select new { ZoneId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ZoneId, x => x.Count, cancellationToken);
    }

    /// <summary>
    /// Reads registered property counts grouped by ward.
    /// </summary>
    public async Task<Dictionary<int, int>> ReadRegisteredCountsByWardAsync(
        List<int> wardIds,
        CancellationToken cancellationToken = default)
    {
        if (!wardIds.Any())
            return new Dictionary<int, int>();

        return await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive
                     && !p.MarkedForDeletion
                     && p.PropertyNo != null
                     && p.PropertyNo != ""
                     && wardIds.Contains(p.WardId))
            .GroupBy(p => p.WardId)
            .Select(g => new { WardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.WardId, x => x.Count, cancellationToken);
    }

    /// <summary>
    /// Reads property use rows for selected stage properties in zones.
    /// </summary>
    public async Task<List<GeoSequencingPropertyUseProjection>> ReadPropertyUsesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        if (workflowStageId == 0 || !zoneIds.Any())
            return new List<GeoSequencingPropertyUseProjection>();

        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        return await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join pd in _context.PropertyDetails.AsNoTracking() on p.Id equals pd.PropertyId
            join tou in _context.TypeOfUse.AsNoTracking() on pd.TypeOfUseId equals tou.Id
            where pwd.WorkflowStageId == workflowStageId
                  && w.IsActive
                  && zoneIds.Contains(w.ZoneId)
                  && pd.IsActive
                  && !pd.MarkedForDeletion
                  && tou.IsActive
            select new GeoSequencingPropertyUseProjection
            {
                PropertyId = p.Id,
                Type = tou.Type,
                TypeOfUseCode = tou.TypeOfUseCode
            })
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads assessment status ids by status name.
    /// </summary>
    public async Task<Dictionary<string, int>> ReadAssessmentStatusIdsByNameAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive && AssessmentStatusNames.Contains(s.StatusName.ToUpper()))
            .Select(s => new { s.Id, StatusName = s.StatusName.ToUpper() })
            .ToDictionaryAsync(s => s.StatusName, s => s.Id, cancellationToken);
    }
}
