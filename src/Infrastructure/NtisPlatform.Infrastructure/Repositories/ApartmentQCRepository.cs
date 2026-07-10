using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Specialized repository for the Apartment QC feature.
/// Handles complex multi-entity read queries and write-preparation operations.
/// Does NOT call SaveChanges — the Application service owns the unit of work.
/// Business assembly/aggregation logic lives in ApartmentQCService (Application layer).
/// </summary>
public sealed class ApartmentQCRepository : IApartmentQCRepository
{
    private readonly ApplicationDbContext _context;

    public ApartmentQCRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ──────────────────────────────── READ ────────────────────────────────

    public async Task<int> CountAsync(ApartmentQCQueryParameters query, CancellationToken cancellationToken = default)
    {
        var typeIds = await ResolveTypeIdsAsync(query.Type, cancellationToken);
        return await BuildJoinedQuery(query, typeIds).CountAsync(cancellationToken);
    }

    public async Task<ApartmentQCFetchedData> FetchPagedDataAsync(
        ApartmentQCQueryParameters query,
        int skip,
        int take,
        ApartmentQCResultType? resultType = null,
        CancellationToken cancellationToken = default)
    {
        var typeIds = await ResolveTypeIdsAsync(query.Type, cancellationToken);
        var raw = await BuildJoinedQuery(query, typeIds)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return ApartmentQCFetchedData.Empty;

        var properties = raw.Select(MapToPropertyData).ToList();
        var propertyIds = properties.Select(p => p.Id).ToList();
        var wardIds     = properties.Select(p => p.WardId).Distinct().ToList();

        var includeRv = resultType == ApartmentQCResultType.Rateable || resultType == ApartmentQCResultType.Dual;
        var includeCv = resultType == ApartmentQCResultType.Capital  || resultType == ApartmentQCResultType.Dual;

        return await FetchSupportingDataAsync(
            properties,
            propertyIds,
            wardIds,
            includeRvCalc: includeRv,
            includeCvCalc: includeCv,
            cancellationToken);
    }

    public async Task<ApartmentQCFetchedData> FetchByPropertyDataAsync(
        int propertyId,
        ApartmentQCResultType resultType,
        CancellationToken cancellationToken = default)
    {
        // No Type filter on the single-property view; pass null to skip pre-fetch.
        var raw = await BuildJoinedQuery(new ApartmentQCQueryParameters { PropertyId = propertyId }, typeIds: null)
            .FirstOrDefaultAsync(cancellationToken);

        if (raw == null)
            return ApartmentQCFetchedData.Empty;

        var property    = MapToPropertyData(raw);
        var propertyIds = new List<int> { property.Id };
        var wardIds     = new List<int> { property.WardId };

        var includeRv = resultType == ApartmentQCResultType.Rateable || resultType == ApartmentQCResultType.Dual;
        var includeCv = resultType == ApartmentQCResultType.Capital  || resultType == ApartmentQCResultType.Dual;

        return await FetchSupportingDataAsync(
            new List<ApartmentQCPropertyData> { property },
            propertyIds,
            wardIds,
            includeRv,
            includeCv,
            cancellationToken);
    }

    public async Task<ApartmentQCFilterOptionsDto> GetFilterOptionsAsync(
        ApartmentQCQueryParameters query,
        ApartmentQCFilterColumn?   column,
        CancellationToken cancellationToken = default)
    {
        var typeIds   = await ResolveTypeIdsAsync(query.Type, cancellationToken);
        var baseQuery = BuildJoinedQuery(query, typeIds);

        return column switch
        {
            ApartmentQCFilterColumn.Wing => new ApartmentQCFilterOptionsDto
            {
                Wings = await baseQuery
                    .Where(p => p.Wing != null)
                    .Select(p => p.Wing!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken)
            },
            ApartmentQCFilterColumn.ApartmentType => new ApartmentQCFilterOptionsDto
            {
                ApartmentTypes = await baseQuery
                    .Where(p => p.ApartmentType != null)
                    .Select(p => p.ApartmentType!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken)
            },
            ApartmentQCFilterColumn.FlatOrShopNo => new ApartmentQCFilterOptionsDto
            {
                FlatOrShopNos = await baseQuery
                    .Where(p => p.FlatOrShopNo != null)
                    .Select(p => p.FlatOrShopNo!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken)
            },
            ApartmentQCFilterColumn.PropertyType => new ApartmentQCFilterOptionsDto
            {
                PropertyTypes = await baseQuery
                    .Where(p => p.PropertyType != null)
                    .Select(p => p.PropertyType!.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken)
            },
            // null = no column specified — return all four in one round-trip.
            _ => await BuildAllOptionsAsync(baseQuery, cancellationToken)
        };
    }

    private static async Task<ApartmentQCFilterOptionsDto> BuildAllOptionsAsync(
        IQueryable<JoinedProperty> baseQuery,
        CancellationToken cancellationToken)
    {
        var raw = await baseQuery
            .Select(p => new FilterOptionRow(p.Wing, p.ApartmentType, p.FlatOrShopNo, p.PropertyType))
            .ToListAsync(cancellationToken);

        return new ApartmentQCFilterOptionsDto
        {
            Wings          = raw.Where(x => x.Wing          != null).Select(x => x.Wing!).Distinct().Order().ToList(),
            ApartmentTypes = raw.Where(x => x.ApartmentType != null).Select(x => x.ApartmentType!).Distinct().Order().ToList(),
            FlatOrShopNos  = raw.Where(x => x.FlatOrShopNo  != null).Select(x => x.FlatOrShopNo!).Distinct().Order().ToList(),
            PropertyTypes  = raw.Where(x => x.PropertyType  != null).Select(x => x.PropertyType!.Value).Distinct().Order().ToList()
        };
    }

