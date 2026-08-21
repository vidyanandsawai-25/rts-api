using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.DataEntrySameAs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Utilities;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Re-implements the legacy [PTIS].[DataEntrySameAS] stored procedure in application code.
/// Makes one or more destination properties' data-entry the SAME AS a source property by copying
/// PropertyDetails -> RoomWiseSubmissionDetails -> RoomWiseMinusData, after soft-deleting the
/// destination's matching data-entry (replace semantics). The whole operation runs in one transaction.
///
/// The clone field-mapping mirrors <see cref="PropertyDataCopier"/>; parking rows are identified via
/// TypeOfUseMaster -> TypeOfUseCategoryMaster, matching the units projection used by this service.
/// </summary>
public class DataEntrySameAsService : IDataEntrySameAsService
{
    private const string FilterParking = "PARKING";
    private const string FilterTypewise = "TYPEWISE";
    private const string FilterPropertywise = "PROPERTYWISE";

    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<RoomWiseSubmissionDetailsEntity, int> _roomWiseSubmissionRepository;
    private readonly IRepository<RoomWiseMinusDataEntity, int> _roomWiseMinusDataRepository;
    private readonly IRepository<ParkingTypeMasterEntity, int> _parkingTypeRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyDetailsRepository;
    private readonly IRepository<WingEntity, int> _wingRepository;
    private readonly IRepository<BuildingPlanTypeEntity, int> _buildingPlanTypeRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<ZoneEntity, int> _zoneRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _propertyCategoryRepository;
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;
    private readonly IRepository<TypeOfUseCategoryEntity, int> _typeOfUseCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataEntrySameAsService> _logger;

