using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Utilities;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

public class LockUnlockService : ILockUnlockService
{
    private const string ActionLock = "lock";
    private const string ActionUnlock = "unlock";

    /// <summary>
    /// Batch size for PropertyId-based bulk operations - matches the chunking convention already
    /// used in TaxZoningService for SQL parameter-count safety on large ID lists.
    /// </summary>
    private const int PropertyIdChunkSize = 900;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<LockUnlockService> _logger;
    private readonly IPropertySearchService _propertySearchService;

    public LockUnlockService(ApplicationDbContext context, ILogger<LockUnlockService> logger, IPropertySearchService propertySearchService)
    {
        _context = context;
        _logger = logger;
        _propertySearchService = propertySearchService;
    }

    public async Task<List<LockableScreenDto>> GetLockableScreensAsync(
        string? search = null, int? id = null, int? moduleId = null, CancellationToken ct = default)
    {
        var query = _context.ScreenMaster
            .AsNoTracking()
            .Where(s => s.IsActive == true && s.IsPropertyLockable == true)
            .GroupJoin(_context.ModuleMasters, s => s.ModuleId, m => m.Id,
                (s, modules) => new { Screen = s, Modules = modules })
            .SelectMany(x => x.Modules.DefaultIfEmpty(), (x, m) => new { Screen = x.Screen, Module = m });

        if (id.HasValue)
        {
            query = query.Where(x => x.Screen.Id == id.Value);
        }

        if (moduleId.HasValue)
        {
            query = query.Where(x => x.Module != null && x.Module.Id == moduleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Screen.ScreenName != null && x.Screen.ScreenName.ToLower().Contains(searchTerm)) ||
                (x.Screen.ScreenNameLocal != null && x.Screen.ScreenNameLocal.ToLower().Contains(searchTerm)) ||
                (x.Module != null && x.Module.ModuleName != null && x.Module.ModuleName.ToLower().Contains(searchTerm)) ||
                (x.Module != null && x.Module.ModuleNameLocal != null && x.Module.ModuleNameLocal.ToLower().Contains(searchTerm)));
        }