    private sealed record FilterOptionRow(
        string? Wing,
        string? ApartmentType,
        string? FlatOrShopNo,
        int?    PropertyType);

    public async Task<OldPropertyLookupDto?> GetOldPropertyDataByNoAsync(
        string oldPropertyNo,
        CancellationToken cancellationToken = default)
    {
        var trimmed = oldPropertyNo.Trim();
        return await (
            from pmo in _context.PropertyMastOld.AsNoTracking()
            join ctm in _context.ConstructionTypeEntity.AsNoTracking()
                on pmo.OldConstructionTypeOfUseId equals ctm.ConstructionCode into ctj
            from ctm in ctj.DefaultIfEmpty()
            where pmo.OldPropertyNo == trimmed
            select new OldPropertyLookupDto
            {
                OldPropertyNo       = pmo.OldPropertyNo,
                OldConstructionArea = (decimal?)pmo.OldConstructionArea,
                OldRV               = (decimal?)pmo.OldRV,
                OldTotalTax         = (decimal?)pmo.OldTotalTax,
                OldUseType          = pmo.OldUseType,
                OldConstructionYear = pmo.OldConstructionYear,
                OldConstructionType = ctm != null ? ctm.Description : null,
                OldCSN = pmo != null ? pmo.OldCSN : null
            }
        ).FirstOrDefaultAsync(cancellationToken);
    }

    // ──────────────────────── FK EXISTENCE CHECKS ──────────────────────────

    public Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _context.PropertyMast
            .AsNoTracking()
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

    public async Task<HashSet<int>> GetExistingFloorIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new HashSet<int>();
        var existing = await _context.FloorEntity.AsNoTracking()
            .Where(e => idList.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        return new HashSet<int>(existing);
    }

    public async Task<HashSet<int>> GetExistingConstructionTypeIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new HashSet<int>();
        var existing = await _context.ConstructionTypeEntity.AsNoTracking()
            .Where(e => idList.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        return new HashSet<int>(existing);
    }

    public async Task<HashSet<int>> GetExistingTypeOfUseIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new HashSet<int>();
        var existing = await _context.TypeOfUse.AsNoTracking()
            .Where(e => idList.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        return new HashSet<int>(existing);
    }

    public async Task<HashSet<int>> GetExistingSubTypeOfUseIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new HashSet<int>();
        var existing = await _context.SubTypeOfUse.AsNoTracking()
            .Where(e => idList.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        return new HashSet<int>(existing);
    }

    // ──────────────────── WRITE-PREPARATION (no SaveChanges) ────────────────────

    public async Task<Dictionary<int, PropertyDetailsEntity>> GetTrackedDetailsForUpdateAsync(
        int propertyId,
        IEnumerable<int> detailIds,
        CancellationToken cancellationToken = default)
    {
        var idList = detailIds.Distinct().ToList();

        var entities = await _context.PropertyDetails
            .Where(x => x.PropertyId == propertyId
                     && idList.Contains(x.Id)
                     && x.IsActive
                     && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        return entities.ToDictionary(d => d.Id);
    }

    public void ApplyDetailPatches(
        Dictionary<int, PropertyDetailsEntity> detailsById,
        IEnumerable<UpdateApartmentQCDetailsDto> dtos,
        int updatedBy)
    {
        var now = DateTime.Now;

        foreach (var dto in dtos)
        {
            if (!detailsById.TryGetValue(dto.DetailId, out var detail))
                continue;

            if (dto.FloorId.HasValue)            detail.FloorId            = dto.FloorId.Value;
            if (dto.ConstructionTypeId.HasValue) detail.ConstructionTypeId = dto.ConstructionTypeId.Value;
            if (dto.TypeOfUseId.HasValue)        detail.TypeOfUseId        = dto.TypeOfUseId.Value;
            if (dto.SubTypeOfUseId.HasValue)   detail.SubTypeOfUseId = dto.SubTypeOfUseId.Value;
            else if (dto.TypeOfUseId.HasValue) detail.SubTypeOfUseId = null;
            if (dto.ConstructionYear != null)    detail.ConstructionYear   = dto.ConstructionYear;
            if (dto.AssessmentYear != null)      detail.AssessmentYear     = dto.AssessmentYear;

            detail.UpdatedBy   = updatedBy;
            detail.UpdatedDate = now;
        }
    }

    public async Task<BasicDetailsPatchOutcome> PrepareBasicDetailsPatchAsync(
        int propertyId,
        UpdateApartmentQCBasicDetailsDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (property == null)
            return BasicDetailsPatchOutcome.PropertyNotFound;

        var now = DateTime.Now;

        // 1. PropertyMast
        if (dto.OwnerName != null)      property.OwnerName      = dto.OwnerName;
        if (dto.OccupierName != null)   property.OccupierName   = dto.OccupierName;
        if (dto.MobileNo != null)       property.MobileNo       = dto.MobileNo;
        if (dto.EmailId != null)        property.EmailId        = dto.EmailId;
        if (dto.FlatOrShopNo != null)   property.FlatOrShopNo   = dto.FlatOrShopNo;
        if (dto.FlatOrShopName != null) property.FlatOrShopName = dto.FlatOrShopName;
        if (dto.PropertyType.HasValue)  property.PropertyTypeId = dto.PropertyType;

        property.UpdatedBy   = updatedBy;
        property.UpdatedDate = now;

        // 2. SocietyDetailsMast — optional Wing
        if (dto.Wing != null)
        {
            var society = await _context.SocietyDetailsMast
                .FirstOrDefaultAsync(
                    x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion,
                    cancellationToken);
            if (society != null)
            {
                society.WingName    = dto.Wing;
                society.UpdatedBy   = updatedBy;
                society.UpdatedDate = now;
            }
        }

        // 3. Re-link PropertyMast to the PropertyMastOld row that owns dto.OldPropertyNo.
        //    We do NOT mutate the OldPropertyNo string on the existing row — that would
        //    corrupt a shared reference. Instead we find the target row's Id and update
        //    the FK on PropertyMast (PropertyMastOldId) to point at it.
        if (dto.OldPropertyNo != null)
        {
            var trimmed  = dto.OldPropertyNo.Trim();
            var targetId = await _context.PropertyMastOld
                .AsNoTracking()
                .Where(o => o.OldPropertyNo == trimmed)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!targetId.HasValue)
                return BasicDetailsPatchOutcome.OldPropertyNoNotFound;

            property.PropertyMastOldId = targetId.Value;
            // UpdatedBy/UpdatedDate already stamped above; no extra stamp needed.
        }

        // 4. PropertyMastDetails — BHK on the latest active assessment row
        if (dto.BHK != null)
        {
            var assessment = await _context.PropertyMastDetails
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);
            if (assessment != null)
            {
                assessment.BHK         = dto.BHK;
                assessment.UpdatedBy   = updatedBy;
                assessment.UpdatedDate = now;
            }
        }

