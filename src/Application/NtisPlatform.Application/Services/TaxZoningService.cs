using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Utilities;

namespace NtisPlatform.Application.Services;

public class TaxZoningService : ITaxZoningService
{
    private readonly IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<TaxZoneEntity, int> _taxZoneRepository;
    private readonly ILogger<TaxZoningService> _logger;

    public TaxZoningService(
        IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<TaxZoneEntity, int> taxZoneRepository,
        ILogger<TaxZoningService> logger)
    {
        _repository = repository;
        _wardRepository = wardRepository;
        _taxZoneRepository = taxZoneRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all property numbers with tax zoning information.
    /// SQL Equivalent: SELECT TaxZoneId, WardId, PropertyNo FROM ptis.PropertyMast GROUP BY TaxZoneId, WardId, PropertyNo
    /// </summary>
    public async Task<PagedResult<TaxZoningDto>> GetAllPropertyNo(TaxZoningQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        // Build query with joins instead of separate lookups
        var q = from p in _repository.GetQueryable().AsNoTracking()
                join w in _wardRepository.GetQueryable() on p.WardId equals w.Id
                join tz in _taxZoneRepository.GetQueryable() on p.TaxZoneId equals tz.Id
                where !string.IsNullOrWhiteSpace(p.PropertyNo)
                      && p.IsActive == (queryParams.IsActive ?? true)
                select new
                {
                    p.TaxZoneId,
                    TaxZoneNo = tz.TaxZoneNo,
                    p.WardId,
                    WardNo = w.WardNo,
                    p.PropertyNo
                };

        // Reuse ApplyFilters for all filtering
        q = ApplyFilters(q, queryParams);

        // GROUP BY for distinct combinations
        var distinctQ = q.GroupBy(x => new { x.TaxZoneId, x.TaxZoneNo, x.WardId, x.WardNo, x.PropertyNo })
                         .Select(g => g.Key);

        var desc = string.Equals(queryParams.SortOrder, "DESC", StringComparison.OrdinalIgnoreCase);
        var sortBy = queryParams.SortBy?.ToLowerInvariant();

        // For integer-keyed sorts (taxzone, ward) we can sort in SQL and paginate there.
        // For propertyno (or the default, which should also respect natural order) we must
        // sort in memory because SQL uses lexicographic string ordering ("10" < "2").
        bool sortInMemory = sortBy == "propertyno" || string.IsNullOrEmpty(sortBy);

        int totalCount;

        if (sortInMemory)
        {
            // Fetch all distinct rows for this filter into memory so we can natural-sort them.
            var all = await distinctQ.ToListAsync(cancellationToken);
            totalCount = all.Count;

            var sorted = desc
                ? all.OrderByDescending(x => x.PropertyNo, NaturalStringComparer.Instance)
                     .ThenByDescending(x => x.WardId)
                     .ThenByDescending(x => x.TaxZoneId)
                : all.OrderBy(x => x.PropertyNo, NaturalStringComparer.Instance)
                     .ThenBy(x => x.WardId)
                     .ThenBy(x => x.TaxZoneId);

            var (pageNumber2, pageSize2, skip2, take2) = PaginationHelper.Calculate(queryParams.PageNumber, queryParams.PageSize, totalCount);
            var pagedItems = sorted.Skip(skip2).Take(take2);

            var dtos2 = pagedItems.Select(x => new TaxZoningDto
            {
                TaxZoneId = x.TaxZoneId,
                TaxZoneNo = x.TaxZoneNo,
                WardId = x.WardId,
                WardNo = x.WardNo,
                PropertyNo = x.PropertyNo
            }).ToList();

            return new PagedResult<TaxZoningDto>(dtos2, totalCount, pageNumber2, pageSize2);
        }

        // Integer-column sorts — safe to do in SQL.
        var orderedQ = sortBy == "taxzone"
            ? (desc
                ? distinctQ.OrderByDescending(x => x.TaxZoneId)
                    .ThenByDescending(x => x.WardId)
                    .ThenByDescending(x => x.PropertyNo)
                : distinctQ.OrderBy(x => x.TaxZoneId)
                    .ThenBy(x => x.WardId)
                    .ThenBy(x => x.PropertyNo))
            : (desc
                ? distinctQ.OrderByDescending(x => x.WardId)
                    .ThenByDescending(x => x.TaxZoneId)
                    .ThenByDescending(x => x.PropertyNo)
                : distinctQ.OrderBy(x => x.WardId)
                    .ThenBy(x => x.TaxZoneId)
                    .ThenBy(x => x.PropertyNo));

        totalCount = await orderedQ.CountAsync(cancellationToken);
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(queryParams.PageNumber, queryParams.PageSize, totalCount);
        var rawPage = await orderedQ.Skip(skip).Take(take).ToListAsync(cancellationToken);

        // Project to DTOs (data already has WardNo and TaxZoneNo from joins)
        var dtos = rawPage.Select(x => new TaxZoningDto
        {
            TaxZoneId = x.TaxZoneId,
            TaxZoneNo = x.TaxZoneNo,
            WardId = x.WardId,
            WardNo = x.WardNo,
            PropertyNo = x.PropertyNo
        }).ToList();

        return new PagedResult<TaxZoningDto>(dtos, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Retrieves tax zoning data grouped by TaxZoneId and WardId with MIN/MAX PropertyNo range.
    /// SQL Equivalent: SELECT TaxZoneId, WardId, MIN(PropertyNo) AS fromProperty, MAX(PropertyNo) AS toProperty 
    ///                 FROM ptis.PropertyMast GROUP BY TaxZoneId, WardId
    /// </summary>    
    public async Task<PagedResult<TaxZoningDto>> GetFromToPropertyNo(TaxZoningQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        // Build query with joins instead of separate lookups
        var q = from p in _repository.GetQueryable().AsNoTracking()
                join w in _wardRepository.GetQueryable() on p.WardId equals w.Id
                join tz in _taxZoneRepository.GetQueryable() on p.TaxZoneId equals tz.Id
                where !string.IsNullOrWhiteSpace(p.PropertyNo)
                      && p.IsActive == (queryParams.IsActive ?? true)
                select new
                {
                    p.TaxZoneId,
                    TaxZoneNo = tz.TaxZoneNo,
                    p.WardId,
                    WardNo = w.WardNo,
                    p.PropertyNo
                };
        // Reuse ApplyFilters for all filtering
        q = ApplyFilters(q, queryParams);

        // GROUP BY TaxZoneId, WardId with MIN/MAX aggregation
        var groupedQ = q
            .GroupBy(x => new { x.TaxZoneId, x.TaxZoneNo, x.WardId, x.WardNo })
            .Select(g => new
            {
                g.Key.TaxZoneId,
                g.Key.TaxZoneNo,
                g.Key.WardId,
                g.Key.WardNo,
                FromProperty = g.Min(p => p.PropertyNo),
                ToProperty = g.Max(p => p.PropertyNo)
            });

        // Apply sorting
        var desc = string.Equals(queryParams.SortOrder, "DESC", StringComparison.OrdinalIgnoreCase);
        var orderedQ = (queryParams.SortBy?.ToLower()) switch
        {
            "taxzone" => desc ? groupedQ.OrderByDescending(x => x.TaxZoneId) : groupedQ.OrderBy(x => x.TaxZoneId),
            _ => desc ? groupedQ.OrderByDescending(x => x.WardId) : groupedQ.OrderBy(x => x.WardId),
        };

        var totalCount = await orderedQ.CountAsync(cancellationToken);
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(queryParams.PageNumber, queryParams.PageSize, totalCount);
        var page = await orderedQ.Skip(skip).Take(take).ToListAsync(cancellationToken);

        // Project to DTOs (data already has WardNo and TaxZoneNo from joins)
        var dtos = page.Select(g => new TaxZoningDto
        {
            TaxZoneId = g.TaxZoneId,
            TaxZoneNo = g.TaxZoneNo,
            WardId = g.WardId,
            WardNo = g.WardNo,
            FromProperty = g.FromProperty ?? string.Empty,
            ToProperty = g.ToProperty ?? string.Empty
        }).ToList();

        _logger.LogInformation(
            "GetFromToPropertyNo returned {Count} grouped items (Page {Page}/{TotalPages})",
            dtos.Count,
            pageNumber,
            totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1);

        return new PagedResult<TaxZoningDto>(dtos, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Updates tax zones by property range.
    /// NOTE: Range filtering uses natural sorting (A1, A2...A10) which requires 
    /// client-side processing. For wards with >10,000 properties, consider using PropertyNo CSV instead.
    /// </summary>
    public async Task<TaxZoningDto?> UpdateAsync(UpdateTaxZoningDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateAsync started for WardId: {WardId}, TaxZoneId: {TaxZoneId}", dto.WardId, dto.TaxZoneId);

        // WardId is now a single int (required)
        var wardId = dto.WardId;

        // Parse PropertyNo as CSV (supports "A1" or "A1,A2")
        // If PropertyNo is provided => we do an exact match update (no range logic)
        var props = FilterExpressionBuilder.Csv(dto.PropertyNo);

        // Normalize From/To (trim, convert whitespace => null)
        var from = FilterExpressionBuilder.Norm(dto.FromProperty);
        var to = FilterExpressionBuilder.Norm(dto.ToProperty);

        // Prevent accidental CSV passed into from/to
        if (from?.Contains(',') == true || to?.Contains(',') == true)
            throw new ArgumentException("FromProperty/ToProperty must be single values (no commas).");

        // Base query: only rows in selected ward
        var q = _repository.GetQueryable().Where(x => x.WardId != 0 && x.WardId == wardId);

        // 1) Exact PropertyNo list update (most specific) WardId=MM1 PropertyNo=A1,A2 => update only those PropertyNos in those wards.
        if (props.Count > 0)
        {
            var affected = 0;
            foreach (var chunk in props.Chunk(900))
            {
                affected += await q
                    .Where(x => x.PropertyNo != null && chunk.Contains(x.PropertyNo.Trim()))
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.TaxZoneId, dto.TaxZoneId), ct);
            }
            if (affected == 0)
            {
                _logger.LogWarning("UpdateAsync: No records updated for PropertyNo list in WardId {WardId}", wardId);
                return null;
            }

            _logger.LogInformation("UpdateAsync: Updated {Count} records by PropertyNo list in WardId {WardId}", affected, wardId);

            // Return the first updated property as DTO (for test expectations)
            return new TaxZoningDto
            {
                WardId = wardId,
                PropertyNo = props.FirstOrDefault() ?? string.Empty,
                TaxZoneId = dto.TaxZoneId
            };
        }

        // 2) Range update (FromProperty..ToProperty) - This supports: a)numeric: 1..100  b)prefix+number: A1..A10 (same prefix) c)fallback lexicographic range (case-insensitive)
        if (from != null || to != null)
        {
            // Require both values to avoid ambiguous ranges
            if (from == null || to == null)
                throw new ArgumentException("Both FromProperty and ToProperty are required.");

            // We cannot express your mixed natural-range logic in SQL easily,
            // so we fetch candidate PropertyNo in the ward to memory and filter using MatchRange().
            // IMPORTANT: Fetch ONLY unique PropertyNo strings to minimize memory and network footprint.
            var propertyNumbers = await q
                .Where(x => x.PropertyNo != null)
                .Select(x => x.PropertyNo)
                .Distinct()
                .ToListAsync(ct);

            // Add warning for large datasets
            if (propertyNumbers.Count > 10000)
            {
                _logger.LogWarning(
                    "UpdateAsync: Large range update detected. Processing {Count} distinct property numbers in WardId {WardId}. Consider using PropertyNo list instead.",
                    propertyNumbers.Count,
                    wardId);
            }

            // Filter in memory using the complex MatchRange logic
            var matchingProps = propertyNumbers
                .Where(pn => FilterExpressionBuilder.MatchRange(pn!, from, to))
                .Select(pn => pn!.Trim())
                .ToList();

            if (matchingProps.Count == 0)
            {
                _logger.LogWarning("UpdateAsync: No records matched range {From}..{To} in WardId {WardId}", from, to, wardId);
                return null;
            }

            // Update by chunks.
            var affected = 0;

            // Chunk by property numbers; ward list already applied in 'q'
            foreach (var chunk in matchingProps.Chunk(900))
            {
                affected += await q
                    .Where(x => x.PropertyNo != null && chunk.Contains(x.PropertyNo.Trim()))
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.TaxZoneId, dto.TaxZoneId), ct);
            }

            if (affected == 0)
            {
                _logger.LogWarning("UpdateAsync: No records updated for range {From}..{To} in WardId {WardId}", from, to, wardId);
                return null;
            }

            _logger.LogInformation("UpdateAsync: Updated {Count} records by range {From}..{To} in WardId {WardId}", affected, from, to, wardId);

            return new TaxZoningDto
            {
                WardId = wardId,
                FromProperty = from,
                ToProperty = to,
                TaxZoneId = dto.TaxZoneId
            };
        }

        // 3) Ward-only update (least specific) Example: WardNo=MM1 => update all property rows in ward MM1
        var allAffected = await q.ExecuteUpdateAsync(s => s.SetProperty(e => e.TaxZoneId, dto.TaxZoneId), ct);

        if (allAffected == 0)
        {
            _logger.LogWarning("UpdateAsync: No records updated for WardId {WardId}", wardId);
            return null;
        }

        _logger.LogInformation("UpdateAsync: Updated {Count} records for entire WardId {WardId}", allAffected, wardId);

        return new TaxZoningDto { WardId = wardId, TaxZoneId = dto.TaxZoneId };
    }
        
    private static IQueryable<T> ApplyFilters<T>(IQueryable<T> query, TaxZoningQueryParameters queryParams) where T : class
    {
        var wardId = queryParams.WardId;
        var taxZoneId = queryParams.TaxZoneId;
        var propertyNo = queryParams.PropertyNo;

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = queryParams.SearchTerm.Trim();
            query = query.Where(x =>
                EF.Property<int>(x, "WardId").ToString().Equals(term) ||
                EF.Property<string>(x, "PropertyNo").Contains(term) ||
                EF.Property<int>(x, "TaxZoneId").ToString().Equals(term)
            );
        }
        if (wardId.HasValue)
        {
            var wardIdValue = wardId.Value;
            query = query.Where(x => EF.Property<int>(x, "WardId") == wardIdValue);
        }
        if (!string.IsNullOrWhiteSpace(propertyNo))
        {
            query = query.Where(x => EF.Property<string>(x, "PropertyNo").Contains(propertyNo));
        }
        if (taxZoneId.HasValue)
        {
            var taxZoneIdValue = taxZoneId.Value;
            query = query.Where(x => EF.Property<int>(x, "TaxZoneId") == taxZoneIdValue);
        }

        return query;
    }
}