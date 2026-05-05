using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Infrastructure.Data;
using System.Collections.Concurrent;

namespace NtisPlatform.Infrastructure.Services.Localization;

/// Loads localization rows from DB and stores them in-memory:  Cache[(resource,language)][key] = value &amp; Very fast lookups; reloadable/invalidate-able.
public sealed class LocalizationService : ILocalizationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    private sealed class ResourceLanguageComparer : IEqualityComparer<(string Resource, string Language)>
    {
        public static readonly ResourceLanguageComparer Instance = new();

        public bool Equals((string Resource, string Language) x, (string Resource, string Language) y)
        {
            return string.Equals(x.Resource, y.Resource, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.Language, y.Language, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Resource, string Language) obj)
        {
            return HashCode.Combine(
                obj.Resource != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Resource) : 0,
                obj.Language != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Language) : 0);
        }
    }

    private readonly ConcurrentDictionary<(string Resource, string Language), Dictionary<string, string>> _cache = new(ResourceLanguageComparer.Instance);

    // Avoid parallel reload storms
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    // Lock for thread-safe bucket modifications
    private readonly object _bucketLock = new();

    public LocalizationService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }
    
    public string GetTranslation(string resource, string language, string key)
    {
        var normLanguage = Normalizelanguage(language);
        if (TryGet(resource, normLanguage, key, out var value))
        {
            return value;
        }

        if (normLanguage != "en" && TryGet(resource, "en", key, out value))
        {
            return value;
        }

        return key;
    }

    public Dictionary<string, string> GetTranslations(string resource, string language, IEnumerable<string> keys)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normLanguage = Normalizelanguage(language);
        var fallbackToEn = normLanguage != "en";

        foreach (var key in keys)
        {
            if (TryGet(resource, normLanguage, key, out var value))
            {
                result[key] = value;
            }
            else if (fallbackToEn && TryGet(resource, "en", key, out value))
            {
                result[key] = value;
            }
            else
            {
                result[key] = key;
            }
        }

        return result;
    }

    public bool TryGetTranslation(string resource, string language, string key, out string? value)
    {
        value = null;
        var normlanguage = Normalizelanguage(language);

        if (!_cache.TryGetValue((resource, normlanguage), out var dict))
            return false;

        if (dict.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        return false;
    }

    public Dictionary<string, string> GetTranslationsExact(string resource, string language, IEnumerable<string> keys)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normlanguage = Normalizelanguage(language);

        if (!_cache.TryGetValue((resource, normlanguage), out var dict))
            return result; // Empty - nothing cached for this language

        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var value))
            {
                result[key] = value;
            }
            // Missing keys are NOT added to result
        }

        return result;
    }

    public void SetTranslation(string resource, string language, string key, string value)
    {
        var normlanguage = Normalizelanguage(language);
        var tupleKey = (resource, normlanguage);

        lock (_bucketLock)
        {
            if (_cache.TryGetValue(tupleKey, out var existing))
            {
                // Create a copy, update, replace atomically
                var copy = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase)
                {
                    [key] = value
                };
                _cache[tupleKey] = copy;
            }
            else
            {
                // Create new bucket
                var newDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [key] = value
                };
                _cache[tupleKey] = newDict;
            }
        }
    }

    public void Invalidate(string? resource = null, string? language = null, string? key = null)
    {
        lock (_bucketLock)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                _cache.Clear();
                return;
            }
            // If language not given -> remove all buckets for this resource (en/hi/mr)
            if (string.IsNullOrWhiteSpace(language))
            {
                foreach (var k in _cache.Keys.Where(k => string.Equals(k.Resource, resource, StringComparison.OrdinalIgnoreCase)).ToArray())
                {
                    _cache.TryRemove(k, out _);
                }
                return;
            }
            var normlanguage = Normalizelanguage(language);
            var tupleKey = (resource, normlanguage);
            // If key not given -> remove whole bucket
            if (string.IsNullOrWhiteSpace(key))
            {
                _cache.TryRemove(tupleKey, out _);
                return;
            }
            // key-level removal
            if (_cache.TryGetValue(tupleKey, out var dict))
            {
                // dict is not thread-safe to mutate if multiple readers
                // safest approach: create a copy, remove key, replace atomically
                var copy = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
                copy.Remove(key);
                _cache[tupleKey] = copy;
            }
        }
    }

    // In LocalizationService.cs - implement threshold behavior
    /// <summary>
    /// Removes multiple keys from cache for a specific resource across all supported languages.
    /// Keys are de-duplicated and converted to HashSet for O(1) lookups.
    /// Falls back to full resource invalidation if key count exceeds threshold.
    /// </summary>
    public void InvalidateKeys(string resource, IEnumerable<string> keys)
    {
        if (string.IsNullOrWhiteSpace(resource))
            return;

        // De-duplicate keys and use HashSet for O(1) lookups
        var keySet = new HashSet<string>(
            keys.Where(k => !string.IsNullOrWhiteSpace(k)),
            StringComparer.OrdinalIgnoreCase);

        if (keySet.Count == 0)
            return;

        // Threshold: If invalidating too many keys, just drop the entire resource to avoid iteration cost
        const int ThresholdForFullInvalidation = 1000;
        if (keySet.Count > ThresholdForFullInvalidation)
        {
            Invalidate(resource);
            return;
        }

        lock (_bucketLock)
        {
            // Get all buckets for this resource
            var resourceBuckets = _cache
                .Where(kvp => string.Equals(kvp.Key.Resource, resource, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var (bucketKey, dict) in resourceBuckets)
            {
                // HashSet.Where with dict.ContainsKey is more efficient than List.Where
                var keysToRemove = keySet.Where(k => dict.ContainsKey(k)).ToList();

                if (keysToRemove.Count == 0)
                    continue;

                // Create a copy, remove keys, replace atomically
                var copy = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
                foreach (var key in keysToRemove)
                {
                    copy.Remove(key);
                }
                _cache[bucketKey] = copy;
            }
        }
    }

    public Task RefreshAsync(string? resource = null, string? language = null, string? key = null, CancellationToken ct = default)
    {
        Invalidate(resource, language, key);
        return ReloadAsync(resource, language, key, excludeGenerated: false, ct);
    }

    public async Task ReloadAsync(string? resource = null, string? language = null, string? key = null, bool excludeGenerated = false, CancellationToken ct = default)
    {
        await _reloadLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var query = db.MultilingualResourceEntity
                .AsNoTracking()
                .Where(x => x.IsActive);

            // Exclude per-entity generated keys (IsGenerated=true) to reduce memory footprint
            // Static catalog keys (validation messages, UI labels) have IsGenerated=false/null
            if (excludeGenerated)
                query = query.Where(x => x.IsGenerated != true);

            // Filter by resource (DB column = Resource)
            if (!string.IsNullOrWhiteSpace(resource))
                query = query.Where(x => x.Resource == resource);

            // Filter by specific key (DB column = Key)
            if (!string.IsNullOrWhiteSpace(key))
                query = query.Where(x => x.Key == key);

            var rows = await query
                .Where(x => x.Resource != null && x.Key != null)
                .Select(x => new LocalizationRow(x.Resource!, x.Key!, x.en_US, x.hi_IN, x.mr_IN))
                .ToListAsync(ct);

            // Nothing to load
            if (rows.Count == 0)
                return;

            // If full reload -> clear everything (prevents stale keys)
            if (string.IsNullOrWhiteSpace(resource) && string.IsNullOrWhiteSpace(language) && string.IsNullOrWhiteSpace(key))
            {
                _cache.Clear();
            }
            else
            {
                // Partial reload: clear only affected buckets/keys first
                Invalidate(resource, language, key);
            }

            // language filter handling
            var requestedLanguage = string.IsNullOrWhiteSpace(language) ? null : Normalizelanguage(language);

            // Group by Resource and build/update buckets 
            foreach (var grp in rows.GroupBy(r => r.Resource, StringComparer.OrdinalIgnoreCase))
            {
                if (requestedLanguage is null)
                {
                    UpsertBucket(grp.Key, "en", grp, static r => r.EnUs);
                    UpsertBucket(grp.Key, "hi", grp, static r => r.HiIn);
                    UpsertBucket(grp.Key, "mr", grp, static r => r.MrIn);
                    continue;
                }

                switch (requestedLanguage)
                {
                    case "hi":
                        UpsertBucket(grp.Key, "hi", grp, static r => r.HiIn);
                        break;
                    case "mr":
                        UpsertBucket(grp.Key, "mr", grp, static r => r.MrIn);
                        break;
                    default:
                        UpsertBucket(grp.Key, "en", grp, static r => r.EnUs);
                        break;
                }
            }
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    /// <summary>
    /// Updates a single cache bucket (resource||language) with values from rows.
    /// </summary>
    private void UpsertBucket(
        string resource,
        string bucketLanguage,
        IEnumerable<LocalizationRow> rows,
        Func<LocalizationRow, string?> valueSelector)
    {
        var tupleKey = (resource, bucketLanguage);

        // Start from existing bucket if present (so key-only reload doesn't wipe other keys)
        var dict = _cache.TryGetValue(tupleKey, out var existing)
            ? new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
                continue;

            var value = valueSelector(row);

            // Only store if there's an actual value for this language
            // Don't store fallback values - let GetTranslations handle fallback at runtime
            if (!string.IsNullOrWhiteSpace(value))
            {
                dict[row.Key] = value;
            }
        }

        _cache[tupleKey] = dict;
    }

    /// <summary>
    /// Strongly-typed record for localization data projection.
    /// </summary>
    private sealed record LocalizationRow(
        string Resource,
        string Key,
        string? EnUs,
        string? HiIn,
        string? MrIn);
    public Dictionary<string, int> GetCacheStats()
    {
        return _cache.ToDictionary(
            kvp => $"{kvp.Key.Resource}||{kvp.Key.Language}",
            kvp => kvp.Value.Count,
            StringComparer.OrdinalIgnoreCase);
    }

    private bool TryGet(string resource, string normalizedLanguage, string key, out string value)
    {
        value = default!;

        if (!_cache.TryGetValue((resource, normalizedLanguage), out var dict))
            return false;

        return dict.TryGetValue(key, out value);
    }

    private static string Normalizelanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en";

        var span = language.AsSpan().Trim();
        var dash = span.IndexOf('-');
        if (dash > 0)
        {
            span = span[..dash];
        }

        if (span.Equals("en", StringComparison.OrdinalIgnoreCase)) return "en";
        if (span.Equals("hi", StringComparison.OrdinalIgnoreCase)) return "hi";
        if (span.Equals("mr", StringComparison.OrdinalIgnoreCase)) return "mr";

        return span.ToString().ToLowerInvariant();
    }
}

/// Preloads localization data into memory on application startup.
public sealed class LocalizationWarmupHostedService : IHostedService
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<LocalizationWarmupHostedService> _logger;

    public LocalizationWarmupHostedService(ILocalizationService localizationService, ILogger<LocalizationWarmupHostedService> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Localization warmup started (loading static catalog translations into memory).");
            // excludeGenerated: true → only load static catalog keys (validation messages, UI labels)
            // Per-entity generated keys are loaded on-demand via LocalizationRepoService.GetAsync
            await _localizationService.ReloadAsync(excludeGenerated: true, ct: cancellationToken);
            
            var stats = _localizationService.GetCacheStats();
            _logger.LogInformation("Localization warmup completed. Loaded {BucketCount} buckets: {Buckets}", 
                stats.Count, 
                string.Join(", ", stats.Select(s => $"{s.Key}({s.Value} keys)")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Localization warmup failed.");
            throw; // fail fast so you know startup cache didn't load
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