        // 5. RenterMast — patch the latest active renter per PropertyDetails row
        if (dto.RenterName != null)
        {
            var propertyDetailIds = await _context.PropertyDetails
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (propertyDetailIds.Count > 0)
            {
                var latestRenterIds = await _context.RenterMast
                    .Where(r => propertyDetailIds.Contains(r.PropertyDetailsId)
                             && r.IsActive
                             && !r.MarkedForDeletion)
                    .GroupBy(r => r.PropertyDetailsId)
                    .Select(g => g.OrderByDescending(r => r.CreatedDate).First().Id)
                    .ToListAsync(cancellationToken);

                if (latestRenterIds.Count > 0)
                {
                    var renters = await _context.RenterMast
                        .Where(r => latestRenterIds.Contains(r.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var renter in renters)
                    {
                        renter.RenterName  = dto.RenterName;
                        renter.UpdatedBy   = updatedBy;
                        renter.UpdatedDate = now;
                    }
                }
            }
        }

        return BasicDetailsPatchOutcome.Success;
    }

    // ──────────────────────── PRIVATE QUERY BUILDERS ────────────────────────

    /// <summary>
    /// Pre-fetches TypeOfUse IDs matching <paramref name="type"/> so the caller can pass a simple
    /// IN-list predicate to <see cref="BuildJoinedQuery"/> instead of a double-nested correlated EXISTS.
    /// Returns null when <paramref name="type"/> is blank (no filter needed).
    /// Returns an empty list when the type string exists but matches no TypeOfUse rows
    /// (BuildJoinedQuery will produce zero results, which is correct).
    /// </summary>
    private async Task<IReadOnlyList<int>?> ResolveTypeIdsAsync(
        string? type,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;

        return await _context.TypeOfUse
            .AsNoTracking()
            .Where(tu => tu.Type == type.Trim())
            .Select(tu => tu.Id)
            .ToListAsync(cancellationToken);
    }

    /// <param name="query">Active filter/sort/page parameters.</param>
    /// <param name="typeIds">
    /// Pre-fetched TypeOfUse IDs for the Type filter.
    /// Null  = no Type filter applied.
    /// Empty = Type filter specified but no matching TypeOfUse rows exist (returns zero results).
    /// </param>
    private IQueryable<JoinedProperty> BuildJoinedQuery(
        ApartmentQCQueryParameters query,
        IReadOnlyList<int>?        typeIds = null)
    {
        var baseQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(pm => pm.IsActive && !pm.MarkedForDeletion)
            .Where(pm => _context.PropertyCategoryMaster
                .Any(pcm => pcm.Id == pm.CategoryId
                         && PropertyCategoryConstants.ApartmentCategoryNames.Contains(pcm.PropertyCategoryName)));

        if (query.WardId.HasValue)
            baseQuery = baseQuery.Where(pm => pm.WardId == query.WardId);

        if (!string.IsNullOrWhiteSpace(query.PropertyNo))
        {
            var trimmedPropertyNo = query.PropertyNo.Trim();
            baseQuery = baseQuery.Where(pm => pm.PropertyNo != null && pm.PropertyNo==trimmedPropertyNo);
        }


        var totalwingList = _context.WingEntity.AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => d.WingNo)
            .ToList();  

        baseQuery = baseQuery.Where(pm =>
            pm.PartitionNo != null &&
            pm.PartitionNo != "" &&
            !totalwingList.Contains(pm.PartitionNo));  