        return await query
            .OrderBy(x => x.Screen.DisplayOrder ?? int.MaxValue)
            .ThenBy(x => x.Screen.ScreenName)
            .Select(x => new LockableScreenDto
            {
                Id = x.Screen.Id,
                ScreenCode = x.Screen.ScreenCode ?? string.Empty,
                ScreenName = x.Screen.ScreenName ?? string.Empty,
                ScreenNameLocal = x.Screen.ScreenNameLocal,
                DisplayOrder = x.Screen.DisplayOrder,
                ModuleId = x.Module != null ? x.Module.Id : null,
                ModuleCode = x.Module != null ? x.Module.ModuleCode : null,
                ModuleName = x.Module != null ? x.Module.ModuleName : null,
                ModuleNameLocal = x.Module != null ? x.Module.ModuleNameLocal : null,
            })
            .ToListAsync(ct);
    }

    public async Task<PagedResult<PropertyLockRowDto>> GetPropertyLocksAsync(
        FilterPropertyLocksRequestDto request, CancellationToken ct)
    {
        if (request.WardId <= 0)
            throw new ArgumentException("WardId is required.");

        var query = _context.PropertyMast
            .Where(pm => pm.WardId == request.WardId);

        if (!string.IsNullOrEmpty(request.FromPropertyNo))
            query = query.Where(pm => string.Compare(pm.PropertyNo, request.FromPropertyNo) >= 0);
        if (!string.IsNullOrEmpty(request.ToPropertyNo))
            query = query.Where(pm => string.Compare(pm.PropertyNo, request.ToPropertyNo) <= 0);
        if (!string.IsNullOrWhiteSpace(request.PartitionNo))
        {
            var partitionNumbers = request.PartitionNo
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(partitionNo => !string.IsNullOrWhiteSpace(partitionNo))
                .Distinct()
                .ToList();

            if (partitionNumbers.Count > 0)
            {
                var includeBlankPartition = partitionNumbers.Contains("0");
                query = query.Where(pm =>
                    (pm.PartitionNo != null && partitionNumbers.Contains(pm.PartitionNo)) ||
                    (includeBlankPartition && string.IsNullOrEmpty(pm.PartitionNo)));
            }
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(pm =>
                (pm.PropertyNo != null && pm.PropertyNo.Contains(search)) ||
                (pm.PartitionNo != null && pm.PartitionNo.Contains(search)));
        }

        var totalCount = await query.CountAsync(ct);

        var pageSize = request.PageSize == -1 ? totalCount : request.PageSize;
        var skip = request.PageSize == -1 ? 0 : (request.PageNumber - 1) * request.PageSize;

        // Natural ordering ("A2" before "A10") can't be translated to SQL, which sorts strings
        // lexicographically ("A10" < "A2"). Only the columns needed to determine sort/paging are
        // pulled for the full matching set; full row data is fetched only for the page returned.
        var sortKeys = await query
            .Select(pm => new { pm.Id, pm.PropertyNo, pm.PartitionNo })
            .ToListAsync(ct);

        var pagedIds = sortKeys
            .OrderBy(p => p.PropertyNo ?? string.Empty, NaturalStringComparer.Instance)
            .ThenBy(p => p.PartitionNo ?? string.Empty, NaturalStringComparer.Instance)
            .Skip(skip)
            .Take(pageSize == 0 ? 1 : pageSize)
            .Select(p => p.Id)
            .ToList();

        // WardNo is constant for every row here since the query is already scoped to one WardId.
        var wardNo = await _context.WardMaster
            .Where(w => w.Id == request.WardId)
            .Select(w => w.WardNo)
            .FirstOrDefaultAsync(ct);

        var propertiesById = await _context.PropertyMast
            .Where(pm => pagedIds.Contains(pm.Id))
            .Select(pm => new { pm.Id, pm.WardId, pm.PropertyNo, pm.PartitionNo })
            .ToDictionaryAsync(pm => pm.Id, ct);

        var pagedProperties = pagedIds
            .Select(id => propertiesById[id])
            .Select(pm => new { pm.Id, pm.WardId, WardNo = wardNo, pm.PropertyNo, pm.PartitionNo })
            .ToList();

        var propertyIds = pagedProperties.Select(p => p.Id).ToList();
        var locksByProperty = await GetLockedScreensByPropertyAsync(propertyIds, ct);

        var items = pagedProperties.Select(p =>
        {
            var lockedScreens = locksByProperty.GetValueOrDefault(p.Id) ?? new List<LockableScreenDto>();
            return new PropertyLockRowDto
            {
                PropertyId = p.Id,
                WardId = p.WardId,
                WardNo = p.WardNo ?? string.Empty,
                PropertyNo = p.PropertyNo ?? string.Empty,
                PartitionNo = p.PartitionNo ?? string.Empty,
                IsLocked = lockedScreens.Count > 0,
                LockedScreens = lockedScreens,
            };
        }).ToList();

        return new PagedResult<PropertyLockRowDto>(items, totalCount, request.PageNumber, pageSize);
    }

    /// <summary>
    /// Same category-based scoping as PropertySearchByCategory (delegated to
    /// IPropertySearchService, which owns the Zone/Ward/Building/Range validation and the
    /// SQL-side natural sort/pagination), enriched here with each returned property's lock status.
    /// </summary>
    public async Task<PagedResult<PropertyLockRowDto>> GetPropertyLocksByCategoryAsync(
        PropertySearchByCategoryQueryParameters request, CancellationToken ct)
    {
        var searchResult = await _propertySearchService.SearchByCategoryAsync(request, ct);

        var propertyIds = searchResult.Items.Select(p => p.PropertyId).ToList();
        var locksByProperty = await GetLockedScreensByPropertyAsync(propertyIds, ct);

        var items = searchResult.Items.Select(p =>
        {
            var lockedScreens = locksByProperty.GetValueOrDefault(p.PropertyId) ?? new List<LockableScreenDto>();
            return new PropertyLockRowDto
            {
                PropertyId = p.PropertyId,
                WardId = p.WardId,
                WardNo = p.WardNo ?? string.Empty,
                PropertyNo = p.PropertyNo ?? string.Empty,
                PartitionNo = p.PartitionNo ?? string.Empty,
                IsLocked = lockedScreens.Count > 0,
                LockedScreens = lockedScreens,
            };
        }).ToList();

        return new PagedResult<PropertyLockRowDto>(items, searchResult.TotalCount, searchResult.PageNumber, searchResult.PageSize);
    }

    /// <summary>
    /// Loads the currently-locked screens for a set of property IDs, grouped by PropertyId.
    /// </summary>
    private async Task<Dictionary<int, List<LockableScreenDto>>> GetLockedScreensByPropertyAsync(List<int> propertyIds, CancellationToken ct)
    {
        var locks = await _context.PropertyScreenLocks
            .Where(l => propertyIds.Contains(l.PropertyId) && l.IsLocked && !l.MarkedForDeletion)
            .Join(_context.ScreenMaster, l => l.LockableScreenId, s => s.Id,
                (l, s) => new
                {
                    l.PropertyId,
                    Screen = new LockableScreenDto
                    {
                        Id = s.Id,
                        ScreenCode = s.ScreenCode ?? string.Empty,
                        ScreenName = s.ScreenName ?? string.Empty,
                        ScreenNameLocal = s.ScreenNameLocal,
                        DisplayOrder = s.DisplayOrder,
                    },
                })
            .ToListAsync(ct);

        return locks
            .GroupBy(x => x.PropertyId)
            .ToDictionary(g => g.Key, g => g
                .Select(x => x.Screen)
                .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
                .ThenBy(s => s.ScreenName)
                .ToList());
    }

    public async Task<BulkLockResultDto> BulkApplyAsync(
        BulkLockRequestDto request, int actingUserId, CancellationToken ct)
    {
        var action = (request.Action ?? string.Empty).Trim().ToLowerInvariant();
        if (action != ActionLock && action != ActionUnlock)
            throw new ArgumentException("Action must be 'lock' or 'unlock'.");

        if (request.PropertyIds == null || request.PropertyIds.Count == 0)
            throw new ArgumentException("At least one property must be selected.");


        if (request.ScreenIds == null || request.ScreenIds.Count == 0)
            throw new ArgumentException("At least one screen must be selected.");

        // Dedupe up front so TotalRequested reflects the distinct (PropertyId, ScreenId) pairs the
        // MERGE below actually targets - otherwise duplicate ids in the request would inflate
        // TotalRequested beyond the cross-join size and misreport FailedCount.
        var requestedPropertyIds = request.PropertyIds.Distinct().ToList();
        var requestedScreenIds = request.ScreenIds.Distinct().ToList();

        var result = new BulkLockResultDto
        {
            TotalRequested = requestedPropertyIds.Count * requestedScreenIds.Count,
        };

        var validPropertyIds = await _context.PropertyMast
            .Where(p => requestedPropertyIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var validScreenIds = await _context.ScreenMaster
            .Where(s => requestedScreenIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var missingProperties = requestedPropertyIds.Except(validPropertyIds).ToList();
        var missingScreens = requestedScreenIds.Except(validScreenIds).ToList();

        foreach (var id in missingProperties)
            result.Errors.Add($"Property {id} not found or inactive.");
        foreach (var id in missingScreens)
            result.Errors.Add($"Screen {id} not found or inactive.");

        if (validPropertyIds.Count == 0 || validScreenIds.Count == 0)
        {
            result.FailedCount = result.TotalRequested;
            return result;
        }

        var now = DateTime.Now;
        var shouldLock = action == ActionLock;
        var propertyIdsJson = JsonSerializer.Serialize(validPropertyIds);
        var screenIdsJson = JsonSerializer.Serialize(validScreenIds);

        // Single set-based MERGE (update-existing + insert-missing in one round trip) instead of a
        // per-pair loop, so bulk locking a whole zone (tens of thousands of PropertyId x ScreenId
        // pairs) stays fast. IDs are passed as JSON (expanded server-side via OPENJSON) rather than
        // SQL IN-lists so there's no parameter-count limit and no chunking needed regardless of size.
        int affected;
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                ;WITH Props AS (SELECT CAST([value] AS INT) AS Id FROM OPENJSON({propertyIdsJson})),
                      Screens AS (SELECT CAST([value] AS INT) AS Id FROM OPENJSON({screenIdsJson}))
                MERGE [PTIS].[PropertyScreenLock] WITH (HOLDLOCK) AS target
                USING (SELECT p.Id AS PropertyId, s.Id AS LockableScreenId FROM Props p CROSS JOIN Screens s) AS source
                    ON target.PropertyId = source.PropertyId AND target.LockableScreenId = source.LockableScreenId
                WHEN MATCHED THEN UPDATE SET
                    IsLocked = {shouldLock},
                    MarkedForDeletion = 0,
                    MarkedForDeletionDate = NULL,
                    IsActive = 1,
                    UpdatedBy = {actingUserId},
                    UpdatedDate = {now},
                    CreatedBy = COALESCE(target.CreatedBy, {actingUserId}),
                    CreatedDate = COALESCE(target.CreatedDate, {now}),
                    LockedBy = CASE WHEN {shouldLock} = 1 THEN {actingUserId} ELSE target.LockedBy END,
                    LockedDate = CASE WHEN {shouldLock} = 1 THEN {now} ELSE target.LockedDate END,
                    UnlockedBy = CASE WHEN {shouldLock} = 0 THEN {actingUserId} ELSE target.UnlockedBy END,
                    UnlockedDate = CASE WHEN {shouldLock} = 0 THEN {now} ELSE target.UnlockedDate END
                WHEN NOT MATCHED THEN INSERT
                    (PropertyId, LockableScreenId, IsLocked, IsActive, CreatedBy, CreatedDate, LockedBy, LockedDate, UnlockedBy, UnlockedDate, MarkedForDeletion)
                    VALUES
                    (source.PropertyId, source.LockableScreenId, {shouldLock}, 1, {actingUserId}, {now},
                     CASE WHEN {shouldLock} = 1 THEN {actingUserId} ELSE NULL END,
                     CASE WHEN {shouldLock} = 1 THEN {now} ELSE NULL END,
                     CASE WHEN {shouldLock} = 0 THEN {actingUserId} ELSE NULL END,
                     CASE WHEN {shouldLock} = 0 THEN {now} ELSE NULL END,
                     0);
            ", ct);

            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Bulk {Action} failed for {PropertyCount} properties x {ScreenCount} screens",
                action, validPropertyIds.Count, validScreenIds.Count);
            throw;
        }

        // Every pair in the cross join is always matched-or-inserted, so a successful MERGE always
        // affects validPropertyIds.Count * validScreenIds.Count rows; the remainder are pairs lost to
        // invalid/inactive property or screen ids (already reported in result.Errors above).
        result.SuccessCount = affected;
        result.FailedCount = result.TotalRequested - affected;

        return result;
    }

    /// <summary>
    /// Bulk lock/unlock scoped by SearchCategory instead of an explicit PropertyIds list - the
    /// server resolves every matching property (potentially thousands, e.g. a whole Zone) via
    /// <see cref="IPropertySearchService.ResolvePropertyIdsByCategoryAsync"/>, then applies the
    /// action with two set-based SQL operations (one ExecuteUpdate for existing lock rows, one
    /// AddRange+SaveChanges for missing ones) instead of looping per (property, screen) pair -
    /// trading per-pair partial-success reporting for speed at this scale (single transaction,
    /// all-or-nothing).
    /// </summary>
    /// <remarks>
    /// Action and ScreenIds shape (required, ScreenIds non-empty, Action is "lock"/"unlock") is
    /// validated declaratively on <see cref="BulkLockByCategoryRequestDto"/> via Data Annotations,
    /// enforced automatically at the API boundary by [ApiController]'s model validation - this
    /// method trusts that and only validates the SearchCategory scope (which needs conditional,
    /// per-category rules Data Annotations can't express).
    /// </remarks>
    public async Task<BulkLockResultDto> BulkApplyByCategoryAsync(
        BulkLockByCategoryRequestDto request, int actingUserId, CancellationToken ct)
    {
        var action = request.Action.Trim().ToLowerInvariant();

        // Map the narrow bulk scope onto the full search-by-category query parameters so
        // ResolvePropertyIdsByCategoryAsync's existing per-category validation (ZoneId required
        // for ZoneWise, WardId+PropertyFrom required for FromToProperty, etc.) applies unchanged -
        // pagination and the extra grid filters (PartType/PropertyCategoryName/IsWing/SearchTerm)
        // simply aren't part of the bulk request contract.
        var scopeQueryParameters = new PropertySearchByCategoryQueryParameters
        {
            SearchCategory = request.Scope.SearchCategory,
            ZoneId = request.Scope.ZoneId,
            WardId = request.Scope.WardId,
            PropertyNo = request.Scope.PropertyNo,
            PartitionNo = request.Scope.PartitionNo,
            PropertyFrom = request.Scope.PropertyFrom,
            PropertyTo = request.Scope.PropertyTo
        };

        var propertyIds = await _propertySearchService.ResolvePropertyIdsByCategoryAsync(scopeQueryParameters, ct);

        // Dedupe up front so TotalRequested reflects the distinct screens actually applied below -
        // otherwise duplicate ids in the request would inflate TotalRequested and misreport
        // FailedCount even though the desired state was fully applied (same fix as BulkApplyAsync).
        var requestedScreenIds = request.ScreenIds.Distinct().ToList();

        var validScreenIds = await _context.ScreenMaster
            .Where(s => requestedScreenIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var totalRequested = propertyIds.Count * requestedScreenIds.Count;
        var result = new BulkLockResultDto { TotalRequested = totalRequested };

        var missingScreens = requestedScreenIds.Except(validScreenIds).ToList();
        foreach (var id in missingScreens)
            result.Errors.Add($"Screen {id} not found or inactive.");

        if (propertyIds.Count == 0 || validScreenIds.Count == 0)
        {
            result.FailedCount = totalRequested;
            return result;
        }

        var now = DateTime.Now;
        var shouldLock = action == ActionLock;
        var missingPairs = new List<PropertyScreenLockEntity>();

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var chunk in propertyIds.Chunk(PropertyIdChunkSize))
            {
                var chunkIds = chunk.ToList();

                // Find which (property, screen) pairs in this chunk already have a lock row, so
                // the update phase only runs when there's something to update, and the insert
                // phase (below) knows which pairs are missing (must avoid violating
                // UQ_PropertyScreenLock_Property_Screen).
                var existingPairs = await _context.PropertyScreenLocks
                    .Where(l => chunkIds.Contains(l.PropertyId) && validScreenIds.Contains(l.LockableScreenId))
                    .Select(l => new { l.PropertyId, l.LockableScreenId })
                    .ToListAsync(ct);

                // Update phase: one set-based UPDATE for every existing pair - no entity
                // loading/tracking. Skipped entirely when nothing exists yet for this chunk
                // (e.g. first-time locking a batch of properties that never had a lock row).
                if (existingPairs.Count > 0)
                {
                    await _context.PropertyScreenLocks
                        .Where(l => chunkIds.Contains(l.PropertyId) && validScreenIds.Contains(l.LockableScreenId))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(l => l.IsLocked, shouldLock)
                            .SetProperty(l => l.MarkedForDeletion, false)
                            .SetProperty(l => l.MarkedForDeletionDate, (DateTime?)null)
                            .SetProperty(l => l.IsActive, true)
                            .SetProperty(l => l.UpdatedBy, actingUserId)
                            .SetProperty(l => l.UpdatedDate, now)
                            .SetProperty(l => l.CreatedBy, l => l.CreatedBy ?? actingUserId)
                            .SetProperty(l => l.CreatedDate, l => l.CreatedDate ?? now)
                            .SetProperty(l => l.LockedBy, l => shouldLock ? actingUserId : l.LockedBy)
                            .SetProperty(l => l.LockedDate, l => shouldLock ? now : l.LockedDate)
                            .SetProperty(l => l.UnlockedBy, l => shouldLock ? l.UnlockedBy : actingUserId)
                            .SetProperty(l => l.UnlockedDate, l => shouldLock ? l.UnlockedDate : now),
                            ct);
                }

                // Insert phase: accumulate the pairs that don't have a row yet.
                var existingSet = existingPairs.Select(p => (p.PropertyId, p.LockableScreenId)).ToHashSet();

                foreach (var propertyId in chunkIds)
                {
                    foreach (var screenId in validScreenIds)
                    {
                        if (existingSet.Contains((propertyId, screenId)))
                            continue;

                        missingPairs.Add(new PropertyScreenLockEntity
                        {
                            PropertyId = propertyId,
                            LockableScreenId = screenId,
                            IsLocked = shouldLock,
                            IsActive = true,
                            CreatedBy = actingUserId,
                            CreatedDate = now,
                            LockedBy = shouldLock ? actingUserId : null,
                            LockedDate = shouldLock ? now : null,
                            UnlockedBy = shouldLock ? null : actingUserId,
                            UnlockedDate = shouldLock ? null : now
                        });
                    }
                }
            }

            if (missingPairs.Count > 0)
                await _context.PropertyScreenLocks.AddRangeAsync(missingPairs, ct);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        result.SuccessCount = propertyIds.Count * validScreenIds.Count;
        result.FailedCount = totalRequested - result.SuccessCount;
        return result;
    }
}
