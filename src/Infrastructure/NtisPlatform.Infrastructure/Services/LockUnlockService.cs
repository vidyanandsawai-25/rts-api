using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

public class LockUnlockService : ILockUnlockService
{
    private const string ActionLock = "lock";
    private const string ActionUnlock = "unlock";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<LockUnlockService> _logger;

    public LockUnlockService(ApplicationDbContext context, ILogger<LockUnlockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<LockableScreenDto>> GetLockableScreensAsync(CancellationToken ct)
    {
        return await _context.ScreenMaster
            .AsNoTracking()
            .Where(s => s.IsActive == true && s.IsPropertyLockable == true)
            .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
            .ThenBy(s => s.ScreenName)
            .Select(s => new LockableScreenDto
            {
                Id = s.Id,
                ScreenCode = s.ScreenCode ?? string.Empty,
                ScreenName = s.ScreenName ?? string.Empty,
                ScreenNameLocal = s.ScreenNameLocal,
                DisplayOrder = s.DisplayOrder,
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
                query = query.Where(pm => pm.PartitionNo != null && partitionNumbers.Contains(pm.PartitionNo));
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

        var pagedProperties = await query
            .OrderBy(pm => pm.PropertyNo)
            .Skip(skip)
            .Take(pageSize == 0 ? 1 : pageSize)
            .Join(_context.WardMaster, pm => pm.WardId, w => w.Id,
                (pm, w) => new
                {
                    pm.Id,
                    pm.WardId,
                    WardNo = w.WardNo,
                    pm.PropertyNo,
                    pm.PartitionNo,
                })
            .ToListAsync(ct);

        var propertyIds = pagedProperties.Select(p => p.Id).ToList();

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

        var locksByProperty = locks
            .GroupBy(x => x.PropertyId)
            .ToDictionary(g => g.Key, g => g
                .Select(x => x.Screen)
                .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
                .ThenBy(s => s.ScreenName)
                .ToList());

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

        var result = new BulkLockResultDto
        {
            TotalRequested = request.PropertyIds.Count * request.ScreenIds.Count,
        };

        var validPropertyIds = await _context.PropertyMast
            .Where(p => request.PropertyIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var validScreenIds = await _context.ScreenMaster
            .Where(s => request.ScreenIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var missingProperties = request.PropertyIds.Except(validPropertyIds).ToList();
        var missingScreens = request.ScreenIds.Except(validScreenIds).ToList();

        foreach (var id in missingProperties)
            result.Errors.Add($"Property {id} not found or inactive.");
        foreach (var id in missingScreens)
            result.Errors.Add($"Screen {id} not found or inactive.");

        if (validPropertyIds.Count == 0 || validScreenIds.Count == 0)
        {
            result.FailedCount = result.TotalRequested;
            return result;
        }

        var existing = await _context.PropertyScreenLocks
            .Where(l => validPropertyIds.Contains(l.PropertyId) && validScreenIds.Contains(l.LockableScreenId))
            .ToListAsync(ct);

        var existingByKey = existing.ToDictionary(l => (l.PropertyId, l.LockableScreenId));
        var now = DateTime.Now;
        var shouldLock = action == ActionLock;

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var propertyId in validPropertyIds)
            {
                foreach (var screenId in validScreenIds)
                {
                    try
                    {
                        if (existingByKey.TryGetValue((propertyId, screenId), out var row))
                        {
                            // Ensure CreatedBy and CreatedDate are set (handle legacy records with NULL values)
                            if (row.CreatedBy == null)
                                row.CreatedBy = actingUserId;
                            if (row.CreatedDate == null)
                                row.CreatedDate = now;

                            row.IsLocked = shouldLock;
                            row.MarkedForDeletion = false;
                            row.MarkedForDeletionDate = null;
                            row.IsActive = true;
                            row.UpdatedBy = actingUserId;
                            row.UpdatedDate = now;
                            if (shouldLock)
                            {
                                row.LockedBy = actingUserId;
                                row.LockedDate = now;
                            }
                            else
                            {
                                row.UnlockedBy = actingUserId;
                                row.UnlockedDate = now;
                            }
                        }
                        else
                        {
                            var entity = new PropertyScreenLockEntity
                            {
                                PropertyId = propertyId,
                                LockableScreenId = screenId,
                                IsLocked = shouldLock,
                                IsActive = true,
                                CreatedBy = actingUserId,
                                CreatedDate = now,
                            };
                            if (shouldLock)
                            {
                                entity.LockedBy = actingUserId;
                                entity.LockedDate = now;
                            }
                            else
                            {
                                entity.UnlockedBy = actingUserId;
                                entity.UnlockedDate = now;
                            }
                            await _context.PropertyScreenLocks.AddAsync(entity, ct);
                        }
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"PropertyId={propertyId}, ScreenId={screenId}: {ex.Message}");
                        _logger.LogError(ex, "Failed to apply lock for property {PropertyId} screen {ScreenId}", propertyId, screenId);
                    }
                }
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var missingPairs = result.TotalRequested - (validPropertyIds.Count * validScreenIds.Count);
        if (missingPairs > 0)
            result.FailedCount += missingPairs;

        return result;
    }
}