        if (!string.IsNullOrWhiteSpace(query.PartitionNo))
        {
            var trimmedPartitionNo = query.PartitionNo.Trim();  

            if (totalwingList.Contains(trimmedPartitionNo))     
            {
                baseQuery = baseQuery.Where(pm =>
                    pm.PartitionNo != null &&
                    pm.PartitionNo.Contains(trimmedPartitionNo));
            }
            else
            {
                baseQuery = baseQuery.Where(pm =>
                    pm.PartitionNo == trimmedPartitionNo);
            }
        }

        if (query.PropertyId.HasValue)
            baseQuery = baseQuery.Where(pm => pm.Id == query.PropertyId);

        if (!string.IsNullOrWhiteSpace(query.FlatOrShopNo))
        {
            var trimmed = query.FlatOrShopNo.Trim();
            baseQuery = baseQuery.Where(pm => pm.FlatOrShopNo != null && pm.FlatOrShopNo.Contains(trimmed));
        }

        if (!string.IsNullOrWhiteSpace(query.ApartmentType))
        {
            var trimmed = query.ApartmentType.Trim();
            baseQuery = baseQuery.Where(pm => pm.Type == trimmed);
        }

        if (query.PropertyType.HasValue)
            baseQuery = baseQuery.Where(pm => pm.PropertyTypeId == query.PropertyType);


        // Type filter: use pre-fetched IDs (single-level EXISTS + IN) rather than a double-nested
        // correlated EXISTS that would resolve TypeOfUse on every candidate property row.
        if (typeIds != null)
        {
            if (typeIds.Count == 0)
            {
                // The requested Type value has no matching TypeOfUse rows — return nothing.
                baseQuery = baseQuery.Where(_ => false);
            }
            else
            {
                baseQuery = baseQuery.Where(pm =>
                    _context.PropertyDetails.Any(pd =>
                        pd.PropertyId == pm.Id
                        && pd.IsActive && !pd.MarkedForDeletion
                        && typeIds.Contains(pd.TypeOfUseId)));
            }
        }
        // Use correlated subqueries to avoid Cartesian explosion from PropertyMastDetails
        // and SocietyDetailsMast having multiple rows per property.
       

