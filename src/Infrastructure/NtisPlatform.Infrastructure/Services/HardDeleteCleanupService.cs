using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Reflection;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service implementation for cleaning up entities marked for hard deletion.
/// Also handles localization cleanup for deleted entities.
/// </summary>
public class HardDeleteCleanupService : IHardDeleteCleanupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<HardDeleteCleanupService> _logger;

    public HardDeleteCleanupService(
        ApplicationDbContext context,
        ILocalizationService localizationService,
        ILogger<HardDeleteCleanupService> logger)
    {
        _context = context;
        _localizationService = localizationService;
        _logger = logger;
    }

    public async Task<int> CleanupMarkedEntitiesAsync(int retentionDays = 0, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.Now.AddDays(-retentionDays);
        var totalDeleted = 0;

        _logger.LogInformation("Starting hard delete cleanup task. Retention days: {RetentionDays}, Cutoff date: {CutoffDate}",
            retentionDays, cutoffDate);

        try
        {
            totalDeleted += await CleanupEntityType<Core.Entities.PropertyEntity>(cutoffDate, cancellationToken);
            totalDeleted += await CleanupEntityType<Core.Entities.Master.UserEntity>(cutoffDate, cancellationToken);
            totalDeleted += await CleanupEntityType<Core.Entities.Rules.RuleEngineEntity>(cutoffDate, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Hard delete cleanup completed. Total entities deleted: {TotalDeleted}", totalDeleted);
            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during hard delete cleanup");
            throw;
        }
    }

    private async Task<int> CleanupEntityType<TEntity>(DateTime cutoffDate, CancellationToken cancellationToken)
        where TEntity : class, IHardDeletable
    {
        try
        {
            var entitiesToDelete = await _context.Set<TEntity>()
                .Where(e => e.MarkedForDeletion &&
                           e.MarkedForDeletionDate.HasValue &&
                           e.MarkedForDeletionDate.Value <= cutoffDate)
                .ToListAsync(cancellationToken);

            if (entitiesToDelete.Count > 0)
            {
                _context.Set<TEntity>().RemoveRange(entitiesToDelete);

                _logger.LogInformation("Marked {Count} {EntityType} entities for permanent deletion",
                    entitiesToDelete.Count, typeof(TEntity).Name);

                return entitiesToDelete.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public async Task MarkForHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        // TODO: Implement mark for hard delete logic
        await Task.CompletedTask;
    }

    public async Task UnmarkForHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        // TODO: Implement unmark for hard delete logic
        await Task.CompletedTask;
    }

    public async Task<bool> ForceHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        try
        {
            var entity = await _context.Set<TEntity>().FindAsync([id], cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Force hard delete failed - {EntityType} with ID {Id} not found",
                    typeof(TEntity).Name, id);
                return false;
            }

            var localizationKeys = ExtractPotentialLocalizationKeys(entity);

            _context.Set<TEntity>().Remove(entity);

            Dictionary<string, List<string>> keysToInvalidate = [];
            if (localizationKeys.Count > 0)
            {
                keysToInvalidate = await DeleteLocalizationRowsByKeysAsync(localizationKeys, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var (resource, keys) in keysToInvalidate)
            {
                _localizationService.InvalidateKeys(resource, keys);
            }

            _logger.LogInformation("Force hard delete completed for {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during force hard delete of {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
    }

    public async Task<BulkResult<TKey>> BulkForceHardDeleteAsync<TEntity, TKey>(TKey[] ids, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        if (ids == null || ids.Length == 0)
        {
            return new BulkResult<TKey>(0, 0, []);
        }

        _logger.LogInformation("Starting bulk force hard delete for {Count} {EntityType} entities",
            ids.Length, typeof(TEntity).Name);

        var deletedIds = new List<TKey>();
        var errors = new List<string>();
        var failedCount = 0;
        var allLocalizationKeys = new List<string>();

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var entity = await _context.Set<TEntity>().FindAsync([id], cancellationToken);

                if (entity == null)
                {
                    failedCount++;
                    errors.Add($"{typeof(TEntity).Name} with ID {id} not found");
                    _logger.LogWarning("Bulk force hard delete - {EntityType} with ID {Id} not found",
                        typeof(TEntity).Name, id);
                }
                else
                {
                    allLocalizationKeys.AddRange(ExtractPotentialLocalizationKeys(entity));
                    _context.Set<TEntity>().Remove(entity);
                    deletedIds.Add(id);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add($"Error deleting {typeof(TEntity).Name} with ID {id}: {ex.Message}");
                _logger.LogError(ex, "Error during bulk force hard delete of {EntityType} with ID {Id}",
                    typeof(TEntity).Name, id);
            }
        }

        try
        {
            Dictionary<string, List<string>> keysToInvalidate = [];
            if (allLocalizationKeys.Count > 0)
            {
                keysToInvalidate = await DeleteLocalizationRowsByKeysAsync(allLocalizationKeys, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var (resource, keys) in keysToInvalidate)
            {
                _localizationService.InvalidateKeys(resource, keys);
            }

            _logger.LogInformation("Bulk force hard delete completed. Success: {SuccessCount}, Failed: {FailedCount}",
                deletedIds.Count, failedCount);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Constraint violation or database error during bulk force hard delete. No entities deleted.");
            errors.Add("Bulk delete failed due to database constraint violation. No entities were deleted.");
            return new BulkResult<TKey>(0, ids.Length, [], errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes during bulk force hard delete");
            throw;
        }

        return new BulkResult<TKey>(
            deletedIds.Count,
            failedCount,
            deletedIds,
            errors.Count > 0 ? errors : null);
    }

    // =======================
    // LOCALIZATION HELPERS
    // =======================

    /// <summary>
    /// Cache of Entity type to DTO type mappings.
    /// Built automatically by scanning DTOs with [IsLocalizable] properties.
    /// </summary>
    private static readonly Lazy<Dictionary<Type, Type>> _entityToDtoMap = new(BuildEntityToDtoMap);

    /// <summary>
    /// Gets the Application assembly using a known type reference.
    /// More reliable than scanning AppDomain assemblies.
    /// </summary>
    private static readonly Lazy<Assembly?> _applicationAssembly = new(() =>
    {
        try
        {
            // Use a known type from Application assembly to ensure it's loaded
            return typeof(ILocalization).Assembly;
        }
        catch
        {
            const string assemblyPrefix = "NtisPlatform.Application";
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Equals(assemblyPrefix, StringComparison.OrdinalIgnoreCase) == true);
        }
    });

    /// <summary>
    /// Gets the Core assembly using a known type reference.
    /// </summary>
    private static readonly Lazy<Assembly?> _coreAssembly = new(() =>
    {
        // Use a known type from Core assembly
        return typeof(IsLocalizableAttribute).Assembly;
    });

    /// <summary>
    /// Cached MethodInfo for ExtractLocalizationKeysWithDto to avoid repeated reflection.
    /// </summary>
    private static readonly Lazy<MethodInfo> _extractLocalizationKeysMethod = new(() =>
        typeof(HardDeleteCleanupService)
            .GetMethod(nameof(ExtractLocalizationKeysWithDto), BindingFlags.NonPublic | BindingFlags.Static)!);

    /// <summary>
    /// Cache for generic method instances per (Entity, DTO) type pair.
    /// Avoids repeated MakeGenericMethod calls which are expensive.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type Entity, Type Dto), MethodInfo> _genericMethodCache = new();

    /// <summary>
    /// Builds Entity→DTO mapping by scanning DTOs with [IsLocalizable] properties.
    /// 
    /// Resolution order for each DTO:
    /// 1. [LocalizableEntity(typeof(Entity1), ...)] attribute (explicit, preferred)
    /// 2. Naming convention: {BaseName}Dto → {BaseName}Entity (fallback)
    /// </summary>
    private static Dictionary<Type, Type> BuildEntityToDtoMap()
    {
        var map = new Dictionary<Type, Type>();

        var appAssembly = _applicationAssembly.Value;
        var coreAssembly = _coreAssembly.Value;

        if (appAssembly == null || coreAssembly == null)
            return map;

        // Get all entity types from Core assembly
        var entityTypes = coreAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Entity", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(t => GetEntityBaseName(t.Name), t => t, StringComparer.OrdinalIgnoreCase);

        // Scan all DTOs with [IsLocalizable] properties
        var localizableDtos = appAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetProperties().Any(p => p.GetCustomAttribute<IsLocalizableAttribute>() != null))
            .ToList();

        foreach (var dtoType in localizableDtos)
        {
            // 1. Check for explicit [LocalizableEntity] attribute
            var attribute = dtoType.GetCustomAttribute<LocalizableEntityAttribute>();
            if (attribute != null)
            {
                foreach (var entityType in attribute.EntityTypes)
                {
                    map[entityType] = dtoType;
                }
            }
            else
            {
                // 2. Fall back to naming convention
                var baseName = GetDtoBaseName(dtoType.Name);
                if (entityTypes.TryGetValue(baseName, out var entityType))
                {
                    map[entityType] = dtoType;
                }
            }
        }

        return map;
    }

    private static string GetEntityBaseName(string entityName)
    {
        return entityName.EndsWith("Entity", StringComparison.OrdinalIgnoreCase)
            ? entityName[..^6]
            : entityName;
    }

    private static string GetDtoBaseName(string dtoName)
    {
        if (dtoName.EndsWith("DTO", StringComparison.OrdinalIgnoreCase))
            return dtoName[..^3];
        if (dtoName.EndsWith("Dto", StringComparison.OrdinalIgnoreCase))
            return dtoName[..^3];
        if (dtoName.EndsWith("Response", StringComparison.OrdinalIgnoreCase))
            return dtoName[..^8];
        return dtoName;
    }

    private static Type? GetDtoTypeForEntity(Type entityType)
    {
        return _entityToDtoMap.Value.TryGetValue(entityType, out var dtoType) ? dtoType : null;
    }

    /// <summary>
    /// Extracts localization keys from an entity using cached reflection.
    /// </summary>
    private static List<string> ExtractPotentialLocalizationKeys<TEntity>(TEntity entity)
        where TEntity : class
    {
        var dtoType = GetDtoTypeForEntity(typeof(TEntity));
        if (dtoType == null)
            return [];

        // Cache the generic method to avoid repeated MakeGenericMethod calls
        var method = _genericMethodCache.GetOrAdd(
            (typeof(TEntity), dtoType),
            key => _extractLocalizationKeysMethod.Value.MakeGenericMethod(key.Entity, key.Dto));

        return (List<string>)method.Invoke(null, [entity])!;
    }

    /// <summary>
    /// Cache for entity property dictionaries to avoid repeated reflection.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _entityPropsCache = new();

    /// <summary>
    /// Cache for DTO localizable property info to avoid repeated reflection.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, List<(PropertyInfo Prop, IsLocalizableAttribute Attr)>> _dtoLocalizablePropsCache = new();

    private static List<string> ExtractLocalizationKeysWithDto<TEntity, TDto>(TEntity entity)
        where TEntity : class
        where TDto : class
    {
        var keys = new List<string>();

        // Cache entity properties
        var entityProps = _entityPropsCache.GetOrAdd(typeof(TEntity), t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase));

        // Cache DTO localizable properties
        var dtoLocalizableProps = _dtoLocalizablePropsCache.GetOrAdd(typeof(TDto), t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => (Prop: p, Attr: p.GetCustomAttribute<IsLocalizableAttribute>()))
                .Where(x => x.Attr != null && x.Prop.PropertyType == typeof(string))
                .Select(x => (x.Prop, x.Attr!))
                .ToList());

        foreach (var (dtoProp, localizableAttr) in dtoLocalizableProps)
        {
            if (entityProps.TryGetValue(dtoProp.Name, out var entityProp) && entityProp.CanRead)
            {
                var value = entityProp.GetValue(entity) as string;
                if (!string.IsNullOrWhiteSpace(value) && IsLocalizationKey(value, localizableAttr.Resource))
                {
                    keys.Add(value);
                }
            }
        }

        return keys;
    }

    /// <summary>
    /// Validates that a string is a localization key with the expected format: {Resource}_{GUID}_{PropertyName}
    /// </summary>
    private static bool IsLocalizationKey(string value, string resource)
    {
        // Key format: {Resource}_{GUID}_{PropertyName}
        // Must start with {Resource}_
        if (value.Length <= resource.Length + 1
            || value[resource.Length] != '_'
            || !value.AsSpan(0, resource.Length).Equals(resource.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Must have at least one more underscore for the property name part: {Resource}_{Id}_{Property}
        var secondUnderscore = value.IndexOf('_', resource.Length + 1);
        return secondUnderscore > resource.Length + 1 && secondUnderscore < value.Length - 1;
    }

    /// <summary>
    /// Stages MultilingualResourceEntity rows for deletion and returns metadata for cache invalidation.
    /// Caller MUST call SaveChangesAsync and invalidate cache ONLY after successful save.
    /// </summary>
    private async Task<Dictionary<string, List<string>>> DeleteLocalizationRowsByKeysAsync(List<string> keys, CancellationToken cancellationToken)
    {
        // De-duplicate keys to minimize SQL IN clause and avoid parameter limits
        var distinctKeys = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctKeys.Count == 0)
            return [];

        List<Core.Entities.MultilingualResourceEntity> localizationRows;

        // Batch if keys exceed SQL Server parameter limit (~1000)
        if (distinctKeys.Count <= 1000)
        {
            localizationRows = await _context.MultilingualResourceEntity
                .Where(x => distinctKeys.Contains(x.Key))
                .ToListAsync(cancellationToken);
        }
        else
        {
            localizationRows = [];
            foreach (var batch in distinctKeys.Chunk(1000))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchList = batch.ToList();
                var rows = await _context.MultilingualResourceEntity
                    .Where(x => batchList.Contains(x.Key))
                    .ToListAsync(cancellationToken);
                localizationRows.AddRange(rows);
            }
        }

        if (localizationRows.Count == 0)
            return [];

        // Stage deletes (not yet persisted)
        _context.MultilingualResourceEntity.RemoveRange(localizationRows);

        // Return metadata for cache invalidation AFTER SaveChangesAsync succeeds
        var keysByResource = localizationRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Resource) && !string.IsNullOrWhiteSpace(r.Key))
            .GroupBy(r => r.Resource!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.Key!).ToList(),
                StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Staged {Count} localization rows for deletion (pending SaveChanges)", localizationRows.Count);

        return keysByResource;
    }
}