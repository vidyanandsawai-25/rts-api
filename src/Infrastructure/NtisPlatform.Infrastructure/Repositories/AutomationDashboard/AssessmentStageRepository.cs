using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Assessment dashboard database operations only.
/// </summary>
public class AssessmentStageRepository : WorkflowStageBaseRepository, IAssessmentStageRepository
{
    private const int SqlServerInClauseBatchSize = 1800;
    private const string AssessmentTypeAssessed = "Assessed";
    private const string AssessmentTypeUnassessed = "Unassessed";
    private const string TaxTotalCode = "TaxTotal";
    private const string TaxTotalName = "TaxTotal";

    public AssessmentStageRepository(ApplicationDbContext context, IMapper mapper) : base(context)
    {
    }

    // Checks whether the workflow stage exists and is active.
    public Task<bool> AssessmentWorkflowStageExistsAsync(int workflowStageId, CancellationToken cancellationToken = default)
        => _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .AnyAsync(s => s.IsActive && s.Id == workflowStageId, cancellationToken);

    // Reads active Assessed and Unassessed status ids from the master table.
    public async Task<Dictionary<string, int>> GetAssessmentStatusIdsAsync(CancellationToken cancellationToken = default)
    {
        var statusNames = new[] { AssessmentTypeAssessed.ToUpperInvariant(), AssessmentTypeUnassessed.ToUpperInvariant() };
        var statuses = await _context.PropertyAssessmentStatuses.AsNoTracking()
            .Where(s => s.IsActive && statusNames.Contains(s.StatusName.ToUpper()))
            .Select(s => new { StatusName = s.StatusName.ToUpper(), s.Id })
            .ToListAsync(cancellationToken);

        return statuses.ToDictionary(
            s => string.Equals(s.StatusName, AssessmentTypeAssessed.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase)
                ? AssessmentTypeAssessed
                : AssessmentTypeUnassessed,
            s => s.Id);
    }

