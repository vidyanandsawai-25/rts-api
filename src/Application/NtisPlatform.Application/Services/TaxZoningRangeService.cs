using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Utilities;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Hand-rolled service (not <c>BaseCommonCrudService</c>-based, see <see cref="ITaxZoningRangeService"/>
/// remarks) implementing the Tax Zoning Range feature: ward + property-number-range/whole-ward tax
/// zone assignment, backed by <c>PTIS.TaxZoningRange</c> and denormalized onto
/// <c>PTIS.PropertyMast.TaxZoneId</c>. The "no gaps, no overlaps" bookkeeping lives entirely inside
/// <c>PTIS.TaxZoningRange</c> itself (see <see cref="TrimOverlappingRangesAsync"/>), so PropertyMast
/// does not need a back-reference to the range that assigned it.
/// </summary>
public partial class TaxZoningRangeService : ITaxZoningRangeService
{
    private const int ChunkSize = 900;

    private readonly IRepository<TaxZoningRangeEntity, int> _rangeRepository;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<TaxZoneEntity, int> _taxZoneRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TaxZoningRangeService> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TaxZoningRangeService(
        IRepository<TaxZoningRangeEntity, int> rangeRepository,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<TaxZoneEntity, int> taxZoneRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<TaxZoningRangeService> logger,
        ILocalizationService localizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _rangeRepository = rangeRepository;
        _propertyRepository = propertyRepository;
        _wardRepository = wardRepository;
        _taxZoneRepository = taxZoneRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _localizationService = localizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetLanguage()
        => _httpContextAccessor.HttpContext?.Items[HttpContextKeys.CurrentLanguage] as string ?? "en";

    #region Read

    public async Task<PagedResult<TaxZoningRangeDto>> GetAllAsync(TaxZoningRangeQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = from r in _rangeRepository.GetQueryable().AsNoTracking()
                    join w in _wardRepository.GetQueryable() on r.WardId equals w.Id
                    join tz in _taxZoneRepository.GetQueryable() on r.TaxZoneId equals tz.Id
                    where !r.MarkedForDeletion
                    select new { Range = r, WardNo = w.WardNo, TaxZoneNo = tz.TaxZoneNo };

        if (queryParameters.WardId.HasValue)
        {
            var wardId = queryParameters.WardId.Value;
            query = query.Where(x => x.Range.WardId == wardId);
        }

        if (queryParameters.TaxZoneId.HasValue)
        {
            var taxZoneId = queryParameters.TaxZoneId.Value;
            query = query.Where(x => x.Range.TaxZoneId == taxZoneId);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.PropertyNo))
        {
            var term = queryParameters.PropertyNo.Trim();
            query = query.Where(x =>
                (x.Range.FromPropertyNo != null && x.Range.FromPropertyNo.Contains(term)) ||
                (x.Range.ToPropertyNo != null && x.Range.ToPropertyNo.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Description))
        {
            var term = queryParameters.Description.Trim();
            query = query.Where(x => x.Range.ZoneDescription.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
        {
            var term = queryParameters.SearchTerm.Trim();
            query = query.Where(x =>
                x.WardNo.Contains(term) ||
                x.TaxZoneNo.Contains(term) ||
                x.Range.ZoneDescription.Contains(term) ||
                (x.Range.FromPropertyNo != null && x.Range.FromPropertyNo.Contains(term)) ||
                (x.Range.ToPropertyNo != null && x.Range.ToPropertyNo.Contains(term)));
        }

        var desc = string.Equals(queryParameters.SortOrder, "DESC", StringComparison.OrdinalIgnoreCase);
        var sortBy = queryParameters.SortBy?.ToLowerInvariant();

        // Filters run at the SQL level (above); sorting + pagination happen in memory below because
        // the default view's natural sort ("2" < "10", ward-then-property-range) isn't SQL-translatable
        // over nvarchar columns. This table is bounded by the number of zoning-range records (not
        // property count), so materializing the filtered set first is cheap.
        var all = await query.ToListAsync(cancellationToken);

        var orderedList = (sortBy switch
        {
            "taxzoneid" => desc ? all.OrderByDescending(x => x.Range.TaxZoneId) : all.OrderBy(x => x.Range.TaxZoneId),
            "wardid" => desc ? all.OrderByDescending(x => x.Range.WardId) : all.OrderBy(x => x.Range.WardId),
            _ => all
                .OrderBy(x => x.WardNo, PropertyRangeMatcher.Comparer)
                .ThenBy(x => x.Range.FromPropertyNo ?? "", PropertyRangeMatcher.Comparer),
        }).ToList();

        var totalCount = orderedList.Count;
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(queryParameters.PageNumber, queryParameters.PageSize, totalCount);
        var page = orderedList.Skip(skip).Take(take).ToList();

        // Batch-fetch properties for every ward represented on this page — one query covers both
        // the entire-ward Min/Max PropertyNo (existing behavior) and the per-row TotalProperties
        // count below, instead of a per-row subquery against PropertyMast (the largest table in the
        // system). SQL MIN/MAX on varchar is lexicographic ("10" < "2"), so sort in-memory via
        // natural sort. Partition rows are intentionally included in TotalProperties — consistent
        // with GetCoverageAsync/GetWardAbstractAsync, which count every PropertyMast row.
        var pageWardIds = page.Select(x => x.Range.WardId).Distinct().ToList();

        var wardProperties = await _propertyRepository.GetQueryable().AsNoTracking()
            .Where(p => pageWardIds.Contains(p.WardId) && !p.MarkedForDeletion && p.PropertyNo != null)
            .Select(p => new { p.WardId, p.PropertyNo, p.PartitionNo })
            .ToListAsync(cancellationToken);

        var wardPropertiesByWard = wardProperties
            .GroupBy(p => p.WardId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var wardPropBounds = wardPropertiesByWard.ToDictionary(
            g => g.Key,
            g =>
            {
                var sorted = g.Value
                    .Where(p => p.PartitionNo == null || p.PartitionNo.Trim() == "")
                    .Select(p => p.PropertyNo!)
                    .OrderBy(n => n, PropertyRangeMatcher.Comparer)
                    .ToList();
                return (Min: sorted.FirstOrDefault(), Max: sorted.LastOrDefault());
            });

        var dtos = page.Select(x =>
        {
            var dto = _mapper.Map<TaxZoningRangeDto>(x.Range);
            dto.WardNo = x.WardNo;
            dto.TaxZoneNo = x.TaxZoneNo;
            if (x.Range.AssignEntireWard && wardPropBounds.TryGetValue(x.Range.WardId, out var bounds))
            {
                dto.MinPropertyNo = bounds.Min;
                dto.MaxPropertyNo = bounds.Max;
            }

            dto.TotalProperties = wardPropertiesByWard.TryGetValue(x.Range.WardId, out var wardProps)
                ? (x.Range.AssignEntireWard
                    ? wardProps.Count
                    : wardProps.Count(p => PropertyRangeMatcher.IsInRange(p.PropertyNo, x.Range.FromPropertyNo, x.Range.ToPropertyNo)))
                : 0;

            return dto;
        }).ToList();

        return new PagedResult<TaxZoningRangeDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<TaxZoningRangeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _rangeRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null || entity.MarkedForDeletion)
            return null;

        var dto = _mapper.Map<TaxZoningRangeDto>(entity);
        var ward = await _wardRepository.GetByIdAsync(entity.WardId, cancellationToken);
        var taxZone = await _taxZoneRepository.GetByIdAsync(entity.TaxZoneId, cancellationToken);
        dto.WardNo = ward?.WardNo ?? string.Empty;
        dto.TaxZoneNo = taxZone?.TaxZoneNo ?? string.Empty;
        return dto;
    }

    #endregion

    #region Create / Update / Delete

    public async Task<IReadOnlyList<TaxZoningRangeDto>> CreateAsync(CreateTaxZoningRangeDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var userId = _currentUserService.GetCurrentUserId();

        if (dto.WardIds == null || dto.WardIds.Count == 0)
            throw new ArgumentException("At least one ward must be selected.", nameof(dto));

        var taxZone = await _taxZoneRepository.GetByIdAsync(dto.TaxZoneId, cancellationToken);
        if (taxZone == null || !taxZone.IsActive)
            throw new ArgumentException($"Tax zone {dto.TaxZoneId} does not exist or is inactive.", nameof(dto));

        var isMultiWard = dto.WardIds.Count > 1;
        var effectiveEntireWard = isMultiWard || dto.AssignEntireWard;

        if (!effectiveEntireWard)
        {
            if (string.IsNullOrWhiteSpace(dto.FromPropertyNo) || string.IsNullOrWhiteSpace(dto.ToPropertyNo))
                throw new ArgumentException("FromPropertyNo and ToPropertyNo are required for a range assignment.", nameof(dto));
        }

        var distinctWardIds = dto.WardIds.Distinct().ToList();
        var wardEntities = new Dictionary<int, WardEntity>();
        var created = new List<TaxZoningRangeEntity>();

        foreach (var wardId in distinctWardIds)
        {
            var ward = await _wardRepository.GetByIdAsync(wardId, cancellationToken);
            if (ward == null || !ward.IsActive)
                throw new ArgumentException($"Ward {wardId} does not exist or is inactive.", nameof(dto));
            wardEntities[wardId] = ward;

            var wardProps = effectiveEntireWard
                ? await GetDistinctPropertyNosAsync(wardId, cancellationToken)
                : null;
            var wardMin = wardProps?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            var wardMax = wardProps?.LastOrDefault(p => !string.IsNullOrWhiteSpace(p));

            var entity = new TaxZoningRangeEntity
            {
                WardId = wardId,
                TaxZoneId = dto.TaxZoneId,
                FromPropertyNo = effectiveEntireWard ? wardMin : dto.FromPropertyNo!.Trim(),
                ToPropertyNo = effectiveEntireWard ? wardMax : dto.ToPropertyNo!.Trim(),
                AssignEntireWard = effectiveEntireWard,
                ZoneDescription = dto.ZoneDescription,
                IsActive = dto.IsActive,
                CreatedBy = userId
            };
            created.Add(entity);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var entity in created)
            {
                await _rangeRepository.AddAsync(entity, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var entity in created)
            {
                await ApplyRangeToPropertiesAsync(entity, cancellationToken);

                if (!effectiveEntireWard)
                {
                    var allWardProps = await GetDistinctPropertyNosAsync(entity.WardId, cancellationToken);
                    await TrimOverlappingRangesAsync(entity.WardId, entity.FromPropertyNo!, entity.ToPropertyNo!, entity.Id, allWardProps, cancellationToken);
                }
                else
                {
                    await SoftDeleteAllRangesForWardAsync(entity.WardId, entity.Id, cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return created.Select(e =>
            {
                var d = _mapper.Map<TaxZoningRangeDto>(e);
                d.WardNo = wardEntities[e.WardId].WardNo;
                d.TaxZoneNo = taxZone.TaxZoneNo;
                return d;
            }).ToList();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TaxZoningRangeDto?> UpdateAsync(int id, UpdateTaxZoningRangeDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var userId = _currentUserService.GetCurrentUserId();

        var entity = await _rangeRepository.GetByIdAsync(id, cancellationToken);
        if (entity == null || entity.MarkedForDeletion)
            return null;

        var ward = await _wardRepository.GetByIdAsync(dto.WardId, cancellationToken);
        if (ward == null || !ward.IsActive)
            throw new ArgumentException($"Ward {dto.WardId} does not exist or is inactive.", nameof(dto));

        var taxZone = await _taxZoneRepository.GetByIdAsync(dto.TaxZoneId, cancellationToken);
        if (taxZone == null || !taxZone.IsActive)
            throw new ArgumentException($"Tax zone {dto.TaxZoneId} does not exist or is inactive.", nameof(dto));

        if (!dto.AssignEntireWard)
        {
            if (string.IsNullOrWhiteSpace(dto.FromPropertyNo) || string.IsNullOrWhiteSpace(dto.ToPropertyNo))
                throw new ArgumentException("FromPropertyNo and ToPropertyNo are required for a range assignment.", nameof(dto));
        }

        // Capture old state before any mutation — needed to preserve zone for excluded properties
        var oldTaxZoneId = entity.TaxZoneId;
        var oldZoneDescription = entity.ZoneDescription;
        var oldCreatedBy = entity.CreatedBy;
        var oldFrom = entity.FromPropertyNo;
        var oldTo = entity.ToPropertyNo;
        var oldAssignEntireWard = entity.AssignEntireWard;

        // Determine new from/to — for entire-ward, resolve to actual min/max property
        var allWardPropsForUpdate = await GetDistinctPropertyNosAsync(dto.WardId, cancellationToken);
        var sortedWardProps = allWardPropsForUpdate
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, PropertyRangeMatcher.Comparer)
            .ToList();

        var newFrom = dto.AssignEntireWard ? sortedWardProps.FirstOrDefault() : dto.FromPropertyNo!.Trim();
        var newTo = dto.AssignEntireWard ? sortedWardProps.LastOrDefault() : dto.ToPropertyNo!.Trim();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Find properties currently linked to this range that will NOT be covered by the new range.
            // These keep the old zone — create new range records for them before unlinking.
            if (!string.IsNullOrWhiteSpace(newFrom) && !string.IsNullOrWhiteSpace(newTo))
            {
                var oldRangeProps = sortedWardProps
                    .Where(p => PropertyRangeMatcher.IsInRange(p,
                        oldAssignEntireWard ? sortedWardProps.FirstOrDefault() : oldFrom,
                        oldAssignEntireWard ? sortedWardProps.LastOrDefault() : oldTo))
                    .ToList();

                var newRangeSet = sortedWardProps
                    .Where(p => PropertyRangeMatcher.IsInRange(p, newFrom, newTo))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var excluded = oldRangeProps.Where(p => !newRangeSet.Contains(p)).ToList();

                if (excluded.Count > 0)
                {
                    var excludedGroups = SplitIntoContiguousGroups(excluded, sortedWardProps);
                    var remainders = excludedGroups.Select(group => new TaxZoningRangeEntity
                    {
                        WardId = dto.WardId,
                        TaxZoneId = oldTaxZoneId,
                        FromPropertyNo = group.First(),
                        ToPropertyNo = group.Last(),
                        AssignEntireWard = false,
                        ZoneDescription = oldZoneDescription,
                        IsActive = true,
                        CreatedBy = oldCreatedBy,
                    }).ToList();

                    foreach (var remainder in remainders)
                    {
                        await _rangeRepository.AddAsync(remainder, cancellationToken);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    foreach (var chunk in excluded.Chunk(ChunkSize))
                    {
                        await _propertyRepository.GetQueryable()
                            .Where(p => p.WardId == dto.WardId && !p.MarkedForDeletion
                                        && p.PropertyNo != null
                                        && chunk.Contains(p.PropertyNo.Trim()))
                            .ExecuteUpdateAsync(
                                s => s.SetProperty(e => e.TaxZoneId, oldTaxZoneId),
                                cancellationToken);
                    }
                }
            }

            entity.WardId = dto.WardId;
            entity.TaxZoneId = dto.TaxZoneId;
            entity.FromPropertyNo = newFrom;
            entity.ToPropertyNo = newTo;
            entity.AssignEntireWard = dto.AssignEntireWard;
            entity.ZoneDescription = dto.ZoneDescription;
            entity.IsActive = dto.IsActive;
            entity.UpdatedBy = userId;

            await _rangeRepository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await ApplyRangeToPropertiesAsync(entity, cancellationToken);

            if (!entity.AssignEntireWard)
            {
                await TrimOverlappingRangesAsync(entity.WardId, entity.FromPropertyNo!, entity.ToPropertyNo!, entity.Id, allWardPropsForUpdate, cancellationToken);
            }
            else
            {
                await SoftDeleteAllRangesForWardAsync(entity.WardId, entity.Id, cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var result = _mapper.Map<TaxZoningRangeDto>(entity);
        result.WardNo = ward.WardNo;
        result.TaxZoneNo = taxZone.TaxZoneNo;
        return result;
    }

    #endregion

    #region Bulk

    public async Task<RangeResult<TaxZoningRangeDto>> BulkUpsertAsync(BulkTaxZoningRangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = _currentUserService.GetCurrentUserId();

        if (request.Items.Count == 0)
            return new RangeResult<TaxZoningRangeDto>(0, 0, []);

        var errors = new List<string>();
        var pending = new List<CreateTaxZoningRangeDto>();

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            if (!Validator.TryValidateObject(item, new ValidationContext(item), validationResults, validateAllProperties: true))
            {
                errors.Add($"Item {i}: {string.Join("; ", validationResults.Select(v => v.ErrorMessage))}");
                continue;
            }

            if (item.WardIds == null || item.WardIds.Count == 0)
            {
                errors.Add($"Item {i}: At least one ward must be selected.");
                continue;
            }

            var isMultiWard = item.WardIds.Count > 1;
            var effectiveEntireWard = isMultiWard || item.AssignEntireWard;

            try
            {
                var taxZone = await _taxZoneRepository.GetByIdAsync(item.TaxZoneId, cancellationToken);
                if (taxZone == null || !taxZone.IsActive)
                    throw new ArgumentException($"Tax zone {item.TaxZoneId} does not exist or is inactive.");

                foreach (var wardId in item.WardIds.Distinct())
                {
                    var ward = await _wardRepository.GetByIdAsync(wardId, cancellationToken);
                    if (ward == null || !ward.IsActive)
                        throw new ArgumentException($"Ward {wardId} does not exist or is inactive.");
                }

                if (!effectiveEntireWard)
                {
                    if (string.IsNullOrWhiteSpace(item.FromPropertyNo) || string.IsNullOrWhiteSpace(item.ToPropertyNo))
                        throw new ArgumentException("FromPropertyNo and ToPropertyNo are required for a range assignment.");
                }

                pending.Add(item);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Item {i}: {ex.Message}");
            }
        }

        if (pending.Count == 0)
            return new RangeResult<TaxZoningRangeDto>(0, errors.Count, [], errors.Count > 0 ? errors : null);

        var results = new List<TaxZoningRangeDto>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var item in pending)
            {
                var created = await CreateOneWithoutTransactionAsync(item, userId, cancellationToken);
                results.AddRange(created);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new RangeResult<TaxZoningRangeDto>(results.Count, errors.Count, results, errors.Count > 0 ? errors : null);
    }

    /// <summary>Shared by <see cref="BulkUpsertAsync"/> — same logic as <see cref="CreateAsync"/> minus the outer transaction (caller manages it).</summary>
    private async Task<List<TaxZoningRangeDto>> CreateOneWithoutTransactionAsync(CreateTaxZoningRangeDto dto, int userId, CancellationToken cancellationToken)
    {
        var taxZone = await _taxZoneRepository.GetByIdAsync(dto.TaxZoneId, cancellationToken);
        var isMultiWard = dto.WardIds.Count > 1;
        var effectiveEntireWard = isMultiWard || dto.AssignEntireWard;
        var results = new List<TaxZoningRangeDto>();

        foreach (var wardId in dto.WardIds.Distinct())
        {
            var ward = await _wardRepository.GetByIdAsync(wardId, cancellationToken);

            var wardPropsForCreate = effectiveEntireWard
                ? await GetDistinctPropertyNosAsync(wardId, cancellationToken)
                : null;
            var wardMinForCreate = wardPropsForCreate?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            var wardMaxForCreate = wardPropsForCreate?.LastOrDefault(p => !string.IsNullOrWhiteSpace(p));

            var entity = new TaxZoningRangeEntity
            {
                WardId = wardId,
                TaxZoneId = dto.TaxZoneId,
                FromPropertyNo = effectiveEntireWard ? wardMinForCreate : dto.FromPropertyNo!.Trim(),
                ToPropertyNo = effectiveEntireWard ? wardMaxForCreate : dto.ToPropertyNo!.Trim(),
                AssignEntireWard = effectiveEntireWard,
                ZoneDescription = dto.ZoneDescription,
                IsActive = dto.IsActive,
                CreatedBy = userId
            };

            await _rangeRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await ApplyRangeToPropertiesAsync(entity, cancellationToken);

            if (!effectiveEntireWard)
            {
                var allWardProps = await GetDistinctPropertyNosAsync(wardId, cancellationToken);
                await TrimOverlappingRangesAsync(wardId, entity.FromPropertyNo!, entity.ToPropertyNo!, entity.Id, allWardProps, cancellationToken);
            }
            else
            {
                await SoftDeleteAllRangesForWardAsync(wardId, entity.Id, cancellationToken);
            }

            var d = _mapper.Map<TaxZoningRangeDto>(entity);
            d.WardNo = ward?.WardNo ?? string.Empty;
            d.TaxZoneNo = taxZone?.TaxZoneNo ?? string.Empty;
            results.Add(d);
        }

        return results;
    }

    #endregion

    #region Coverage / Abstract

    public async Task<TaxZoningCoverageDto> GetCoverageAsync(IReadOnlyList<int>? wardIds = null, CancellationToken cancellationToken = default)
    {
        var propQuery = _propertyRepository.GetQueryable().AsNoTracking().Where(p => !p.MarkedForDeletion);

        if (wardIds is { Count: > 0 })
            propQuery = propQuery.Where(p => wardIds.Contains(p.WardId));

        var properties = await propQuery
            .Select(p => new { p.WardId, p.PropertyNo })
            .ToListAsync(cancellationToken);

        var relevantWardIds = properties.Select(p => p.WardId).Distinct().ToList();
        var rangesByWard = await GetActiveRangesByWardAsync(relevantWardIds, cancellationToken);

        // Coverage is derived from currently-active TaxZoningRange bounds, not the denormalized
        // PropertyMast.TaxZoneId column, so it stays in sync the instant a range is added/edited/deleted.
        var countsByZone = new Dictionary<int, int>();
        var covered = 0;

        foreach (var prop in properties)
        {
            if (!rangesByWard.TryGetValue(prop.WardId, out var wardRanges))
                continue;

            var zoneId = MatchZone(prop.PropertyNo, wardRanges);
            if (zoneId == null)
                continue;

            covered++;
            countsByZone[zoneId.Value] = countsByZone.GetValueOrDefault(zoneId.Value) + 1;
        }

        var taxZones = await _taxZoneRepository.GetQueryable().AsNoTracking()
            .Where(z => z.IsActive)
            .Select(z => new { z.Id, z.TaxZoneNo })
            .ToListAsync(cancellationToken);

        var zoneWise = taxZones.Select(tz => new TaxZoningZoneWiseCountDto
        {
            TaxZoneId = tz.Id,
            TaxZoneNo = tz.TaxZoneNo,
            Count = countsByZone.GetValueOrDefault(tz.Id)
        }).ToList();

        var total = properties.Count;
        return new TaxZoningCoverageDto
        {
            TotalProperties = total,
            CoveredProperties = covered,
            PendingProperties = total - covered,
            ZoneWiseCounts = zoneWise
        };
    }

    public async Task<PagedResult<WardZoningAbstractDto>> GetWardAbstractAsync(WardAbstractQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        var wardQuery = _wardRepository.GetQueryable().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            wardQuery = wardQuery.Where(w => w.WardNo.Contains(queryParams.SearchTerm));

        wardQuery = wardQuery.OrderBy(w => w.WardNo);

        var totalCount = await wardQuery.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, queryParams.PageNumber);
        var pageSize = queryParams.PageSize <= 0 ? totalCount : queryParams.PageSize;

        var wards = await wardQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var wardIds = wards.Select(w => w.Id).ToList();
        var rangesByWard = await GetActiveRangesByWardAsync(wardIds, cancellationToken);
        var taxZoneNos = await _taxZoneRepository.GetQueryable().AsNoTracking()
            .Select(z => new { z.Id, z.TaxZoneNo })
            .ToDictionaryAsync(z => z.Id, z => z.TaxZoneNo, cancellationToken);

        var result = new List<WardZoningAbstractDto>();

        foreach (var ward in wards)
        {
            var wardPropertyNos = await _propertyRepository.GetQueryable().AsNoTracking()
                .Where(p => !p.MarkedForDeletion && p.WardId == ward.Id)
                .Select(p => p.PropertyNo)
                .ToListAsync(cancellationToken);

            var wardRanges = rangesByWard.GetValueOrDefault(ward.Id) ?? new List<ActiveRangeBounds>();

            // Coverage is derived from currently-active TaxZoningRange bounds, not the denormalized
            // PropertyMast.TaxZoneId column, so it stays in sync the instant a range is deleted/edited.
            var total = wardPropertyNos.Count;
            var countsByZone = new Dictionary<int, int>();
            var covered = 0;

            foreach (var propertyNo in wardPropertyNos)
            {
                var zoneId = MatchZone(propertyNo, wardRanges);
                if (zoneId == null)
                    continue;

                covered++;
                countsByZone[zoneId.Value] = countsByZone.GetValueOrDefault(zoneId.Value) + 1;
            }

            var zoneCounts = countsByZone.Select(kv => new WardZoningAbstractZoneCountDto
            {
                TaxZoneId = kv.Key,
                TaxZoneNo = taxZoneNos.GetValueOrDefault(kv.Key, string.Empty),
                Count = kv.Value
            }).ToList();

            result.Add(new WardZoningAbstractDto
            {
                WardId = ward.Id,
                WardNo = ward.WardNo,
                TotalProperties = total,
                CoveredProperties = covered,
                PendingProperties = total - covered,
                CoveragePercent = total == 0 ? 0 : Math.Round(covered * 100.0 / total, 2),
                ZoneCounts = zoneCounts
            });
        }

        return new PagedResult<WardZoningAbstractDto>(result, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<WardPropertyDto>> GetPropertiesByWardAsync(WardPropertyQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        // Only main properties — exclude partitioned sub-records (PartitionNo is not null/empty).
        // Group by PropertyNo so that duplicate records sharing the same number collapse to one row.
        var ward = await _wardRepository.GetByIdAsync(queryParams.WardId, cancellationToken);
        var wardNo = ward?.WardNo ?? string.Empty;

        var query = from p in _propertyRepository.GetQueryable().AsNoTracking()
                    where p.WardId == queryParams.WardId
                          && !p.MarkedForDeletion
                          && p.PropertyNo != null
                          && (p.PartitionNo == null || p.PartitionNo.Trim() == "")
                    group p by p.PropertyNo into g
                    orderby g.Key
                    select new WardPropertyDto
                    {
                        PropertyId = 0,
                        WardId = queryParams.WardId,
                        WardNo = wardNo,
                        PropertyNo = g.Key,
                        IsActive = true,
                    };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = queryParams.PageSize == -1
            ? (await query.ToListAsync(cancellationToken)).OrderBy(x => x.PropertyNo ?? string.Empty, PropertyRangeMatcher.Comparer).ToList()
            : await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync(cancellationToken);

        return new PagedResult<WardPropertyDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize == -1 ? totalCount : queryParams.PageSize);
    }

    #endregion

    #region Internal helpers

    /// <inheritdoc/>
    public async Task ReconcilePropertyZoneChangeAsync(
        int propertyId,
        int wardId,
        string propertyNo,
        int newTaxZoneId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create a new single-property range for the new zone
            var newRange = new TaxZoningRangeEntity
            {
                WardId = wardId,
                TaxZoneId = newTaxZoneId,
                FromPropertyNo = propertyNo,
                ToPropertyNo = propertyNo,
                AssignEntireWard = false,
                ZoneDescription = string.Empty,
                IsActive = true,
                CreatedBy = userId,
            };

            await _rangeRepository.AddAsync(newRange, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Carve this property out of any existing ranges (trim/split as needed). The caller
            // (PropertyBasicDetailsService) already wrote the new TaxZoneId onto PropertyMast
            // directly, so this only needs to keep TaxZoningRange's own bounds bookkeeping correct.
            var allWardProps = await GetDistinctPropertyNosAsync(wardId, cancellationToken);
            await TrimOverlappingRangesAsync(wardId, propertyNo, propertyNo, newRange.Id, allWardProps, cancellationToken);

                                                                                                                                                                                                                                                                                                                                             await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Soft-deletes all existing active ranges for a ward except <paramref name="excludeRangeId"/>.
    /// Called when an entire-ward assignment is created, since it supersedes every prior range.
    /// </summary>
    private async Task SoftDeleteAllRangesForWardAsync(int wardId, int excludeRangeId, CancellationToken cancellationToken)
    {
        var existing = await _rangeRepository.GetQueryable()
            .Where(r => r.WardId == wardId && !r.MarkedForDeletion && r.Id != excludeRangeId)
            .ToListAsync(cancellationToken);

        foreach (var old in existing)
        {
            old.IsActive = false;
            old.MarkedForDeletion = true;
            old.MarkedForDeletionDate ??= DateTime.Now;
            await _rangeRepository.UpdateAsync(old, cancellationToken);
        }

        if (existing.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// After a new specific range is persisted, trims any existing specific ranges for the same
    /// ward whose coverage overlaps the new range. The new range takes precedence:
    /// <list type="bullet">
    ///   <item>fully-covered existing ranges are soft-deleted;</item>
    ///   <item>partially-covered ranges are trimmed (From/To updated);</item>
    ///   <item>if the new range cuts through the middle of an existing range, the existing range is
    ///   split into two records and property references are updated for the disconnected tail.</item>
    /// </list>
    /// </summary>
    private async Task TrimOverlappingRangesAsync(
        int wardId,
        string newFrom,
        string newTo,
        int newRangeId,
        List<string?> allWardProps,
        CancellationToken cancellationToken)
    {
        var existingRanges = await _rangeRepository.GetQueryable()
            .Where(r => r.WardId == wardId && !r.MarkedForDeletion && !r.AssignEntireWard && r.Id != newRangeId)
            .ToListAsync(cancellationToken);

        var entireWardRanges = await _rangeRepository.GetQueryable()
            .Where(r => r.WardId == wardId && !r.MarkedForDeletion && r.AssignEntireWard && r.Id != newRangeId)
            .ToListAsync(cancellationToken);

        if (existingRanges.Count == 0 && entireWardRanges.Count == 0)
            return;

        var sortedAll = allWardProps
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, PropertyRangeMatcher.Comparer)
            .ToList();

        // ── Trim existing specific ranges ──────────────────────────────────────
        foreach (var range in existingRanges)
        {
            var overlapSet = sortedAll
                .Where(p =>
                    PropertyRangeMatcher.IsInRange(p, range.FromPropertyNo, range.ToPropertyNo) &&
                    PropertyRangeMatcher.IsInRange(p, newFrom, newTo))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (overlapSet.Count == 0)
                continue;

            var remaining = sortedAll
                .Where(p =>
                    PropertyRangeMatcher.IsInRange(p, range.FromPropertyNo, range.ToPropertyNo) &&
                    !overlapSet.Contains(p))
                .ToList();

            if (remaining.Count == 0)
            {
                // Entire existing range is subsumed — soft-delete it
                range.IsActive = false;
                range.MarkedForDeletion = true;
                range.MarkedForDeletionDate ??= DateTime.Now;
                await _rangeRepository.UpdateAsync(range, cancellationToken);
                continue;
            }

            var groups = SplitIntoContiguousGroups(remaining, sortedAll);

            // Trim original range to the first contiguous group
            range.FromPropertyNo = groups[0].First();
            range.ToPropertyNo = groups[0].Last();
            await _rangeRepository.UpdateAsync(range, cancellationToken);

            // If the new range cut the middle, create extra record(s) for disconnected tails
            for (var gi = 1; gi < groups.Count; gi++)
            {
                var group = groups[gi];
                var split = new TaxZoningRangeEntity
                {
                    WardId = wardId,
                    TaxZoneId = range.TaxZoneId,
                    FromPropertyNo = group.First(),
                    ToPropertyNo = group.Last(),
                    AssignEntireWard = false,
                    ZoneDescription = range.ZoneDescription,
                    IsActive = range.IsActive,
                    CreatedBy = range.CreatedBy,
                };

                // No PropertyMast write needed here — these properties' TaxZoneId already equals
                // range.TaxZoneId (this split only re-homes them under a new TaxZoningRange row so
                // future edits see correct, non-overlapping bounds; TaxZoneId itself never changes).
                await _rangeRepository.AddAsync(split, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // flush to obtain split.Id
            }
        }

        // ── Carve the new specific range out of any entire-ward records ────────
        // An entire-ward range (AssignEntireWard=true, NULL from/to) implicitly covers every
        // property in the ward. Adding a specific sub-range converts the entire-ward record into
        // one or more specific records that cover only the remaining properties.
        if (entireWardRanges.Count > 0 && sortedAll.Count > 0)
        {
            var newRangeProps = sortedAll
                .Where(p => PropertyRangeMatcher.IsInRange(p, newFrom, newTo))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var range in entireWardRanges)
            {
                if (newRangeProps.Count == 0)
                    continue;

                var remaining = sortedAll.Where(p => !newRangeProps.Contains(p)).ToList();

                if (remaining.Count == 0)
                {
                    // New range covers the entire ward — soft-delete the entire-ward record
                    range.IsActive = false;
                    range.MarkedForDeletion = true;
                    range.MarkedForDeletionDate ??= DateTime.Now;
                    await _rangeRepository.UpdateAsync(range, cancellationToken);
                    continue;
                }

                var groups = SplitIntoContiguousGroups(remaining, sortedAll);

                // Convert entire-ward record to a specific range for the first remaining group
                range.AssignEntireWard = false;
                range.FromPropertyNo = groups[0].First();
                range.ToPropertyNo = groups[0].Last();
                await _rangeRepository.UpdateAsync(range, cancellationToken);

                // Create additional specific records for any disconnected tails
                for (var gi = 1; gi < groups.Count; gi++)
                {
                    var group = groups[gi];
                    var split = new TaxZoningRangeEntity
                    {
                        WardId = wardId,
                        TaxZoneId = range.TaxZoneId,
                        FromPropertyNo = group.First(),
                        ToPropertyNo = group.Last(),
                        AssignEntireWard = false,
                        ZoneDescription = range.ZoneDescription,
                        IsActive = range.IsActive,
                        CreatedBy = range.CreatedBy,
                    };

                    // No PropertyMast write needed — these properties already have TaxZoneId ==
                    // range.TaxZoneId from when the entire-ward assignment first applied it to
                    // every property in the ward. This split only re-homes the bookkeeping row.
                    await _rangeRepository.AddAsync(split, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Splits a sorted list of remaining property numbers into runs that are contiguous
    /// within <paramref name="sortedAllProps"/> (i.e. no gaps exist in the full ward list).
    /// </summary>
    private static List<List<string>> SplitIntoContiguousGroups(List<string> remaining, List<string> sortedAllProps)
    {
        var remainingSet = new HashSet<string>(remaining, StringComparer.OrdinalIgnoreCase);
        var groups = new List<List<string>>();
        var current = new List<string>();

        foreach (var prop in sortedAllProps)
        {
            if (remainingSet.Contains(prop))
            {
                current.Add(prop);
            }
            else if (current.Count > 0)
            {
                groups.Add(current);
                current = new List<string>();
            }
        }

        if (current.Count > 0)
            groups.Add(current);

        return groups;
    }

    private async Task<List<string?>> GetDistinctPropertyNosAsync(int wardId, CancellationToken cancellationToken)
    {
        var list = await _propertyRepository.GetQueryable().AsNoTracking()
            .Where(p => p.WardId == wardId && !p.MarkedForDeletion && p.PropertyNo != null)
            .Select(p => p.PropertyNo)
            .Distinct()
            .ToListAsync(cancellationToken);

        return list.OrderBy(p => p, PropertyRangeMatcher.Comparer).ToList();
    }

    private sealed record ActiveRangeBounds(int WardId, int TaxZoneId, string? FromPropertyNo, string? ToPropertyNo, bool AssignEntireWard);

    /// <summary>
    /// Loads currently-active (non-deleted) TaxZoningRange bounds for the given wards, grouped by
    /// WardId. Coverage is derived from this — the source of truth for "is this property covered" —
    /// rather than from the denormalized PropertyMast.TaxZoneId column, so a deleted/edited range is
    /// reflected in coverage counts immediately, with no separate PropertyMast write required.
    /// </summary>
    private async Task<Dictionary<int, List<ActiveRangeBounds>>> GetActiveRangesByWardAsync(
        IReadOnlyList<int> wardIds, CancellationToken cancellationToken)
    {
        if (wardIds.Count == 0)
            return new Dictionary<int, List<ActiveRangeBounds>>();

        var ranges = await _rangeRepository.GetQueryable().AsNoTracking()
            .Where(r => !r.MarkedForDeletion && r.IsActive && wardIds.Contains(r.WardId))
            .Select(r => new ActiveRangeBounds(r.WardId, r.TaxZoneId, r.FromPropertyNo, r.ToPropertyNo, r.AssignEntireWard))
            .ToListAsync(cancellationToken);

        return ranges.GroupBy(r => r.WardId).ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>Returns the TaxZoneId of the first active range covering <paramref name="propertyNo"/>, or null.</summary>
    private static int? MatchZone(string? propertyNo, IReadOnlyList<ActiveRangeBounds> wardRanges)
    {
        foreach (var r in wardRanges)
        {
            if (r.AssignEntireWard || PropertyRangeMatcher.IsInRange(propertyNo, r.FromPropertyNo, r.ToPropertyNo))
                return r.TaxZoneId;
        }
        return null;
    }

    private async Task ApplyRangeToPropertiesAsync(TaxZoningRangeEntity range, CancellationToken cancellationToken)
    {
        var propertyQuery = _propertyRepository.GetQueryable().Where(p => p.WardId == range.WardId && !p.MarkedForDeletion);

        if (range.AssignEntireWard)
        {
            await propertyQuery.ExecuteUpdateAsync(s => s
                .SetProperty(e => e.TaxZoneId, range.TaxZoneId), cancellationToken);
            return;
        }

        var propertyNos = await propertyQuery
            .Where(p => p.PropertyNo != null)
            .Select(p => p.PropertyNo)
            .Distinct()
            .ToListAsync(cancellationToken);

        var matching = propertyNos
            .Where(pn => PropertyRangeMatcher.IsInRange(pn, range.FromPropertyNo, range.ToPropertyNo))
            .Select(pn => pn!.Trim())
            .ToList();

        if (matching.Count == 0)
        {
            _logger.LogWarning(
                "ApplyRangeToPropertiesAsync: no properties matched range {From}..{To} in WardId {WardId}",
                range.FromPropertyNo, range.ToPropertyNo, range.WardId);
            return;
        }

        foreach (var chunk in matching.Chunk(ChunkSize))
        {
            await propertyQuery
                .Where(p => p.PropertyNo != null && chunk.Contains(p.PropertyNo.Trim()))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.TaxZoneId, range.TaxZoneId), cancellationToken);
        }
    }

    #endregion
}
