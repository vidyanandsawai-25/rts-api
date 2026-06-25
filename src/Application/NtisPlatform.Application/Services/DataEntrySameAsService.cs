using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.DataEntrySameAs;
using NtisPlatform.Application.Interfaces;
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
/// the ParkingTypeMaster table (the C# model has no TypeOfUseCategory, which the SP used).
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
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DataEntrySameAsResultDto> ExecuteAsync(
        DataEntrySameAsRequestDto request,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Normalize & validate filter type ──────────────────────────────
        var filterType = (request.FilterType ?? string.Empty).Trim().ToUpperInvariant();
        if (filterType != FilterParking && filterType != FilterTypewise && filterType != FilterPropertywise)
        {
            throw new ArgumentException(
                $"FilterType must be one of {FilterParking}, {FilterTypewise} or {FilterPropertywise}.");
        }
        var isParking = filterType == FilterParking;

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

        var droppedCount = (request.DestinationPropertyIds?.Count ?? 0) - destinationIds.Count;
        if (droppedCount > 0)
        {
            result.SkippedDestinations = droppedCount;
            result.Warnings.Add(
                $"{droppedCount} destination id(s) were dropped (self-reference, duplicate, or not found).");
        }

        if (destinationIds.Count == 0)
            throw new ArgumentException("No valid destination properties supplied.");

        // ── 4..9. Transactional work — the three filter modes act independently ──
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (filterType == FilterTypewise)
            {
                // TYPEWISE: only propagate PropertyMast.Type — no soft-delete, no copy.
                result.TypeUpdatedProperties = await StampTypeAsync(
                    source.Id, source.WardId, source.PropertyNo, source.PartitionNo, source.Type,
                    destinationIds, request.Type, updatedBy, cancellationToken);

                // Also upsert BuildingPlanType for the building-level properties (PartitionNo = WingNo OR '').
                var srcTypeValue = int.TryParse(source.Type, out var parsedSrcType) ? parsedSrcType : 0;
                var typeToSet = request.Type is >= 1 and <= 99 ? request.Type : srcTypeValue;
                result.BuildingPlanTypeInserted = await UpsertBuildingPlanTypeAsync(
                    source.WardId, source.PropertyNo, typeToSet, updatedBy, cancellationToken);
            }
            else
            {
                // PARKING / PROPERTYWISE: replace the destination's matching data-entry, then copy.
                // Parking TypeOfUse set (replaces the SP's TypeOfUseCategory join) — only needed here.
                var parkingTypeIds = await _parkingTypeRepository.GetQueryable()
                    .Select(p => p.TypeOfUseId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                await SoftDeleteDestinationDataEntryAsync(destinationIds, parkingTypeIds, isParking, updatedBy, cancellationToken);

                await CopyDataEntryAsync(request.SourcePropertyId, destinationIds, parkingTypeIds, isParking, updatedBy, result, cancellationToken);
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

        // Mirrors the source query: PropertyMast LEFT JOIN SocietyDetailsMast LEFT JOIN WingMaster.
        // The `PartitionNo != WingNo` predicate compares against a possibly-NULL wing number; as in SQL,
        // a NULL comparison is "unknown" and excludes the row (so rows without a matching wing drop out).
        var rows =
            from pm in _propertyRepository.GetQueryable()
            join sdm in _societyDetailsRepository.GetQueryable()
                on (int?)pm.Id equals sdm.PropertyId into sdmGroup
            from sdm in sdmGroup.DefaultIfEmpty()
            join wm in _wingRepository.GetQueryable()
                on sdm.WingId equals (int?)wm.Id into wmGroup
            from wm in wmGroup.DefaultIfEmpty()
            where pm.WardId == wardId
                  && pm.PropertyNo == propertyNo
                  && (!hasPartition || pm.PartitionNo == partitionNo)
                  && pm.PartitionNo != ""
                  && pm.PartitionNo != wm.WingNo
            select new DataEntrySameAsPropertyDto
            {
                PropertyId = pm.Id,
                WardId = pm.WardId,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                Type = pm.Type ?? string.Empty,
                WingName = sdm.WingName,
                FlatOrShopNo = pm.FlatOrShopNo
            };

        return await rows.ToListAsync(cancellationToken);
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

        result.PropertyDetailsCopied = newDetails.Count;

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

        result.RoomSubmissionsCopied = newSubmissions.Count;

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
            result.RoomMinusCopied = newMinus.Count;
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
        if (requestedType is >= 1 and <= 99)
        {
            var requestedTypeText = requestedType.ToString();
            await _propertyRepository.GetQueryable()
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
            return 0;

        await _propertyRepository.GetQueryable()
            .Where(p => qualifyingIds.Contains(p.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Type, typeToSetText)
                .SetProperty(p => p.UpdatedBy, updatedBy)
                .SetProperty(p => p.UpdatedDate, now),
                cancellationToken);

        return qualifyingIds.Count;
    }

    /// <summary>
    /// TYPEWISE only: upserts PTIS.BuildingPlanType for the building-level properties of the
    /// source's Ward+PropertyNo — those whose PartitionNo is empty OR equals their wing number.
    /// Matches by PropertyId: updates an existing (non-deleted) row's Type, else inserts a new row.
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

        // Building-level gate: PartitionNo == "" OR any wing number == PartitionNo.
        var qualifyingIds = candidates
            .Where(c =>
            {
                var partition = c.PartitionNo ?? string.Empty;
                if (partition.Length == 0)
                    return true;
                return wingsByProperty.TryGetValue(c.Id, out var wings)
                       && wings.Any(w => string.Equals(w, partition, StringComparison.Ordinal));
            })
            .Select(c => c.Id)
            .ToList();

        if (qualifyingIds.Count == 0)
            return 0;

        // (PropertyId, Type) is a unique pair — insert-only, never update.
        // A property may have several rows with different Type values, e.g. (552371,3) and (552371,4).
        var existingPairs = await _buildingPlanTypeRepository.GetQueryable()
            .Where(b => qualifyingIds.Contains(b.PropertyId) && !b.MarkedForDeletion)
            .Select(b => new { b.PropertyId, b.Type })
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<(int PropertyId, string? Type)>(
            existingPairs.Select(p => (p.PropertyId, p.Type)));

        // Insert one row per qualifying property whose (PropertyId, typeToSet) pair doesn't exist yet.
        var inserts = qualifyingIds
            .Where(propertyId => existingSet.Add((propertyId, typeToSetText)))
            .Select(propertyId => new BuildingPlanTypeEntity
            {
                PropertyId = propertyId,
                Type = typeToSetText,
                DocumentGuid = null,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = updatedBy,
                CreatedDate = now
            })
            .ToList();

        if (inserts.Count > 0)
            await _buildingPlanTypeRepository.AddRangeAsync(inserts, cancellationToken);

        // SaveChanges happens at the surrounding transaction commit.
        return inserts.Count;
    }

    // ── Projection records (avoid materializing entities with NULL IsActive) ──
    private sealed record SourceDetail(
        int Id, bool? IsTaxable, int FloorId, int? SubFloorId, string? ConstructionYear, string? AssessmentYear,
        int ConstructionTypeId, int TypeOfUseId, int? SubTypeOfUseId, double? CarpetAreaSqMeter,
        double? CarpetAreaSqFeet, double? BuiltupAreaSqMeter, double? BuiltupAreaSqFeet, int? NoOfRooms, bool? IsRenter);

    private sealed record SourceSubmission(
        int Id, int PropertyDetailsId, double? LengthMtr, double? WidthMtr, double? AreaSqMtr, double? HeightMtr,
        double? Base1Mtr, double? Base2Mtr, int? NoOfRooms, double? TotalAreaSqMtr, string? Shape, string? RoomNo,
        bool OuterYesNo, int? RoomTypeId, string? SubmissionType, bool MinusYesNo);

    private sealed record SourceMinus(
        int RoomWiseSubmissionId, double? LengthMtr, double? WidthMtr, double? AreaSqMtr, double? HeightMtr,
        double? Base1Mtr, double? Base2Mtr, string? Shape, bool IsOffset);
}