    // Reads active workflow properties with zone, status, and renter flag.
    public async Task<List<AssessmentStagePropertyProjection>> GetStagePropertiesAsync(
        int workflowStageId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        return await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            where pwd.WorkflowStageId == workflowStageId && w.IsActive && z.IsActive
            select new AssessmentStagePropertyProjection
            {
                PropertyId = p.Id,
                PartitionNo = p.PartitionNo,
                ZoneId = z.Id,
                ZoneName = z.Description ?? z.ZoneNo,
                ZoneNo = z.ZoneNo,
                AssessmentStatusId = p.PropertyAssessmentStatusId,
                IsRented = (
                    from pd in _context.PropertyDetails.AsNoTracking()
                    join rm in _context.RenterMast.AsNoTracking() on pd.Id equals rm.PropertyDetailsId
                    where pd.PropertyId == p.Id && pd.IsActive && !pd.MarkedForDeletion
                          && rm.IsActive && !rm.MarkedForDeletion && rm.TaxLiability != null
                          && rm.TaxLiability.Trim().ToUpper() == "RENTER"
                    select rm.Id
                ).Any()
            }).Distinct().ToListAsync(cancellationToken);
    }

    // Reads assessed properties with old mapped values for classification comparisons.
    public async Task<List<AssessedClassificationPropertyProjection>> GetAssessedClassificationPropertiesAsync(
        int workflowStageId,
        int assessedStatusId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        return await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            join pmo in _context.PropertyMastOld.AsNoTracking() on p.PropertyMastOldId equals pmo.Id into oldPropertyJoin
            from pmo in oldPropertyJoin.DefaultIfEmpty()
            where pwd.WorkflowStageId == workflowStageId && w.IsActive && z.IsActive
                  && p.PropertyAssessmentStatusId == assessedStatusId
                  && (pmo == null || (pmo.IsActive && !pmo.MarkedForDeletion))
            select new AssessedClassificationPropertyProjection
            {
                PropertyId = p.Id,
                PropertyMastOldId = p.PropertyMastOldId,
                PartitionNo = p.PartitionNo,
                ZoneId = z.Id,
                ZoneName = z.Description ?? z.ZoneNo,
                ZoneNo = z.ZoneNo,
                OldConstructionArea = pmo != null ? pmo.OldConstructionArea : null,
                OldUseType = pmo != null ? pmo.OldUseType : null,
                OldRV = pmo != null ? pmo.OldRV : null
            }).Distinct().ToListAsync(cancellationToken);
    }

    // Reads unassessed properties with zone and open-plot data.
    public async Task<List<UnassessedPropertyProjection>> GetUnassessedPropertiesAsync(
        int workflowStageId,
        int unassessedStatusId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null)
    {
        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        return await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            where pwd.WorkflowStageId == workflowStageId
                  && w.IsActive && z.IsActive && p.PropertyAssessmentStatusId == unassessedStatusId
            select new UnassessedPropertyProjection
            {
                PropertyId = p.Id,
                PropertyTypeId = p.PropertyTypeId,
                PartitionNo = p.PartitionNo,
                ZoneId = z.Id,
                ZoneName = z.Description ?? z.ZoneNo,
                ZoneNo = z.ZoneNo,
                IsOpenPlot = p.OpenPlot == true
            }).Distinct().ToListAsync(cancellationToken);
    }

    // Reads workflow properties and classifies them as Renter when any detail has Renter tax liability.
    public async Task<List<RentedClassifiedPropertyProjection>> GetRentedPropertiesAsync(int workflowStageId, CancellationToken cancellationToken = default)
    {
        var stageProperties = await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            where pwd.WorkflowStageId == workflowStageId && p.IsActive && !p.MarkedForDeletion && w.IsActive && z.IsActive
            select new RentedClassifiedPropertyProjection
            {
                PropertyId = p.Id,
                PartitionNo = p.PartitionNo,
                ZoneId = z.Id,
                ZoneName = z.Description ?? z.ZoneNo,
                ZoneNo = z.ZoneNo
            }).Distinct().ToListAsync(cancellationToken);

        if (!stageProperties.Any())
            return stageProperties;

        var propertyIds = stageProperties.Select(p => p.PropertyId).ToList();
        var renterPropertyIds = new HashSet<int>();
        foreach (var batch in BatchIds(propertyIds))
        {
            var renterIds = await (
                from pd in _context.PropertyDetails.AsNoTracking()
                join rm in _context.RenterMast.AsNoTracking() on pd.Id equals rm.PropertyDetailsId
                where batch.Contains(pd.PropertyId)
                      && pd.IsActive
                      && !pd.MarkedForDeletion
                      && rm.IsActive
                      && !rm.MarkedForDeletion
                      && rm.TaxLiability != null
                      && rm.TaxLiability.Trim().ToUpper() == "RENTER"
                select pd.PropertyId
            ).Distinct().ToListAsync(cancellationToken);

            foreach (var propertyId in renterIds)
                renterPropertyIds.Add(propertyId);
        }

        foreach (var property in stageProperties)
        {
            property.ClassificationType = renterPropertyIds.Contains(property.PropertyId) ? "Renter" : "Owner";
        }

        return stageProperties;
    }

    // Reads Rented tab properties with renter flag and demand values using set-based grouped queries.
    public async Task<List<RentedPropertyDemandProjection>> GetRentedPropertyDemandDataAsync(
        int workflowStageId, CancellationToken cancellationToken = default,PropertySearchRequestDto? searchRequest = null)
    {
        var properties = ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast.AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion),
            searchRequest);

        var stagePropertyIds = (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            where pwd.WorkflowStageId == workflowStageId
                  && w.IsActive
                  && z.IsActive
            select p.Id).Distinct();

        var stageProperties = await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in properties on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id
            where pwd.WorkflowStageId == workflowStageId
                  && w.IsActive
                  && z.IsActive
            select new
            {
                PropertyId = p.Id,
                p.PartitionNo,
                ZoneId = z.Id,
                ZoneName = z.Description ?? z.ZoneNo,
                ZoneNo = z.ZoneNo
            }).Distinct().ToListAsync(cancellationToken);

        if (!stageProperties.Any())
            return new List<RentedPropertyDemandProjection>();

        var totalTaxIds = await _context.TaxMaster.AsNoTracking()
            .Where(tax => tax.IsActive && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName)
            .Select(tax => tax.Id)
            .ToListAsync(cancellationToken);

        var renterPropertyIds = await (
            from pd in _context.PropertyDetails.AsNoTracking()
            join rm in _context.RenterMast.AsNoTracking() on pd.Id equals rm.PropertyDetailsId
            where stagePropertyIds.Contains(pd.PropertyId)
                  && pd.IsActive&& !pd.MarkedForDeletion && rm.IsActive && !rm.MarkedForDeletion
                  && rm.TaxLiability != null && rm.TaxLiability.Trim().ToUpper() == "RENTER"
            select pd.PropertyId).Distinct().ToListAsync(cancellationToken);

        var currentDemandByProperty = totalTaxIds.Any()
            ? await _context.TransMast.AsNoTracking()
                .Where(tm => stagePropertyIds.Contains(tm.PropertyId)
                             && tm.IsActive
                             && !tm.MarkedForDeletion
                             && totalTaxIds.Contains(tm.TaxId))
                .GroupBy(tm => tm.PropertyId)
                .Select(g => new { PropertyId = g.Key, Demand = g.Sum(x => x.TaxAmount) })
                .ToDictionaryAsync(x => x.PropertyId, x => x.Demand, cancellationToken)
            : new Dictionary<int, decimal>();

        var retroDemandByProperty = totalTaxIds.Any()
            ? await _context.TaxPendingDetailsRetro.AsNoTracking()
                .Where(retro => stagePropertyIds.Contains(retro.PropertyId)
                                && retro.IsActive
                                && !retro.MarkedForDeletion
                                && totalTaxIds.Contains(retro.TaxId))
                .GroupBy(retro => retro.PropertyId)
                .Select(g => new { PropertyId = g.Key, Demand = g.Sum(x => x.PendingAmount ?? 0m) })
                .ToDictionaryAsync(x => x.PropertyId, x => x.Demand, cancellationToken)
            : new Dictionary<int, decimal>();

        var oldDemandByProperty = totalTaxIds.Any()
            ? await (
                from propertyMap in _context.PropertyMapMasters.AsNoTracking()
                join tmo in _context.TransMastOld.AsNoTracking() on propertyMap.ParentPropertyMapId equals tmo.PropertyMastOldId
                where stagePropertyIds.Contains(propertyMap.Id)
                      && propertyMap.IsActive
                      && propertyMap.ParentPropertyMapId.HasValue
                      && tmo.IsActive
                      && !tmo.MarkedForDeletion
                      && totalTaxIds.Contains(tmo.TaxId)
                group tmo by propertyMap.Id into g
                select new { PropertyId = g.Key, Demand = g.Sum(x => x.TaxAmount) })
                .ToDictionaryAsync(x => x.PropertyId, x => x.Demand, cancellationToken)
            : new Dictionary<int, decimal>();

        var renterPropertyIdSet = renterPropertyIds.ToHashSet();
        return stageProperties.Select(property => new RentedPropertyDemandProjection
        {
            PropertyId = property.PropertyId,
            PartitionNo = property.PartitionNo,
            ZoneId = property.ZoneId,
            ZoneName = property.ZoneName,
            ZoneNo = property.ZoneNo,
            HasRenterTaxLiability = renterPropertyIdSet.Contains(property.PropertyId),
            OldDemand = oldDemandByProperty.GetValueOrDefault(property.PropertyId),
            CurrentDemand = currentDemandByProperty.GetValueOrDefault(property.PropertyId),
            RetroDemand = retroDemandByProperty.GetValueOrDefault(property.PropertyId)
        })
            .ToList();
    }

    // Reads current property details and TypeOfUse values used by classifications.
    public async Task<List<AssessmentPropertyUseDetailProjection>> GetPropertyUseDetailsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Distinct().ToList();
        if (!ids.Any())
            return new List<AssessmentPropertyUseDetailProjection>();

        var rows = new List<AssessmentPropertyUseDetailProjection>();
        foreach (var batch in BatchIds(ids))
        {
            rows.AddRange(await (
                from pd in _context.PropertyDetails.AsNoTracking()
                join tou in _context.TypeOfUse.AsNoTracking() on pd.TypeOfUseId equals tou.Id into typeOfUseJoin
                from tou in typeOfUseJoin.DefaultIfEmpty()
                where batch.Contains(pd.PropertyId) && pd.IsActive && !pd.MarkedForDeletion
                select new AssessmentPropertyUseDetailProjection
                {
                    PropertyId = pd.PropertyId,
                    CarpetArea = pd.CarpetAreaSqMeter ?? 0d,
                    IsOpenPlot = pd.IsOpenPlot == true,
                    Type = tou != null ? tou.Type : null,
                    TypeOfUseCode = tou != null ? tou.TypeOfUseCode : null,
                    TypeOfUseDescription = tou != null ? tou.Description : null
                }).ToListAsync(cancellationToken));
        }

        return rows;
    }

    // Reads mixed-use property ids using the same mixed type codes as the common breakdown logic.
    public async Task<List<int>> GetMixedPropertyIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Distinct().ToList();
        if (!ids.Any())
            return new List<int>();

        var mixedTypes = new[] { "R-C", "C-R", "C-I", "I-C", "I-R", "R-I" };
        var mixedIds = new List<int>();
        foreach (var batch in BatchIds(ids))
        {
            mixedIds.AddRange(await (
                from p in _context.PropertyMast.AsNoTracking()
                join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id
                where batch.Contains(p.Id) && pt.IsActive && pt.Type != null && mixedTypes.Contains(pt.Type.ToUpper())
                select p.Id
            ).Distinct().ToListAsync(cancellationToken));
        }

        return mixedIds.Distinct().ToList();
    }

    // Reads current RV value per property from TransMast.
    public async Task<Dictionary<int, decimal>> GetCurrentRvByPropertyAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<int, decimal>();

        var rows = new List<(int PropertyId, decimal RV)>();
        foreach (var batch in BatchIds(ids))
        {
            var batchRows = await _context.TransMast.AsNoTracking()
                .Where(tm => batch.Contains(tm.PropertyId) && tm.IsActive && !tm.MarkedForDeletion && tm.CalculationType == "RV")
                .GroupBy(tm => tm.PropertyId)
                .Select(g => new { PropertyId = g.Key, RV = g.Max(x => x.CalculationValue) })
                .ToListAsync(cancellationToken);

            rows.AddRange(batchRows.Select(x => (x.PropertyId, x.RV)));
        }

        return rows.GroupBy(x => x.PropertyId).ToDictionary(g => g.Key, g => g.Max(x => x.RV));
    }

    // Calculates old demand by zone using mapped old property ids.
    public async Task<Dictionary<int, decimal>> GetOldDemandByZoneAsync(IEnumerable<AssessmentStagePropertyProjection> properties, CancellationToken cancellationToken = default)
    {
        var propertyZoneMap = properties.Select(p => (p.PropertyId, p.ZoneId)).Distinct().ToList();
        if (!propertyZoneMap.Any())
            return new Dictionary<int, decimal>();

        var propertyIds = propertyZoneMap.Select(p => p.PropertyId).ToList();
        var mappedOldProperties = new List<(int PropertyId, int PropertyMastOldId)>();
        foreach (var batch in BatchIds(propertyIds))
        {
            var batchMappings = await (
                from propertyMap in _context.PropertyMapMasters.AsNoTracking()
                where batch.Contains(propertyMap.Id) && propertyMap.IsActive && propertyMap.ParentPropertyMapId.HasValue
                select new { PropertyId = propertyMap.Id, PropertyMastOldId = propertyMap.ParentPropertyMapId!.Value }
            ).Distinct().ToListAsync(cancellationToken);

            mappedOldProperties.AddRange(batchMappings.Select(x => (x.PropertyId, x.PropertyMastOldId)));
        }

        var oldDemandByOldProperty = await GetOldDemandByOldPropertyAsync(mappedOldProperties.Select(x => x.PropertyMastOldId), cancellationToken);
        return mappedOldProperties
            .Join(propertyZoneMap, m => m.PropertyId, p => p.PropertyId, (m, p) => new { p.ZoneId, m.PropertyMastOldId })
            .GroupBy(x => x.ZoneId)
            .ToDictionary(g => g.Key, g => g.Sum(x => oldDemandByOldProperty.GetValueOrDefault(x.PropertyMastOldId)));
    }

    // Calculates current demand by zone from TransMast.
    public async Task<Dictionary<int, decimal>> GetCurrentDemandByZoneAsync(IEnumerable<AssessmentStagePropertyProjection> properties, CancellationToken cancellationToken = default)
    {
        var propertyZoneMap = properties.Select(p => (p.PropertyId, p.ZoneId)).Distinct().ToList();
        var demandByProperty = await GetCurrentDemandByPropertyAsync(propertyZoneMap.Select(p => p.PropertyId), cancellationToken);
        return SumDemandByZone(propertyZoneMap, demandByProperty);
    }
    // Calculates retro demand by zone from TaxPendingDetailsRetro.
    public async Task<Dictionary<int, decimal>> GetRetroDemandByZoneAsync(IEnumerable<AssessmentStagePropertyProjection> properties, CancellationToken cancellationToken = default)
    {
        var propertyZoneMap = properties.Select(p => (p.PropertyId, p.ZoneId)).Distinct().ToList();
        var demandByProperty = await GetRetroDemandByPropertyAsync(propertyZoneMap.Select(p => p.PropertyId), cancellationToken);
        return SumDemandByZone(propertyZoneMap, demandByProperty);
    }

    // Calculates old demand per new property using old property mappings.
    public async Task<Dictionary<int, decimal>> GetOldDemandByPropertyAsync(IEnumerable<AssessedClassifiedPropertyProjection> properties, CancellationToken cancellationToken = default)
    {
        var mappings = properties
            .Where(p => p.PropertyMastOldId.HasValue)
            .Select(p => new { p.PropertyId, PropertyMastOldId = p.PropertyMastOldId!.Value })
            .Distinct()
            .ToList();

        var oldDemandByOldProperty = await GetOldDemandByOldPropertyAsync(mappings.Select(x => x.PropertyMastOldId), cancellationToken);
        return mappings
            .GroupBy(p => p.PropertyId)
            .ToDictionary(g => g.Key, g => g.Sum(x => oldDemandByOldProperty.GetValueOrDefault(x.PropertyMastOldId)));
    }

    // Calculates old demand per new property id by first resolving its mapped old property.
    public async Task<Dictionary<int, decimal>> GetOldDemandByPropertyIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<int, decimal>();

        var mappings = new List<(int PropertyId, int PropertyMastOldId)>();
        foreach (var batch in BatchIds(ids))
        {
            var batchMappings = await (
                from propertyMap in _context.PropertyMapMasters.AsNoTracking()
                where batch.Contains(propertyMap.Id) && propertyMap.IsActive && propertyMap.ParentPropertyMapId.HasValue
                select new { PropertyId = propertyMap.Id, PropertyMastOldId = propertyMap.ParentPropertyMapId!.Value }
            ).Distinct().ToListAsync(cancellationToken);

            mappings.AddRange(batchMappings.Select(x => (x.PropertyId, x.PropertyMastOldId)));
        }

        var oldDemandByOldProperty = await GetOldDemandByOldPropertyAsync(mappings.Select(x => x.PropertyMastOldId), cancellationToken);
        return mappings
            .GroupBy(p => p.PropertyId)
            .ToDictionary(g => g.Key, g => g.Sum(x => oldDemandByOldProperty.GetValueOrDefault(x.PropertyMastOldId)));
    }

    // Calculates current demand per property from TransMast.
    public async Task<Dictionary<int, decimal>> GetCurrentDemandByPropertyAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<int, decimal>();

        var rows = new List<(int PropertyId, decimal Demand)>();
        foreach (var batch in BatchIds(ids))
        {
            var batchRows = await (
                from tm in _context.TransMast.AsNoTracking()
                join tax in _context.TaxMaster.AsNoTracking() on tm.TaxId equals tax.Id
                where batch.Contains(tm.PropertyId) && tm.IsActive && !tm.MarkedForDeletion && tax.IsActive
                      && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
                group tm by tm.PropertyId into g
                select new { PropertyId = g.Key, Demand = g.Sum(x => x.TaxAmount) }
            ).ToListAsync(cancellationToken);

            rows.AddRange(batchRows.Select(x => (x.PropertyId, x.Demand)));
        }

        return rows.GroupBy(x => x.PropertyId).ToDictionary(g => g.Key, g => g.Sum(x => x.Demand));
    }

    // Calculates retro demand per property from TaxPendingDetailsRetro.
    public async Task<Dictionary<int, decimal>> GetRetroDemandByPropertyAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<int, decimal>();

        var rows = new List<(int PropertyId, decimal Demand)>();
        foreach (var batch in BatchIds(ids))
        {
            var batchRows = await (
                from retro in _context.TaxPendingDetailsRetro.AsNoTracking()
                join tax in _context.TaxMaster.AsNoTracking() on retro.TaxId equals tax.Id
                where batch.Contains(retro.PropertyId) && retro.IsActive && !retro.MarkedForDeletion && tax.IsActive
                      && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
                group retro by retro.PropertyId into g
                select new { PropertyId = g.Key, Demand = g.Sum(x => x.PendingAmount ?? 0m) }
            ).ToListAsync(cancellationToken);

            rows.AddRange(batchRows.Select(x => (x.PropertyId, x.Demand)));
        }

        return rows.GroupBy(x => x.PropertyId).ToDictionary(g => g.Key, g => g.Sum(x => x.Demand));
    }

    // Reads active signing authority id by code.
    public Task<int> GetSignAuthorityIdByCodeAsync(string authorityCode, CancellationToken cancellationToken = default)
        => _context.SignAuthorityMaster
            .AsNoTracking()
            .Where(a => a.IsActive && a.AuthorityCode.ToUpper() == authorityCode.Trim().ToUpper())
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

    // Checks whether the property exists and can be signed.
    public Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _context.PropertyMast
            .AsNoTracking()
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

    // Reads active property ids from the requested ids.
    public async Task<List<int>> GetExistingPropertyIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Where(id => id > 0).Distinct().ToList();
        if (!ids.Any())
            return new List<int>();

        var existingIds = new List<int>();
        foreach (var batch in BatchIds(ids))
        {
            existingIds.AddRange(await _context.PropertyMast
                .AsNoTracking()
                .Where(p => batch.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken));
        }

        return existingIds.Distinct().ToList();
    }

    // Checks whether the property already has an active signature row.
    public Task<bool> PropertySignatureExistsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _context.PropertySignatureDetails
            .AsNoTracking()
            .AnyAsync(s => s.PropertyId == propertyId && s.IsActive, cancellationToken);

    // Reads property ids that already have an active signature row.
    public async Task<List<int>> GetExistingPropertySignatureIdsAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Where(id => id > 0).Distinct().ToList();
        if (!ids.Any())
            return new List<int>();

        var signedIds = new List<int>();
        foreach (var batch in BatchIds(ids))
        {
            signedIds.AddRange(await _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(s => batch.Contains(s.PropertyId) && s.IsActive)
                .Select(s => s.PropertyId)
                .ToListAsync(cancellationToken));
        }

        return signedIds.Distinct().ToList();
    }

    // Calculates old demand per old property from TransMastOld.
    private async Task<Dictionary<int, decimal>> GetOldDemandByOldPropertyAsync(IEnumerable<int> oldPropertyIds, CancellationToken cancellationToken)
    {
        var ids = oldPropertyIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<int, decimal>();

        var rows = new List<(int PropertyMastOldId, decimal Demand)>();
        foreach (var batch in BatchIds(ids))
        {
            var batchRows = await (
                from tmo in _context.TransMastOld.AsNoTracking()
                join tax in _context.TaxMaster.AsNoTracking() on tmo.TaxId equals tax.Id
                where batch.Contains(tmo.PropertyMastOldId) && tmo.IsActive && !tmo.MarkedForDeletion && tax.IsActive
                      && tax.TaxCode == TaxTotalCode && tax.TaxName == TaxTotalName
                group tmo by tmo.PropertyMastOldId into g
                select new { PropertyMastOldId = g.Key, Demand = g.Sum(x => x.TaxAmount) }
            ).ToListAsync(cancellationToken);

            rows.AddRange(batchRows.Select(x => (x.PropertyMastOldId, x.Demand)));
        }

        return rows.GroupBy(x => x.PropertyMastOldId).ToDictionary(g => g.Key, g => g.Sum(x => x.Demand));
    }

    // Groups property-level demand values into zone-level totals.
    private static Dictionary<int, decimal> SumDemandByZone(IEnumerable<(int PropertyId, int ZoneId)> propertyZoneMap, IReadOnlyDictionary<int, decimal> demandByProperty)
    {
        return propertyZoneMap
            .GroupBy(p => p.ZoneId)
            .ToDictionary(g => g.Key, g => g.Sum(x => demandByProperty.GetValueOrDefault(x.PropertyId)));
    }

    private static IEnumerable<List<int>> BatchIds(IEnumerable<int> ids)
    {
        var batch = new List<int>(SqlServerInClauseBatchSize);
        foreach (var id in ids.Distinct())
        {
            batch.Add(id);
            if (batch.Count < SqlServerInClauseBatchSize)
                continue;

            yield return batch;
            batch = new List<int>(SqlServerInClauseBatchSize);
        }

        if (batch.Count > 0)
            yield return batch;
    }

    // Inserts one Clerk approval request into PropertySignatureDetails.
    public async Task<int> InsertPropertySignatureAsync(int propertyId, int userId, int signAuthorityId, CancellationToken cancellationToken = default)
    {
        return await InsertPropertySignaturesAsync(new[] { propertyId }, userId, signAuthorityId, cancellationToken);
    }

    // Inserts many Clerk approval requests into PropertySignatureDetails.
    public async Task<int> InsertPropertySignaturesAsync(IEnumerable<int> propertyIds, int userId, int signAuthorityId, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.Where(id => id > 0).Distinct().ToList();
        if (!ids.Any())
            return 0;

        var noticeNoByPropertyId = await GetNoticeNoByPropertyIdAsync(ids, cancellationToken);
        var now = DateTime.Now;
        var signatures = ids.Select(propertyId => new PropertySignatureDetailsEntity
        {
            UserId = userId,
            PropertyId = propertyId,
            SignAuthorityId = signAuthorityId,
            NoticeNo = noticeNoByPropertyId.GetValueOrDefault(propertyId),
            IsActive = true,
            CreatedDate = now,
            CreatedBy = userId
        }).ToList();

        await _context.PropertySignatureDetails.AddRangeAsync(signatures, cancellationToken);
        return await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<int, string?>> GetNoticeNoByPropertyIdAsync(IEnumerable<int> propertyIds, CancellationToken cancellationToken)
    {
        var ids = propertyIds.Where(id => id > 0).Distinct().ToList();
        var noticeNoByPropertyId = new Dictionary<int, string?>();

        foreach (var batch in BatchIds(ids))
        {
            var rows = await _context.PropertyMast
                .AsNoTracking()
                .Where(p => batch.Contains(p.Id))
                .Select(p => new { p.Id, p.UPICId })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
                noticeNoByPropertyId[row.Id] = row.UPICId;
        }

        return noticeNoByPropertyId;
    }


}