    public DataEntrySameAsService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<RoomWiseSubmissionDetailsEntity, int> roomWiseSubmissionRepository,
        IRepository<RoomWiseMinusDataEntity, int> roomWiseMinusDataRepository,
        IRepository<ParkingTypeMasterEntity, int> parkingTypeRepository,
        IRepository<SocietyDetailsEntity, int> societyDetailsRepository,
        IRepository<WingEntity, int> wingRepository,
        IRepository<BuildingPlanTypeEntity, int> buildingPlanTypeRepository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<ZoneEntity, int> zoneRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<PropertyCategoryEntity, int> propertyCategoryRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<TypeOfUseCategoryEntity, int> typeOfUseCategoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<DataEntrySameAsService> logger)
    {
        _propertyRepository = propertyRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _roomWiseSubmissionRepository = roomWiseSubmissionRepository;
        _roomWiseMinusDataRepository = roomWiseMinusDataRepository;
        _parkingTypeRepository = parkingTypeRepository;
        _societyDetailsRepository = societyDetailsRepository;
        _wingRepository = wingRepository;
        _buildingPlanTypeRepository = buildingPlanTypeRepository;
        _wardRepository = wardRepository;
        _zoneRepository = zoneRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _propertyCategoryRepository = propertyCategoryRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _typeOfUseCategoryRepository = typeOfUseCategoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DataEntrySameAsResultDto> ExecuteAsync(
        DataEntrySameAsRequestDto request,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Normalize & validate filter type(s) ───────────────────────────
        // FilterType accepts one mode or a comma-separated list (e.g. "PARKING,PROPERTYWISE");
        // each listed mode is applied within the single transaction below.
        var filterTypes = (request.FilterType ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => f.ToUpperInvariant())
            .Distinct()
            .ToList();

        if (filterTypes.Count == 0)
        {
            throw new ArgumentException(
                $"FilterType must be one of {FilterParking}, {FilterTypewise} or {FilterPropertywise} (comma-separated list allowed).");
        }

        var invalid = filterTypes
            .Where(f => f != FilterParking && f != FilterTypewise && f != FilterPropertywise)
            .ToList();
        if (invalid.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid FilterType value(s): {string.Join(", ", invalid)}. " +
                $"Allowed values are {FilterParking}, {FilterTypewise} and {FilterPropertywise}.");
        }

        // ── 2. Validate source ───────────────────────────────────────────────
        var source = await _propertyRepository.GetQueryable()
            .Where(p => p.Id == request.SourcePropertyId)
            .Select(p => new { p.Id, p.WardId, p.PropertyNo, p.PartitionNo, p.Type })
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
            throw new ArgumentException($"Source property {request.SourcePropertyId} not found.");

        var result = new DataEntrySameAsResultDto { SourcePropertyId = request.SourcePropertyId };

        // ── 3. Clean destinations (distinct, drop self / non-existent) ────────
        var requested = (request.DestinationPropertyIds ?? [])
            .Where(id => id != request.SourcePropertyId)
            .Distinct()
            .ToList();

        var existingIds = await _propertyRepository.GetQueryable()
            .Where(p => requested.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var destinationIds = requested.Where(existingIds.Contains).ToList();

        // TYPEWISE can act on the source alone: "make this property SAME AS itself with a new Type"
        // changes only the main property's own Type. Allowed only when TYPEWISE is the sole filter
        // and the caller explicitly listed the source among the destinations.
        var isTypewiseSelfChange =
            filterTypes.Count == 1
            && filterTypes[0] == FilterTypewise
            && (request.DestinationPropertyIds?.Contains(request.SourcePropertyId) ?? false);

        // Don't count the source itself as a "dropped" destination for a deliberate self-type-change.
        var droppedCount = (request.DestinationPropertyIds?.Count ?? 0)
            - destinationIds.Count
            - (isTypewiseSelfChange ? 1 : 0);

        if (droppedCount > 0)
        {
            result.SkippedDestinations = droppedCount;
            result.Warnings.Add(
                $"{droppedCount} destination id(s) were dropped (self-reference, duplicate, or not found).");
        }

        if (destinationIds.Count == 0)
        {
            if (!isTypewiseSelfChange)
                throw new ArgumentException("No valid destination properties supplied.");

            if (request.Type is not (>= 1 and <= 99))
                throw new ArgumentException(
                    "A new Type between 1 and 99 is required to change the property's own type.");
        }

        // ── 4..9. Transactional work — each requested filter mode acts independently ──
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Use the same TypeOfUse category classification as GetPropertyUnitsAsync. Using
            // ParkingTypeMaster here caused Open Parking rows to display as parking but be copied
            // by PROPERTYWISE instead of PARKING when the two masters were out of sync.
            var needsCopy = filterTypes.Contains(FilterParking) || filterTypes.Contains(FilterPropertywise);
            var parkingTypeIds = needsCopy
                ? await _typeOfUseRepository.GetQueryable()
                    .Where(tom => _typeOfUseCategoryRepository.GetQueryable()
                        .Any(c => c.Id == tom.TypeOfUseCategoryId && c.TypeOfUseCategoryCode == TypeOfUseConstants.Parking))
                    .Select(tom => tom.Id)
                    .Distinct()
                    .ToListAsync(cancellationToken)
                : [];

            foreach (var filterType in filterTypes)
            {
                if (filterType == FilterTypewise)
                {
                    // TYPEWISE: only propagate PropertyMast.Type — no soft-delete, no copy.
                    result.TypeUpdatedProperties += await StampTypeAsync(
                        source.Id, source.WardId, source.PropertyNo, source.PartitionNo, source.Type,
                        destinationIds, request.Type, updatedBy, cancellationToken);

                    // Also upsert BuildingPlanType for the building-level properties (PartitionNo = WingNo OR '').
                    var srcTypeValue = int.TryParse(source.Type, out var parsedSrcType) ? parsedSrcType : 0;
                    var typeToSet = request.Type is >= 1 and <= 99 ? request.Type : srcTypeValue;
                    result.BuildingPlanTypeInserted += await UpsertBuildingPlanTypeAsync(
                        source.WardId, source.PropertyNo, typeToSet, updatedBy, cancellationToken);
                }
                else
                {
                    // PARKING / PROPERTYWISE: replace the destination's matching data-entry, then copy.
                    var isParking = filterType == FilterParking;

                    await SoftDeleteDestinationDataEntryAsync(destinationIds, parkingTypeIds, isParking, updatedBy, cancellationToken);

                    await CopyDataEntryAsync(request.SourcePropertyId, destinationIds, parkingTypeIds, isParking, updatedBy, result, cancellationToken);
                }
            }

            result.ProcessedDestinations = destinationIds.Count;

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return result;
    }

    public async Task<List<DataEntrySameAsPropertyDto>> GetSiblingPropertiesAsync(
        DataEntrySameAsQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var wardId = query.WardId;
        var propertyNo = query.PropertyNo;
        var partitionNo = query.PartitionNo ?? string.Empty;
        // PartitionNo is optional: when supplied, return only that partition;
        // when omitted, return all matching properties.
        var hasPartition = !string.IsNullOrEmpty(partitionNo);

        // PropertyMast LEFT JOIN SocietyDetailsMast LEFT JOIN WingMaster LEFT JOIN PropertyDetails.
        // The `PartitionNo != WingNo` predicate must drop rows whose wing did not match. In raw SQL
        // `PartitionNo != NULL` is "unknown" and excludes the row, but EF Core rewrites `!=` with C#
        // null-semantics ("A" != null == true), which would WRONGLY keep unmatched-wing rows and produce
        // a duplicate per society row. The explicit `wm != null` guard drops those rows (WingNo is
        // non-nullable, so an unmatched left join is the only source of a null wing number), restoring
        // the SQL behaviour and keeping the expression null-safe when run in-memory.
        // PropertyDetails is grouped so that CarpetArea columns are summed per property.
        var rows =
            from pm in _propertyRepository.GetQueryable()
            join sdm in _societyDetailsRepository.GetQueryable()
                on (int?)pm.Id equals sdm.PropertyId into sdmGroup
            from sdm in sdmGroup.DefaultIfEmpty()
            join wm in _wingRepository.GetQueryable()
                on sdm.WingId equals (int?)wm.Id into wmGroup
            from wm in wmGroup.DefaultIfEmpty()
            join pd in _propertyDetailsRepository.GetQueryable().Where(pd => pd.IsActive && !pd.MarkedForDeletion)
                on pm.Id equals pd.PropertyId into pdGroup
            from pd in pdGroup.DefaultIfEmpty()
            where pm.WardId == wardId
                  && pm.PropertyNo == propertyNo
                  && (!hasPartition || pm.PartitionNo == partitionNo)
                  && pm.PartitionNo != ""
                  && wm != null
                  && pm.PartitionNo != wm.WingNo
            group new { pd.CarpetAreaSqMeter, pd.CarpetAreaSqFeet } by new
            {
                pm.Id,
                pm.WardId,
                pm.PropertyNo,
                pm.PartitionNo,
                pm.Type,
                sdm.WingName,
                pm.FlatOrShopNo
            } into g
            select new DataEntrySameAsPropertyDto
            {
                PropertyId = g.Key.Id,
                WardId = g.Key.WardId,
                PropertyNo = g.Key.PropertyNo,
                PartitionNo = g.Key.PartitionNo,
                Type = g.Key.Type ?? string.Empty,
                WingName = g.Key.WingName,
                FlatOrShopNo = g.Key.FlatOrShopNo,
                CarpetAreaSqMeter = g.Sum(x => x.CarpetAreaSqMeter ?? 0),
                CarpetAreaSqFeet = g.Sum(x => x.CarpetAreaSqFeet ?? 0)
            };

        return await rows.ToListAsync(cancellationToken);
    }

    public async Task<List<DataEntrySameAsUnitDto>> GetPropertyUnitsAsync(
        DataEntrySameAsUnitsQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var wardId = query.WardId;
        var propertyNo = query.PropertyNo;

        // Treat blank optional inputs as "not supplied" so empty query strings don't filter.
        string? partitionNo = string.IsNullOrWhiteSpace(query.PartitionNo) ? null : query.PartitionNo;
        string? partType = string.IsNullOrWhiteSpace(query.PartType) ? null : query.PartType;
        string? categoryName = string.IsNullOrWhiteSpace(query.CategoryName) ? null : query.CategoryName;
        string? type = string.IsNullOrWhiteSpace(query.Type) ? null : query.Type;
        string? searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm) ? null : query.SearchTerm;
        var hasPartition = partitionNo != null;

        // TypeOfUse ids that belong to the PARKING category (TypeOfUseMaster -> TypeOfUseCategoryMaster).
        // Materialized once and matched via Contains, so the main query needs no extra joins and the
        // parking split stays null-safe. Mirrors the parkingTypeIds pattern used in ExecuteAsync.
        var parkingTypeOfUseIds = await _typeOfUseRepository.GetQueryable()
            .Where(tom => _typeOfUseCategoryRepository.GetQueryable()
                .Any(c => c.Id == tom.TypeOfUseCategoryId && c.TypeOfUseCategoryCode == TypeOfUseConstants.Parking))
            .Select(tom => tom.Id)
            .ToListAsync(cancellationToken);

        // PropertyMast LEFT JOIN Ward/Zone/PropertyType/Category masters LEFT JOIN PropertyDetails, grouped
        // per property with carpet areas summed. Hard rules (always applied): only active/non-deleted
        // properties, PartType != 'Amenity', and IsWing = 0 (partition is not a WingMaster.WingNo).
        // Like GetSiblingPropertiesAsync, the null guards (ptm != null, ptm.PartType != null) restore SQL
        // three-valued logic — EF would otherwise keep NULL-PartType rows via C# null-semantics.
        var rows =
            from pm in _propertyRepository.GetQueryable()
            join wm in _wardRepository.GetQueryable()
                on pm.WardId equals wm.Id into wmGroup
            from wm in wmGroup.DefaultIfEmpty()
            join zm in _zoneRepository.GetQueryable()
                on wm.ZoneId equals zm.Id into zmGroup
            from zm in zmGroup.DefaultIfEmpty()
            join ptm in _propertyTypeRepository.GetQueryable()
                on pm.PropertyTypeId equals (int?)ptm.Id into ptmGroup
            from ptm in ptmGroup.DefaultIfEmpty()
            join pcm in _propertyCategoryRepository.GetQueryable()
                on pm.CategoryId equals (int?)pcm.Id into pcmGroup
            from pcm in pcmGroup.DefaultIfEmpty()
            join pd in _propertyDetailsRepository.GetQueryable().Where(pd => pd.IsActive && !pd.MarkedForDeletion)
                on pm.Id equals pd.PropertyId into pdGroup
            from pd in pdGroup.DefaultIfEmpty()
            where pm.IsActive && !pm.MarkedForDeletion
                  && pm.WardId == wardId
                  && pm.PropertyNo == propertyNo
                  && (!hasPartition || pm.PartitionNo == partitionNo)
                  // Hard rule: PartType present and not 'Amenity' (NULL PartType excluded, as in SQL).
                  && ptm != null && ptm.PartType != null && ptm.PartType != PartTypeConstants.Amenity
                  // Hard rule: IsWing = 0 — no WingMaster row whose WingNo equals this partition.
                  && !_wingRepository.GetQueryable().Any(w => w.WingNo == pm.PartitionNo)
                  // Hard rule: skip Apartment-category rows whose PartitionNo is blank.
                  && !(pcm.PropertyCategoryName == PropertyConstants.Categories.Apartment
                       && (pm.PartitionNo == null || pm.PartitionNo == ""))
            group new
            {
                pd.CarpetAreaSqMeter,
                pd.CarpetAreaSqFeet,
                pd.BuiltupAreaSqMeter,
                pd.BuiltupAreaSqFeet,
                IsParking = parkingTypeOfUseIds.Contains(pd.TypeOfUseId)
            } by new
            {
                pm.Id,
                pm.TaxZoneId,
                ZoneId = (int?)zm.Id,
                zm.ZoneNo,
                pm.WardId,
                wm.WardNo,
                pm.PropertyNo,
                pm.PartitionNo,
                pm.PropertyTypeId,
                ptm.PartType,
                pm.CategoryId,
                pcm.PropertyCategoryName,
                pm.Type,
                pm.FlatOrShopNo
            } into g
            select new DataEntrySameAsUnitDto
            {
                PropertyId = g.Key.Id,
                TaxZoneId = g.Key.TaxZoneId,
                ZoneId = g.Key.ZoneId,
                ZoneNo = g.Key.ZoneNo,
                WardId = g.Key.WardId,
                WardNo = g.Key.WardNo,
                PropertyNo = g.Key.PropertyNo,
                PartitionNo = g.Key.PartitionNo ?? string.Empty,
                PropertyTypeId = g.Key.PropertyTypeId,
                PartType = g.Key.PartType,
                CategoryId = g.Key.CategoryId,
                PropertyCategoryName = g.Key.PropertyCategoryName,
                IsWing = false,
                Type = g.Key.Type ?? "0",
                FlatOrShopNo = g.Key.FlatOrShopNo ?? "0",
                TotalCarpetAreaSqMeter = g.Sum(x => x.CarpetAreaSqMeter ?? 0),
                TotalCarpetAreaSqFeet = g.Sum(x => x.CarpetAreaSqFeet ?? 0),
                TotalBuiltupAreaSqMeter = g.Sum(x => x.BuiltupAreaSqMeter ?? 0),
                TotalBuiltupAreaSqFeet = g.Sum(x => x.BuiltupAreaSqFeet ?? 0),
                ParkingCarpetAreaSqMeter = g.Sum(x => x.IsParking ? (x.CarpetAreaSqMeter ?? 0) : 0),
                ParkingCarpetAreaSqFeet = g.Sum(x => x.IsParking ? (x.CarpetAreaSqFeet ?? 0) : 0),
                ParkingBuiltupAreaSqMeter = g.Sum(x => x.IsParking ? (x.BuiltupAreaSqMeter ?? 0) : 0),
                ParkingBuiltupAreaSqFeet = g.Sum(x => x.IsParking ? (x.BuiltupAreaSqFeet ?? 0) : 0)
            };

        // Optional filters + search applied on the projected rows (kept out of the join query so
        // nullable-reference narrowing behaves normally; still translated to SQL by EF).
        if (partType != null)
            rows = rows.Where(r => r.PartType == partType);
        if (categoryName != null)
            rows = rows.Where(r => r.PropertyCategoryName == categoryName);
        if (type != null)
            rows = rows.Where(r => r.Type == type);
        if (searchTerm != null)
            rows = rows.Where(r =>
                (r.PartType != null && r.PartType.Contains(searchTerm))
                || (r.PropertyCategoryName != null && r.PropertyCategoryName.Contains(searchTerm))
                || r.Type.Contains(searchTerm));

        var results = await rows.ToListAsync(cancellationToken);

        // Natural ordering ("A2" before "A10") can't be translated to SQL, so sort in memory.
        return results
            .OrderBy(r => r.PartitionNo, NaturalStringComparer.Instance)
            .ThenBy(r => r.FlatOrShopNo, NaturalStringComparer.Instance)
            .ToList();
    }

    /// <summary>
    /// Soft-deletes (IsActive=false, MarkedForDeletion=true) the destinations' matching data-entry,
    /// child → parent. Uses set-based ExecuteUpdate to avoid materializing rows with NULL IsActive.
    /// </summary>
    private async Task SoftDeleteDestinationDataEntryAsync(
        List<int> destinationIds,
        List<int> parkingTypeIds,
        bool isParking,
        int updatedBy,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        // PropertyDetails on destinations matching the filter.
        // NOTE: do NOT filter on IsActive here. Legacy rows can have IsActive = NULL in the database;
        // `pd.IsActive` translates to `IsActive = 1` and would skip those rows, leaving them in place so
        // the subsequent copy produces duplicates of the same type. Replace targets every matching row
        // that is not already marked for deletion (mirrors the SP's unconditional delete).
        var detailQuery = _propertyDetailsRepository.GetQueryable()
            .Where(pd => destinationIds.Contains(pd.PropertyId) && !pd.MarkedForDeletion);
        detailQuery = isParking
            ? detailQuery.Where(pd => parkingTypeIds.Contains(pd.TypeOfUseId))
            : detailQuery.Where(pd => !parkingTypeIds.Contains(pd.TypeOfUseId));

        var detailIds = await detailQuery.Select(pd => pd.Id).ToListAsync(cancellationToken);
        if (detailIds.Count == 0)
            return;

        var submissionIds = await _roomWiseSubmissionRepository.GetQueryable()
            .Where(rs => rs.PropertyDetailsId.HasValue
                         && detailIds.Contains(rs.PropertyDetailsId.Value)
                         && !rs.MarkedForDeletion)
            .Select(rs => rs.Id)
            .ToListAsync(cancellationToken);

        // Child: RoomWiseMinusData.
        if (submissionIds.Count > 0)
        {
            await _roomWiseMinusDataRepository.GetQueryable()
                .Where(rm => submissionIds.Contains(rm.RoomWiseSubmissionId) && !rm.MarkedForDeletion)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rm => rm.IsActive, false)
                    .SetProperty(rm => rm.MarkedForDeletion, true)
                    .SetProperty(rm => rm.MarkedForDeletionDate, now)
                    .SetProperty(rm => rm.UpdatedBy, updatedBy)
                    .SetProperty(rm => rm.UpdatedDate, now),
                    cancellationToken);

            // Middle: RoomWiseSubmissionDetails.
            await _roomWiseSubmissionRepository.GetQueryable()
                .Where(rs => submissionIds.Contains(rs.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rs => rs.IsActive, false)
                    .SetProperty(rs => rs.MarkedForDeletion, true)
                    .SetProperty(rs => rs.MarkedForDeletionDate, now)
                    .SetProperty(rs => rs.UpdatedBy, updatedBy)
                    .SetProperty(rs => rs.UpdatedDate, now),
                    cancellationToken);
        }

        // Parent: PropertyDetails.
        await _propertyDetailsRepository.GetQueryable()
            .Where(pd => detailIds.Contains(pd.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(pd => pd.IsActive, false)
                .SetProperty(pd => pd.MarkedForDeletion, true)
                .SetProperty(pd => pd.MarkedForDeletionDate, now)
                .SetProperty(pd => pd.UpdatedBy, updatedBy)
                .SetProperty(pd => pd.UpdatedDate, now),
                cancellationToken);
    }

    /// <summary>
    /// Copies the source property's active data-entry to every destination, filtered by the parking
    /// predicate, remapping ids through each level. Field mapping mirrors <see cref="PropertyDataCopier"/>.
    /// </summary>
    private async Task CopyDataEntryAsync(
        int sourcePropertyId,
        List<int> destinationIds,
        List<int> parkingTypeIds,
        bool isParking,
        int updatedBy,
        DataEntrySameAsResultDto result,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        // Source PropertyDetails (filtered, active) — projected to avoid NULL-IsActive materialization.
        var srcDetailQuery = _propertyDetailsRepository.GetQueryable()
            .Where(pd => pd.PropertyId == sourcePropertyId && pd.IsActive && !pd.MarkedForDeletion);
        srcDetailQuery = isParking
            ? srcDetailQuery.Where(pd => parkingTypeIds.Contains(pd.TypeOfUseId))
            : srcDetailQuery.Where(pd => !parkingTypeIds.Contains(pd.TypeOfUseId));

        var sourceDetails = await srcDetailQuery
            .Select(pd => new SourceDetail(
                pd.Id, pd.IsTaxable, pd.FloorId, pd.SubFloorId, pd.ConstructionYear, pd.AssessmentYear,
                pd.ConstructionTypeId, pd.TypeOfUseId, pd.SubTypeOfUseId, pd.CarpetAreaSqMeter,
                pd.CarpetAreaSqFeet, pd.BuiltupAreaSqMeter, pd.BuiltupAreaSqFeet, pd.NoOfRooms, pd.IsRenter))
            .ToListAsync(cancellationToken);

        if (sourceDetails.Count == 0)
            return;

        var sourceDetailIds = sourceDetails.Select(d => d.Id).ToList();

        // Destination floor overrides (FloorId = destination.PropertyFloorId ?? source.FloorId).
        var destFloorById = await _propertyRepository.GetQueryable()
            .Where(p => destinationIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PropertyFloorId })
            .ToDictionaryAsync(p => p.Id, p => p.PropertyFloorId, cancellationToken);

        // Source RoomWiseSubmissionDetails (projected).
        var sourceSubmissions = await _roomWiseSubmissionRepository.GetQueryable()
            .Where(rs => rs.PropertyDetailsId.HasValue
                         && sourceDetailIds.Contains(rs.PropertyDetailsId.Value)
                         && rs.IsActive && !rs.MarkedForDeletion)
            .Select(rs => new SourceSubmission(
                rs.Id, rs.PropertyDetailsId!.Value, rs.LengthMtr, rs.WidthMtr, rs.AreaSqMtr, rs.HeightMtr,
                rs.Base1Mtr, rs.Base2Mtr, rs.NoOfRooms, rs.TotalAreaSqMtr, rs.Shape, rs.RoomNo,
                rs.OuterYesNo, rs.RoomTypeId, rs.SubmissionType, rs.MinusYesNo))
            .ToListAsync(cancellationToken);

        var sourceSubmissionIds = sourceSubmissions.Select(s => s.Id).ToList();

        // Source RoomWiseMinusData (projected).
        var sourceMinus = sourceSubmissionIds.Count == 0
            ? []
            : await _roomWiseMinusDataRepository.GetQueryable()
                .Where(rm => sourceSubmissionIds.Contains(rm.RoomWiseSubmissionId) && rm.IsActive && !rm.MarkedForDeletion)
                .Select(rm => new SourceMinus(
                    rm.RoomWiseSubmissionId, rm.LengthMtr, rm.WidthMtr, rm.AreaSqMtr, rm.HeightMtr,
                    rm.Base1Mtr, rm.Base2Mtr, rm.Shape, rm.IsOffset))
                .ToListAsync(cancellationToken);

        // ── Level 1: PropertyDetails (one new row per source-detail × destination) ──
        var newDetails = new List<PropertyDetailsEntity>();
        var detailKeys = new List<(int DestId, int SourceDetailId)>();

        foreach (var destId in destinationIds)
        {
            destFloorById.TryGetValue(destId, out var destFloorId);
            foreach (var src in sourceDetails)
            {
                newDetails.Add(new PropertyDetailsEntity
                {
                    PropertyId = destId,
                    FloorId = destFloorId ?? src.FloorId,
                    SubFloorId = src.SubFloorId,
                    ConstructionYear = src.ConstructionYear,
                    AssessmentYear = src.AssessmentYear,
                    ConstructionTypeId = src.ConstructionTypeId,
                    TypeOfUseId = src.TypeOfUseId,
                    SubTypeOfUseId = src.SubTypeOfUseId,
                    CarpetAreaSqMeter = src.CarpetAreaSqMeter,
                    CarpetAreaSqFeet = src.CarpetAreaSqFeet,
                    BuiltupAreaSqMeter = src.BuiltupAreaSqMeter,
                    BuiltupAreaSqFeet = src.BuiltupAreaSqFeet,
                    NoOfRooms = src.NoOfRooms,
                    IsRenter = src.IsRenter,
                    IsTaxable = src.IsTaxable,
                    MarkedForDeletion = false,
                    IsActive = true,
                    CreatedBy = updatedBy,
                    CreatedDate = now,
                    UpdatedBy = updatedBy,
                    UpdatedDate = now
                });
                detailKeys.Add((destId, src.Id));
            }
        }

        await _propertyDetailsRepository.AddRangeAsync(newDetails, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // (destId, oldDetailId) -> newDetailId
        var detailMap = new Dictionary<(int, int), int>(detailKeys.Count);
        for (var i = 0; i < detailKeys.Count; i++)
            detailMap[detailKeys[i]] = newDetails[i].Id;

        result.PropertyDetailsCopied += newDetails.Count;

        // ── Level 2: RoomWiseSubmissionDetails ──
        var newSubmissions = new List<RoomWiseSubmissionDetailsEntity>();
        var submissionKeys = new List<(int DestId, int SourceSubmissionId)>();

        foreach (var destId in destinationIds)
        {
            foreach (var src in sourceSubmissions)
            {
                if (!detailMap.TryGetValue((destId, src.PropertyDetailsId), out var newDetailId))
                    continue;

                newSubmissions.Add(new RoomWiseSubmissionDetailsEntity
                {
                    PropertyId = destId,
                    PropertyDetailsId = newDetailId,
                    LengthMtr = src.LengthMtr,
                    WidthMtr = src.WidthMtr,
                    AreaSqMtr = src.AreaSqMtr,
                    HeightMtr = src.HeightMtr,
                    Base1Mtr = src.Base1Mtr,
                    Base2Mtr = src.Base2Mtr,
                    NoOfRooms = src.NoOfRooms,
                    TotalAreaSqMtr = src.TotalAreaSqMtr,
                    Shape = src.Shape,
                    RoomNo = src.RoomNo,
                    OuterYesNo = src.OuterYesNo,
                    RoomTypeId = src.RoomTypeId,
                    SubmissionType = src.SubmissionType,
                    MinusYesNo = src.MinusYesNo,
                    MarkedForDeletion = false,
                    IsActive = true,
                    CreatedBy = updatedBy,
                    CreatedDate = now,
                    UpdatedBy = updatedBy,
                    UpdatedDate = now
                });
                submissionKeys.Add((destId, src.Id));
            }
        }

        if (newSubmissions.Count > 0)
        {
            await _roomWiseSubmissionRepository.AddRangeAsync(newSubmissions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // (destId, oldSubmissionId) -> newSubmissionId
        var submissionMap = new Dictionary<(int, int), int>(submissionKeys.Count);
        for (var i = 0; i < submissionKeys.Count; i++)
            submissionMap[submissionKeys[i]] = newSubmissions[i].Id;

        result.RoomSubmissionsCopied += newSubmissions.Count;

        // ── Level 3: RoomWiseMinusData ──
        if (sourceMinus.Count == 0)
            return;

        var newMinus = new List<RoomWiseMinusDataEntity>();
        foreach (var destId in destinationIds)
        {
            foreach (var src in sourceMinus)
            {
                if (!submissionMap.TryGetValue((destId, src.RoomWiseSubmissionId), out var newSubmissionId))
                    continue;

                newMinus.Add(new RoomWiseMinusDataEntity
                {
                    RoomWiseSubmissionId = newSubmissionId,
                    LengthMtr = src.LengthMtr,
                    WidthMtr = src.WidthMtr,
                    AreaSqMtr = src.AreaSqMtr,
                    HeightMtr = src.HeightMtr,
                    Base1Mtr = src.Base1Mtr,
                    Base2Mtr = src.Base2Mtr,
                    Shape = src.Shape,
                    IsOffset = src.IsOffset,
                    MarkedForDeletion = false,
                    IsActive = true,
                    CreatedBy = updatedBy,
                    CreatedDate = now,
                    UpdatedBy = updatedBy,
                    UpdatedDate = now
                });
            }
        }

        if (newMinus.Count > 0)
        {
            await _roomWiseMinusDataRepository.AddRangeAsync(newMinus, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            result.RoomMinusCopied += newMinus.Count;
        }
    }

    /// <summary>
    /// TYPEWISE Type propagation. Stamps the manual Type (1-99) on the source when supplied, and sets
    /// the effective Type on destinations that pass the SP's sibling-partition gate (a destination with
    /// the same Ward+PropertyNo but a different PartitionNo that is non-empty and differs from its wing).
    /// Returns the number of destination rows updated.
    /// </summary>
    private async Task<int> StampTypeAsync(
        int sourcePropertyId,
        int sourceWardId,
        string? sourcePropertyNo,
        string? sourcePartitionNo,
        string? sourceType,
        List<int> destinationIds,
        int requestedType,
        int updatedBy,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var srcPartition = sourcePartitionNo ?? string.Empty;
        var srcTypeValue = int.TryParse(sourceType, out var parsed) ? parsed : 0;
        var typeToSet = requestedType is >= 1 and <= 99 ? requestedType : srcTypeValue;
        var typeToSetText = typeToSet.ToString();

        // Source stamp: only when a manual Type (1-99) is supplied; applied directly, always overwrites.
        var sourceUpdated = 0;
        if (requestedType is >= 1 and <= 99)
        {
            var requestedTypeText = requestedType.ToString();
            sourceUpdated = await _propertyRepository.GetQueryable()
                .Where(p => p.Id == sourcePropertyId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Type, requestedTypeText)
                    .SetProperty(p => p.UpdatedBy, updatedBy)
                    .SetProperty(p => p.UpdatedDate, now),
                    cancellationToken);
        }

        // Candidate destinations: same Ward + PropertyNo, different non-empty PartitionNo.
        var candidates = await _propertyRepository.GetQueryable()
            .Where(p => destinationIds.Contains(p.Id)
                        && p.WardId == sourceWardId
                        && p.PropertyNo == sourcePropertyNo
                        && p.PartitionNo != null
                        && p.PartitionNo != ""
                        && p.PartitionNo != srcPartition)
            .Select(p => new { p.Id, p.PartitionNo })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return sourceUpdated;

        var candidateIds = candidates.Select(c => c.Id).ToList();

        // Wing numbers per candidate property (SocietyDetailsMast -> WingMaster).
        var societyRows = await _societyDetailsRepository.GetQueryable()
            .Where(sd => sd.PropertyId.HasValue && candidateIds.Contains(sd.PropertyId.Value))
            .Select(sd => new { PropertyId = sd.PropertyId!.Value, sd.WingId })
            .ToListAsync(cancellationToken);

        var wingIds = societyRows.Where(s => s.WingId.HasValue).Select(s => s.WingId!.Value).Distinct().ToList();
        var wingNoById = wingIds.Count == 0
            ? new Dictionary<int, string>()
            : await _wingRepository.GetQueryable()
                .Where(w => wingIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WingNo, cancellationToken);

        // PropertyId -> set of wing numbers (null/empty when WingId missing).
        var wingsByProperty = societyRows
            .GroupBy(s => s.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.WingId.HasValue && wingNoById.TryGetValue(s.WingId.Value, out var no)
                        ? no ?? string.Empty
                        : string.Empty)
                      .ToList());

        // Gate (PartitionNo already guaranteed non-empty and != source partition):
        // qualifies if it has no society wing rows, or any wing number differs from its PartitionNo.
        var qualifyingIds = candidates
            .Where(c =>
            {
                var partition = c.PartitionNo ?? string.Empty;
                return !wingsByProperty.TryGetValue(c.Id, out var wings)
                       || wings.Count == 0
                       || wings.Any(w => !string.Equals(w, partition, StringComparison.Ordinal));
            })
            .Select(c => c.Id)
            .ToList();

        if (qualifyingIds.Count == 0)
            return sourceUpdated;

        await _propertyRepository.GetQueryable()
            .Where(p => qualifyingIds.Contains(p.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Type, typeToSetText)
                .SetProperty(p => p.UpdatedBy, updatedBy)
                .SetProperty(p => p.UpdatedDate, now),
                cancellationToken);

        return sourceUpdated + qualifyingIds.Count;
    }

    /// <summary>
    /// TYPEWISE only: upserts PTIS.BuildingPlanType for the building identified by WardId+PropertyNo —
    /// inserts one row for (WardId, PropertyNo, Type) if it doesn't already exist (non-deleted).
    /// Building-level guard: at least one candidate property must have PartitionNo empty or equal to a wing number.
    /// </summary>
    private async Task<int> UpsertBuildingPlanTypeAsync(
        int sourceWardId,
        string? sourcePropertyNo,
        int typeToSet,
        int updatedBy,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var typeToSetText = typeToSet.ToString();

        // All properties in the source's Ward + PropertyNo.
        var candidates = await _propertyRepository.GetQueryable()
            .Where(p => p.WardId == sourceWardId && p.PropertyNo == sourcePropertyNo)
            .Select(p => new { p.Id, p.PartitionNo })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return 0;

        var candidateIds = candidates.Select(c => c.Id).ToList();

        // Wing numbers per candidate property (SocietyDetailsMast -> WingMaster).
        var societyRows = await _societyDetailsRepository.GetQueryable()
            .Where(sd => sd.PropertyId.HasValue && candidateIds.Contains(sd.PropertyId.Value))
            .Select(sd => new { PropertyId = sd.PropertyId!.Value, sd.WingId })
            .ToListAsync(cancellationToken);

        var wingIds = societyRows.Where(s => s.WingId.HasValue).Select(s => s.WingId!.Value).Distinct().ToList();
        var wingNoById = wingIds.Count == 0
            ? new Dictionary<int, string>()
            : await _wingRepository.GetQueryable()
                .Where(w => wingIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WingNo, cancellationToken);

        var wingsByProperty = societyRows
            .GroupBy(s => s.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.WingId.HasValue && wingNoById.TryGetValue(s.WingId.Value, out var no)
                        ? no ?? string.Empty
                        : string.Empty)
                      .ToList());

        // Building-level guard: at least one candidate has PartitionNo == "" OR matching wing number.
        var hasQualifyingProperty = candidates.Any(c =>
        {
            var partition = c.PartitionNo ?? string.Empty;
            if (partition.Length == 0)
                return true;
            return wingsByProperty.TryGetValue(c.Id, out var wings)
                   && wings.Any(w => string.Equals(w, partition, StringComparison.Ordinal));
        });

        if (!hasQualifyingProperty)
            return 0;

        if (string.IsNullOrWhiteSpace(sourcePropertyNo))
            return 0;

        // (WardId, PropertyNo, Type) is the new unique key — insert at most one row.
        var alreadyExists = await _buildingPlanTypeRepository.GetQueryable()
            .AnyAsync(b => b.WardId == sourceWardId
                        && b.PropertyNo == sourcePropertyNo
                        && b.Type == typeToSetText
                        && !b.MarkedForDeletion, cancellationToken);

        if (alreadyExists)
            return 0;

        var insert = new BuildingPlanTypeEntity
        {
            WardId = sourceWardId,
            PropertyNo = sourcePropertyNo,
            Type = typeToSetText,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedBy = updatedBy,
            CreatedDate = now
        };

        await _buildingPlanTypeRepository.AddAsync(insert, cancellationToken);

        // SaveChanges happens at the surrounding transaction commit.
        return 1;
    }

    // ── Projection records (avoid materializing entities with NULL IsActive) ──
    private sealed record SourceDetail(
        int Id, bool? IsTaxable, int? FloorId, int? SubFloorId, string? ConstructionYear, string? AssessmentYear,
        int? ConstructionTypeId, int TypeOfUseId, int? SubTypeOfUseId, double? CarpetAreaSqMeter,
        double? CarpetAreaSqFeet, double? BuiltupAreaSqMeter, double? BuiltupAreaSqFeet, int? NoOfRooms, bool? IsRenter);

    private sealed record SourceSubmission(
        int Id, int PropertyDetailsId, double? LengthMtr, double? WidthMtr, double? AreaSqMtr, double? HeightMtr,
        double? Base1Mtr, double? Base2Mtr, int? NoOfRooms, double? TotalAreaSqMtr, string? Shape, string? RoomNo,
        bool OuterYesNo, int? RoomTypeId, string? SubmissionType, bool MinusYesNo);

    private sealed record SourceMinus(
        int RoomWiseSubmissionId, double? LengthMtr, double? WidthMtr, double? AreaSqMtr, double? HeightMtr,
        double? Base1Mtr, double? Base2Mtr, string? Shape, bool IsOffset);
}
