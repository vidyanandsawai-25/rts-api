using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Internal Survey stage database reads.
/// </summary>
public class InternalSurveyStageRepository : WorkflowStageBaseRepository, IInternalSurveyStageRepository
{
    public InternalSurveyStageRepository(ApplicationDbContext context) : base(context)
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
    public Task<List<(int ZoneId, string ZoneName)>> ReadZonesAsync(
        int? zoneId,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = zoneId.HasValue ? new PropertySearchRequestDto { ZoneId = zoneId } : null;
        return GetZonesAsync(searchRequest, cancellationToken);
    }

    /// <summary>
    /// Reads one active zone by id.
    /// </summary>
    public Task<(int ZoneId, string ZoneName)> ReadZoneAsync(int zoneId, CancellationToken cancellationToken = default)
        => GetZoneAsync(zoneId, cancellationToken);

    /// <summary>
    /// Reads active wards for one zone.
    /// </summary>
    public Task<List<(int WardId, string WardNo)>> ReadWardsInZoneAsync(int zoneId, CancellationToken cancellationToken = default)
        => GetWardsInZoneAsync(zoneId, cancellationToken);

    /// <summary>
    /// Reads the active Geo-Sequencing stage id.
    /// </summary>
    public Task<int> ReadGeoSequencingStageIdAsync(CancellationToken cancellationToken = default)
        => GetStageIdByNameAsync(GeoSequencingStageName, cancellationToken);

    /// <summary>
    /// Reads assessed and unassessed assessment status ids.
    /// </summary>
    public Task<(int AssessedId, int UnassessedId)> ReadAssessedAndUnassessedStatusIdsAsync(
        CancellationToken cancellationToken = default)
        => GetAssessedAndUnassessedStatusIdsAsync(cancellationToken);

    /// <summary>
    /// Reads the Internal Survey property photo type id.
    /// </summary>
    public Task<int> ReadPropertyPhotoTypeIdAsync(CancellationToken cancellationToken = default)
        => GetPhotoTypeIdAsync(PropertyPhotoTypeCode, cancellationToken);

    /// <summary>
    /// Reads stage properties for selected zones.
    /// </summary>
    public async Task<List<InternalSurveyStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        if (workflowStageId == 0 || !zoneIds.Any())
            return new List<InternalSurveyStagePropertyProjection>();

        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        var query =
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into propertyTypeJoin
            from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()
            where pwd.WorkflowStageId == workflowStageId
                  && w.IsActive
                  && zoneIds.Contains(w.ZoneId)
            select new
            {
                Property = p,
                Ward = w,
                PropertyType = pt
            };

        if (requirePropertyNo)
        {
            query = query.Where(x => x.Property.PropertyNo != null && x.Property.PropertyNo != "");
        }

        return await query
            .Select(x => new InternalSurveyStagePropertyProjection
            {
                PropertyId = x.Property.Id,
                WardId = x.Property.WardId,
                ZoneId = x.Ward.ZoneId,
                PartitionNo = x.Property.PartitionNo,
                PropertyTypeCode = x.PropertyType == null ? null : x.PropertyType.Type,
                AssessmentStatusId = x.Property.PropertyAssessmentStatusId
            })
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property use rows for selected stage properties in zones.
    /// </summary>
    public async Task<List<InternalSurveyPropertyUseSourceProjection>> ReadPropertyUsesForStageInZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        if (workflowStageId == 0 || !zoneIds.Any())
            return new List<InternalSurveyPropertyUseSourceProjection>();

        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        var query =
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
            select new
            {
                Property = p,
                Ward = w,
                Type = tou.Type,
                TypeOfUseCode = tou.TypeOfUseCode
            };

        if (requirePropertyNo)
        {
            query = query.Where(x => x.Property.PropertyNo != null && x.Property.PropertyNo != "");
        }

        return await query
            .Select(x => new InternalSurveyPropertyUseSourceProjection
            {
                PropertyId = x.Property.Id,
                WardId = x.Property.WardId,
                ZoneId = x.Ward.ZoneId,
                Type = x.Type,
                TypeOfUseCode = x.TypeOfUseCode
            })
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property photo counts grouped by zone.
    /// </summary>
    public async Task<List<InternalSurveyPhotoCountProjection>> ReadPhotoCountsByZoneAsync(
        int workflowStageId,
        List<int> zoneIds,
        int propertyPhotoTypeId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        if (workflowStageId == 0 || propertyPhotoTypeId == 0 || !zoneIds.Any())
            return new List<InternalSurveyPhotoCountProjection>();

        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        var stageProperties =
            (from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
             join p in properties on pwd.PropertyId equals p.Id
             join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
             where pwd.WorkflowStageId == workflowStageId
                   && p.PropertyNo != null
                   && p.PropertyNo != ""
                   && w.IsActive
                   && zoneIds.Contains(w.ZoneId)
             select new { PropertyId = p.Id, w.ZoneId }).Distinct();

        return await (
            from sp in stageProperties
            join pp in _context.PropertyPhotos.AsNoTracking() on sp.PropertyId equals pp.PropertyId
            where pp.PhotoTypeId == propertyPhotoTypeId
                  && pp.IsActive
                  && !pp.MarkedForDeletion
            group pp by sp.ZoneId into g
            select new InternalSurveyPhotoCountProjection
            {
                ZoneId = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property photo counts grouped by ward.
    /// </summary>
    public async Task<List<InternalSurveyPhotoCountProjection>> ReadPhotoCountsByWardAsync(
        int workflowStageId,
        List<int> wardIds,
        int propertyPhotoTypeId,
        CancellationToken cancellationToken = default)
    {
        if (workflowStageId == 0 || propertyPhotoTypeId == 0 || !wardIds.Any())
            return new List<InternalSurveyPhotoCountProjection>();

        var stageProperties =
            (from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
             join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
             where pwd.WorkflowStageId == workflowStageId
                   && p.IsActive
                   && !p.MarkedForDeletion
                   && p.PropertyNo != null
                   && p.PropertyNo != ""
                   && wardIds.Contains(p.WardId)
             select new { PropertyId = p.Id, p.WardId }).Distinct();

        return await (
            from sp in stageProperties
            join pp in _context.PropertyPhotos.AsNoTracking() on sp.PropertyId equals pp.PropertyId
            where pp.PhotoTypeId == propertyPhotoTypeId
                  && pp.IsActive
                  && !pp.MarkedForDeletion
            group pp by sp.WardId into g
            select new InternalSurveyPhotoCountProjection
            {
                WardId = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);
    }
}