        var joined =
            from pm in baseQuery
            join ptm in _context.PropertyTypeMasters.AsNoTracking()
                on pm.PropertyTypeId equals ptm.Id
            let bhk = _context.PropertyMastDetails.AsNoTracking()
                .Where(d => d.PropertyId == pm.Id && d.IsActive && !d.MarkedForDeletion)
                .OrderByDescending(d => d.CreatedDate)
                .Select(d => d.BHK)
                .FirstOrDefault()
            let wing = _context.SocietyDetailsMast.AsNoTracking()
                .Where(s => s.PropertyId == pm.Id && s.IsActive && !s.MarkedForDeletion)
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => s.WingName)
                .FirstOrDefault()
            select new JoinedProperty
            {
                Id                    = pm.Id,
                TaxZoneId             = pm.TaxZoneId,
                WardId                = pm.WardId,
                PropertyNo            = pm.PropertyNo,
                PartitionNo           = pm.PartitionNo,
                MobileNo              = pm.MobileNo,
                EmailId               = pm.EmailId,
                FlatOrShopNo          = pm.FlatOrShopNo,
                FlatOrShopName        = pm.FlatOrShopName,
                FlatOrShopNoEnglish   = pm.FlatOrShopNoEnglish,
                FlatOrShopNameEnglish = pm.FlatOrShopNameEnglish,
                OwnerName             = pm.OwnerName,
                OwnerNameEnglish      = pm.OwnerNameEnglish,
                OccupierName          = pm.OccupierName,
                OccupierNameEnglish   = pm.OccupierNameEnglish,
                PartType              = ptm.PartType,
                PropertyType          = ptm.Id,
                PropertyTypeName      = ptm.PropertyDescription,
                BHK                   = bhk,
                Wing                  = wing,
                ApartmentType         = pm.Type
            };

        if (!string.IsNullOrWhiteSpace(query.PartType))
        {
            var trimmedPartType = query.PartType.Trim();
            joined = joined.Where(x => x.PartType != null && x.PartType == trimmedPartType);
        }

        if (!string.IsNullOrWhiteSpace(query.Wing))
        {
            var trimmedWing = query.Wing.Trim();
            joined = joined.Where(x => x.Wing != null && x.Wing.Contains(trimmedWing));
        }
        // Step 5: Apply sorting
        var isDescending = query.SortOrder?.ToLower() == "desc";
        var sortBy = query.SortBy?.ToLower();

        joined = sortBy switch
        {
            "id" => isDescending ? joined.OrderByDescending(x => x.Id) : joined.OrderBy(x => x.Id),
            "taxzoneid" => isDescending ? joined.OrderByDescending(x => x.TaxZoneId) : joined.OrderBy(x => x.TaxZoneId),
            "wardid" => isDescending ? joined.OrderByDescending(x => x.WardId) : joined.OrderBy(x => x.WardId),
            "propertyno" => joined.OrderByNatural(x => x.PropertyNo, isDescending),
            "partitionno" => joined.OrderByNatural(x => x.PartitionNo, isDescending),
            "mobileno" => joined.OrderByNatural(x => x.MobileNo, isDescending),
            "emailid" => joined.OrderByNatural(x => x.EmailId, isDescending),
            "flatorshopno" => joined.OrderByNatural(x => x.FlatOrShopNo, isDescending),
            "flatorshopname" => joined.OrderByNatural(x => x.FlatOrShopName, isDescending),
            "flatorshopnoenglish" => joined.OrderByNatural(x => x.FlatOrShopNoEnglish, isDescending),
            "flatorshopnameenglish" => joined.OrderByNatural(x => x.FlatOrShopNameEnglish, isDescending),
            "ownername" => joined.OrderByNatural(x => x.OwnerName, isDescending),
            "ownernameenglish" => joined.OrderByNatural(x => x.OwnerNameEnglish, isDescending),
            "occupiername" => joined.OrderByNatural(x => x.OccupierName, isDescending),
            "occupiernameenglish" => joined.OrderByNatural(x => x.OccupierNameEnglish, isDescending),
            "parttype" => joined.OrderByNatural(x => x.PartType, isDescending),
            "propertytype" => joined.OrderByNatural(x => Convert.ToString(x.PropertyType), isDescending),
            "propertytypename" => joined.OrderByNatural(x => x.PropertyTypeName, isDescending),
            "bhk" => joined.OrderByNatural(x => x.BHK, isDescending),
            "wing" => joined.OrderByNatural(x => x.Wing, isDescending),
            "apartmenttype" => joined.OrderByNatural(x => x.ApartmentType, isDescending),
            _ => joined.OrderBy(x => x.Id)
        };

        return joined;
    }

    private async Task<ApartmentQCFetchedData> FetchSupportingDataAsync(
        IReadOnlyList<ApartmentQCPropertyData> properties,
        IReadOnlyList<int> propertyIds,
        IReadOnlyList<int> wardIds,
        bool includeRvCalc,
        bool includeCvCalc,
        CancellationToken cancellationToken)
    {
        var oldDataList = await (
            from pm in _context.PropertyMast.AsNoTracking()
            join pmo in _context.PropertyMastOld.AsNoTracking()
                 on pm.PropertyMastOldId equals pmo.Id into a
            from pmo in a.DefaultIfEmpty()
            join ctm in _context.ConstructionTypeEntity.AsNoTracking()
                on pmo.OldConstructionTypeOfUseId equals ctm.ConstructionCode into ctj
            from ctm in ctj.DefaultIfEmpty()
            where propertyIds.Contains(pm.Id) && pm.IsActive && !pm.MarkedForDeletion
            select new OldDataRow(
                pm.Id,
                pmo.OldPropertyNo,
                (decimal?)pmo.OldConstructionArea,
                (decimal?)pmo.OldRV,
                (decimal?)pmo.OldTotalTax,
                pmo.OldUseType,
                pmo.OldConstructionYear,
                ctm != null ? ctm.Description : null,
                pmo.OldCSN))
            .ToListAsync(cancellationToken);

        var wardZoneList = await (
            from wm in _context.WardMaster.AsNoTracking()
            join zm in _context.ZoneMaster.AsNoTracking() on wm.ZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()
            where wardIds.Contains(wm.Id) && wm.IsActive
            select new WardZoneRow(wm.Id, wm.WardNo, zm != null ? zm.Description : null))
            .ToListAsync(cancellationToken);

        var detailsList = await (
            from pd in _context.PropertyDetails.AsNoTracking()
                .Where(pd => propertyIds.Contains(pd.PropertyId) && pd.IsActive && !pd.MarkedForDeletion)
            join f   in _context.FloorEntity.AsNoTracking()           on pd.FloorId          equals f.Id  into fj
            from f   in fj.DefaultIfEmpty()
            join sf  in _context.SubFloorEntity.AsNoTracking()        on pd.SubFloorId        equals sf.Id into sfj
            from sf  in sfj.DefaultIfEmpty()
            join tu  in _context.TypeOfUse.AsNoTracking()             on pd.TypeOfUseId       equals tu.Id into tuj
            from tu  in tuj.DefaultIfEmpty()
            join stu in _context.SubTypeOfUse.AsNoTracking()          on pd.SubTypeOfUseId    equals stu.Id into stuj
            from stu in stuj.DefaultIfEmpty()
            join ct  in _context.ConstructionTypeEntity.AsNoTracking() on pd.ConstructionTypeId equals ct.Id into ctj
            from ct  in ctj.DefaultIfEmpty()
            select new DetailRow
            {
                Id                  = pd.Id,
                PropertyId          = pd.PropertyId,
                ConstructionYear    = pd.ConstructionYear,
                AssessmentYear      = pd.AssessmentYear,
                CarpetAreaSqMeter   = (decimal?)pd.CarpetAreaSqMeter,
                CarpetAreaSqFeet    = (decimal?)pd.CarpetAreaSqFeet,
                BuiltupAreaSqMeter  = (decimal?)pd.BuiltupAreaSqMeter,
                BuiltupAreaSqFeet   = (decimal?)pd.BuiltupAreaSqFeet,
                Floor               = f   != null ? f.Description                            : null,
                SubFloor            = sf  != null ? sf.Description                           : null,
                TypeOfUse           = tu  != null ? tu.TypeOfUseCode + "-" + tu.Description  : null,
                Type                = tu  != null ? tu.Type                                   : null,
                ConstructionType    = ct  != null ? ct.Description                       : null,
                SubTypeOfUse        = stu != null ? stu.Description                           : null,
                NoOfRooms           = pd.NoOfRooms
            }).ToListAsync(cancellationToken);

        var propertyDetailIds = detailsList.Select(d => d.Id).Distinct().ToList();

        var occupancyList = await _context.PropertyOccupancyDetails
            .AsNoTracking()
            .Where(po => propertyDetailIds.Contains(po.PropertyDetailId) && po.IsActive && !po.MarkedForDeletion)
            .GroupBy(po => po.PropertyDetailId)
            .Select(g => new OccupancyRow(
                g.Key,
                g.OrderByDescending(po => po.OccupancyDate).Select(po => po.OccupancyDate).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var renterList = await _context.RenterMast
            .AsNoTracking()
            .Where(rm => propertyDetailIds.Contains(rm.PropertyDetailsId) && rm.IsActive && !rm.MarkedForDeletion)
            .GroupBy(rm => rm.PropertyDetailsId)
            .Select(g => g
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new RenterRow(r.PropertyDetailsId, r.RenterName, r.RenterNameEnglish, (decimal?)r.FinalYearlyRent, (decimal?)r.RentMonthly))
                .FirstOrDefault())
            .ToListAsync(cancellationToken);

        // Pre-fetch the latest finance/pending year IDs as scalar constants.
        var financeYearId = await _context.YearMaster
                .AsNoTracking()
                .Where(ym => ym.IsActive)
                .Select(ym => ym.Id)
                .FirstOrDefaultAsync();

        var transMastList = await _context.TransMast
            .AsNoTracking()
            .Where(x => propertyIds.Contains(x.PropertyId) && x.FinanceYearId == financeYearId && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(g => new TransMastRow(
                g.Key,
                g.Max(x => (decimal?)x.RVorCVValue),
                g.Sum(x => (decimal?)x.TaxAmount) ?? 0m))
            .ToListAsync(cancellationToken);

        var transMastCVList = await _context.TransMastCV
            .AsNoTracking()
            .Where(x => propertyIds.Contains(x.PropertyId) && x.FinanceYearId == financeYearId && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(g => new TransMastCVRow(
                g.Key,
                g.Max(x => (decimal?)x.CapitalValue),
                g.Sum(x => (decimal?)x.TaxAmount) ?? 0m))
            .ToListAsync(cancellationToken);

        var transMastRVList = await _context.TransMastRV
            .AsNoTracking()
            .Where(x => propertyIds.Contains(x.PropertyId) && x.FinanceYearId == financeYearId && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(g => new TransMastRVRow(
                g.Key,
                g.Max(x => (decimal?)x.RateableValue),
                g.Sum(x => (decimal?)x.TaxAmount) ?? 0m))
            .ToListAsync(cancellationToken);

        var taxPendingList = await _context.TaxPendingDetails
            .AsNoTracking()
            .Where(x => propertyIds.Contains(x.PropertyId) && x.PendingYearId == financeYearId && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(g => new TaxPendingRow(g.Key, g.Sum(x => (decimal?)x.PendingAmount) ?? 0m))
            .ToListAsync(cancellationToken);

        var taxPendingCVList = await _context.TaxPendingDetailsCV
            .AsNoTracking()
            .Where(x => propertyIds.Contains(x.PropertyId) && x.PendingYearId == financeYearId && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(g => new TaxPendingRow(g.Key, g.Sum(x => (decimal?)x.PendingAmount) ?? 0m))
            .ToListAsync(cancellationToken);

        var taxPendingRVList = await _context.TaxPendingDetailsRV
            .AsNoTracking()
            .Where(x => propertyIds.Contains(x.PropertyId) && x.PendingYearId == financeYearId && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(g => new TaxPendingRow(g.Key, g.Sum(x => (decimal?)x.PendingAmount) ?? 0m))
            .ToListAsync(cancellationToken);

        Dictionary<int, ApartmentQCRvCalcData> rvCalc;
        if (includeRvCalc && propertyDetailIds.Count > 0)
        {
            var rvCalcList = await _context.PropertyTaxCalculationRVResults
                .AsNoTracking()
                .Where(x => propertyDetailIds.Contains(x.PropertyDetailsId) && x.IsActive && !x.MarkedForDeletion)
                .GroupBy(x => x.PropertyDetailsId)
                .Select(g => new ApartmentQCRvCalcData(
                    g.Key,
                    g.Max(x => x.YearlyRent != null ? (decimal?)Convert.ToDecimal(x.YearlyRent) : null),
                    g.Max(x => x.MonthlyRate != null ? (decimal?)Convert.ToDecimal(x.MonthlyRate) : null),
                    g.Max(x => x.YearlyRate != null ? (decimal?)Convert.ToDecimal(x.YearlyRate) : null),
                    g.Max(x => x.Depreciation != null ? (decimal?)Convert.ToDecimal(x.Depreciation) : null),
                    g.Max(x => x.AnnualRentalValue != null ? (decimal?)Convert.ToDecimal(x.AnnualRentalValue) : null),
                    g.Max(x => x.Maintenance != null ? (decimal?)Convert.ToDecimal(x.Maintenance) : null),
                    g.Max(x => x.RateableValue != null ? (decimal?)Convert.ToDecimal(x.RateableValue) : null)))
                .ToListAsync(cancellationToken);
            rvCalc = rvCalcList.ToDictionary(x => x.PropertyDetailsId);
        }
        else
        {
            rvCalc = new Dictionary<int, ApartmentQCRvCalcData>();
        }

        Dictionary<int, ApartmentQCCvCalcData> cvCalc;
        if (includeCvCalc && propertyDetailIds.Count > 0)
        {
            var cvCalcList = await (
                from pd in _context.PropertyTaxCalculationCVResults.AsNoTracking()
                join f  in _context.FloorFactorCVMasters.AsNoTracking()   on pd.FloorFactorCVId equals f.Id  into ffm
                from floorFactor   in ffm.DefaultIfEmpty()
                join fa in _context.AgeFactorCVMasters.AsNoTracking()     on pd.AgeFactorCVId equals fa.Id into afm
                from ageFactor     in afm.DefaultIfEmpty()
                join fn in _context.NatureFactorCVMasters.AsNoTracking()  on pd.NatureFactorCVId equals fn.Id into nfm
                from natureFactor  in nfm.DefaultIfEmpty()
                join fu in _context.UseFactorCVMaster.AsNoTracking()      on pd.UseFactorCVId equals fu.Id into ufm
                from useFactor     in ufm.DefaultIfEmpty()
                where propertyDetailIds.Contains(pd.PropertyDetailsId) && pd.IsActive == true && pd.MarkedForDeletion != true
                group new { pd, floorFactor, ageFactor, natureFactor, useFactor }
                    by pd.PropertyDetailsId into grp
                select new ApartmentQCCvCalcData(
                    grp.Key,
                    grp.Max(x => (decimal?)x.pd.BaseValue),
                    grp.Max(x => x.floorFactor.FactorWithLift),
                    grp.Max(x => x.ageFactor.Factor),
                    grp.Max(x => x.natureFactor.Factor),
                    grp.Max(x => x.useFactor.Factor),
                    grp.Max(x => x.pd.CapitalValue),
                    grp.Max(x => x.pd.FloorFactorCVId),
                    grp.Max(x => x.pd.AgeFactorCVId),
                    grp.Max(x => x.pd.NatureFactorCVId),
                    grp.Max(x => x.pd.UseFactorCVId),
                    grp.Max(x => x.pd.RateCVMasterId))
            ).ToListAsync(cancellationToken);
            cvCalc = cvCalcList.ToDictionary(x => x.PropertyDetailsId);
        }
        else
        {
            cvCalc = new Dictionary<int, ApartmentQCCvCalcData>();
        }

        return new ApartmentQCFetchedData
        {
            Properties  = properties,
            OldData     = oldDataList.ToDictionary(x => x.Id,
                              x => new ApartmentQCOldPropertyData(x.Id, x.OldPropertyNo, x.OldConstructionArea,
                                       x.OldRV, x.OldTotalTax, x.OldUseType, x.OldConstructionYear, x.OldConstructionType, x.OldCSN)),
            WardZones   = wardZoneList.ToDictionary(x => x.Id,
                              x => new ApartmentQCWardData(x.Id, x.WardNo, x.ZoneNo)),
            Details     = detailsList.Select(d => new ApartmentQCDetailData
                          {
                              Id                  = d.Id,
                              PropertyId          = d.PropertyId,
                              ConstructionYear    = d.ConstructionYear,
                              AssessmentYear      = d.AssessmentYear,
                              CarpetAreaSqMeter   = d.CarpetAreaSqMeter,
                              CarpetAreaSqFeet    = d.CarpetAreaSqFeet,
                              BuiltupAreaSqMeter  = d.BuiltupAreaSqMeter,
                              BuiltupAreaSqFeet   = d.BuiltupAreaSqFeet,
                              Floor               = d.Floor,
                              SubFloor            = d.SubFloor,
                              TypeOfUse           = d.TypeOfUse,
                              Type                = d.Type,
                              ConstructionType    = d.ConstructionType,
                              SubTypeOfUse        = d.SubTypeOfUse,
                              NoOfRooms           = d.NoOfRooms
                          }).ToList(),
            Occupancies = occupancyList.ToDictionary(x => x.PropertyDetailId,
                              x => new ApartmentQCOccupancyData(x.PropertyDetailId, x.OccupancyDate)),
            Renters     = renterList
                              .Where(r => r != null)
                              .ToDictionary(r => r!.PropertyDetailsId,
                                  r => new ApartmentQCRenterData(r!.PropertyDetailsId, r.RenterName, r.RenterNameEnglish, r.FinalYearlyRent, r.RentMonthly)),
            Tm          = transMastList.ToDictionary(x => x.PropertyId,
                              x => new ApartmentQCTransactionData(x.PropertyId, x.RVorCVValue, x.TmTaxAmount)),
            Tmcv        = transMastCVList.ToDictionary(x => x.PropertyId,
                              x => new ApartmentQCTransactionCVData(x.PropertyId, x.CapitalValue, x.TmcvTaxAmount)),
            Tmrv        = transMastRVList.ToDictionary(x => x.PropertyId,
                              x => new ApartmentQCTransactionRVData(x.PropertyId, x.RateableValue, x.TmrvTaxAmount)),
            Tp          = taxPendingList.ToDictionary(x => x.PropertyId,
                              x => new ApartmentQCTaxPendingData(x.PropertyId, x.PendingAmount)),
            Tpcv        = taxPendingCVList.ToDictionary(x => x.PropertyId,
                              x => new ApartmentQCTaxPendingData(x.PropertyId, x.PendingAmount)),
            Tprv        = taxPendingRVList.ToDictionary(x => x.PropertyId,
                              x => new ApartmentQCTaxPendingData(x.PropertyId, x.PendingAmount)),
            RvCalc      = rvCalc,
            CvCalc      = cvCalc
        };
    }

    // ──────────────────────── PRIVATE MAPPING ────────────────────────────────

    private static ApartmentQCPropertyData MapToPropertyData(JoinedProperty p) => new()
    {
        Id                    = p.Id,
        TaxZoneId             = p.TaxZoneId,
        WardId                = p.WardId,
        PropertyNo            = p.PropertyNo,
        PartitionNo           = p.PartitionNo,
        MobileNo              = p.MobileNo,
        EmailId               = p.EmailId,
        FlatOrShopNo          = p.FlatOrShopNo,
        FlatOrShopName        = p.FlatOrShopName,
        FlatOrShopNoEnglish   = p.FlatOrShopNoEnglish,
        FlatOrShopNameEnglish = p.FlatOrShopNameEnglish,
        OwnerName             = p.OwnerName,
        OwnerNameEnglish      = p.OwnerNameEnglish,
        OccupierName          = p.OccupierName,
        OccupierNameEnglish   = p.OccupierNameEnglish,
        PartType              = p.PartType,
        PropertyType          = p.PropertyType,
        PropertyTypeName      = p.PropertyTypeName,
        BHK                   = p.BHK,
        Wing                  = p.Wing,
        ApartmentType         = p.ApartmentType
    };

    // ──────────────────────── PRIVATE EF PROJECTION TYPES ────────────────────────
    // These types are shaped for EF LINQ translation and must stay in Infrastructure.

    private sealed class JoinedProperty
    {
        public int     Id                    { get; set; }
        public int?    TaxZoneId             { get; set; }
        public int     WardId                { get; set; }
        public string? PropertyNo            { get; set; }
        public string? PartitionNo           { get; set; }
        public string? MobileNo              { get; set; }
        public string? EmailId               { get; set; }
        public string? FlatOrShopNo          { get; set; }
        public string? FlatOrShopName        { get; set; }
        public string? FlatOrShopNoEnglish   { get; set; }
        public string? FlatOrShopNameEnglish { get; set; }
        public string? OwnerName             { get; set; }
        public string? OwnerNameEnglish      { get; set; }
        public string? OccupierName          { get; set; }
        public string? OccupierNameEnglish   { get; set; }
        public string? PartType              { get; set; }
        public int?    PropertyType          { get; set; }
        public string? PropertyTypeName      { get; set; }
        public string? BHK                  { get; set; }
        public string? Wing                 { get; set; }
        public string? ApartmentType        { get; set; }
    }

    private sealed class DetailRow
    {
        public int      Id                 { get; set; }
        public int      PropertyId         { get; set; }
        public string?  ConstructionYear   { get; set; }
        public string?  AssessmentYear     { get; set; }
        public decimal? CarpetAreaSqMeter  { get; set; }
        public decimal? CarpetAreaSqFeet   { get; set; }
        public decimal? BuiltupAreaSqMeter { get; set; }
        public decimal? BuiltupAreaSqFeet  { get; set; }
        public string?  Floor              { get; set; }
        public string?  SubFloor           { get; set; }
        public string?  TypeOfUse          { get; set; }
        public string?  Type               { get; set; }
        public string?  ConstructionType   { get; set; }
        public string?  SubTypeOfUse       { get; set; }
        public int?     NoOfRooms          { get; set; }
    }

    private sealed record OldDataRow(
        int      Id,
        string?  OldPropertyNo,
        decimal? OldConstructionArea,
        decimal? OldRV,
        decimal? OldTotalTax,
        string?  OldUseType,
        string?  OldConstructionYear,
        string?  OldConstructionType,
        string?  OldCSN);

    private sealed record WardZoneRow(int Id, string? WardNo, string? ZoneNo);
    private sealed record OccupancyRow(int PropertyDetailId, DateTime? OccupancyDate);
    private sealed record RenterRow(int PropertyDetailsId, string? RenterName, string? RenterNameEnglish, decimal? FinalYearlyRent, decimal? RentMonthly);
    private sealed record TransMastRow(int PropertyId, decimal? RVorCVValue, decimal TmTaxAmount);
    private sealed record TransMastCVRow(int PropertyId, decimal? CapitalValue, decimal TmcvTaxAmount);
    private sealed record TransMastRVRow(int PropertyId, decimal? RateableValue, decimal TmrvTaxAmount);
    private sealed record TaxPendingRow(int PropertyId, decimal PendingAmount);

    // ──────────────────── ROOM AGGREGATE READ ─────────────────────────────────

    public async Task<(double TotalAreaSqMtr, int Count)> GetRoomAggregatesAsync(
        int propertyDetailsId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.RoomWiseSubmissionDetails
            .AsNoTracking()
            .Where(r => r.PropertyDetailsId == propertyDetailsId && r.IsActive && !r.MarkedForDeletion)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalAreaSqMtr = g.Sum(r => r.TotalAreaSqMtr ?? 0),
                Count          = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? (0, 0) : (result.TotalAreaSqMtr, result.Count);
    }

    public Task<PropertyDetailsEntity?> GetTrackedPropertyDetailsByIdAsync(
        int propertyDetailsId,
        CancellationToken cancellationToken = default)
        => _context.PropertyDetails
            .FirstOrDefaultAsync(p => p.Id == propertyDetailsId && p.IsActive && !p.MarkedForDeletion, cancellationToken);
}
