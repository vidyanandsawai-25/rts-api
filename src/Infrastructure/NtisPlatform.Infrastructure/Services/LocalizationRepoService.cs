using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

public class LocalizationRepoService : ILocalization
{
    private readonly ApplicationDbContext _db;
    private readonly ILocalizationService _localizationService;

    public LocalizationRepoService(ApplicationDbContext db, ILocalizationService localizationService)
    {
        _db = db;
        _localizationService = localizationService;
    }

    public async Task<string> SaveAsync(LocalizationEntry entry)
    {
        var key = entry.Key;

        var entity = await _db.MultilingualResourceEntity
            .FirstOrDefaultAsync(x => x.Resource == entry.Resource && x.Key == key);

        if (entity == null)
        {
            // Create new entry
            entity = new MultilingualResourceEntity
            {
                Resource = entry.Resource,
                Key = key,
                CreatedDate = DateTime.Now,
                IsActive = true,
                IsGenerated = true
            };
            SetLanguageValue(entity, entry.Language, entry.Value);
            _db.MultilingualResourceEntity.Add(entity);
        }
        else
        {
            // Update existing entry
            SetLanguageValue(entity, entry.Language, entry.Value);
            entity.UpdatedDate = DateTime.Now;
        }

        await _db.SaveChangesAsync();

        // ✅ Update cache only if NOT inside a transaction
        // If inside transaction, invalidate instead to prevent cache/DB divergence on rollback
        UpdateCacheAfterSave(entry.Resource, entry.Language, key, entry.Value);

        return key;
    }

    public async Task<Dictionary<string, string>> SaveBatchAsync(IEnumerable<LocalizationEntry> entries)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0)
            return new Dictionary<string, string>();

        var result = new Dictionary<string, string>();

        // Get all keys we need to check
        var keys = entryList.Select(e => e.Key).ToList();
        var resources = entryList.Select(e => e.Resource).Distinct().ToList();

        // Fetch existing entries in one query
        var existingEntities = await _db.MultilingualResourceEntity
            .Where(x => resources.Contains(x.Resource) && keys.Contains(x.Key))
            .ToDictionaryAsync(x => (x.Resource, x.Key), x => x);

        var toAdd = new List<MultilingualResourceEntity>();
        foreach (var entry in entryList)
        {
            if (existingEntities.TryGetValue((entry.Resource, entry.Key), out var existing))
            {
                // Update existing
                SetLanguageValue(existing, entry.Language, entry.Value);
                existing.UpdatedDate = DateTime.Now;
            }
            else
            {
                // Create new
                var newEntity = new MultilingualResourceEntity
                {
                    Resource = entry.Resource,
                    Key = entry.Key,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    IsGenerated = true
                };
                SetLanguageValue(newEntity, entry.Language, entry.Value);
                toAdd.Add(newEntity);
            }

            result[entry.PropertyName] = entry.Key;
        }

        // Batch add new entries
        if (toAdd.Count > 0)
        {
            _db.MultilingualResourceEntity.AddRange(toAdd);
        }

        // Single SaveChanges for all operations
        await _db.SaveChangesAsync();

        // ✅ Update cache only if NOT inside a transaction
        // If inside transaction, invalidate affected resources to prevent cache/DB divergence on rollback
        UpdateCacheAfterBatchSave(entryList);

        return result;
    }

    public async Task<Dictionary<string, string>> GetAsync(string resource, IEnumerable<string> keys, string language)
    {
        var cached = _localizationService.GetTranslationsExact(resource, language, keys);
        var missing = keys.Where(k => !cached.ContainsKey(k)).ToList();

        if (missing.Count == 0)
            return cached;

        var rows = await _db.MultilingualResourceEntity
            .Where(x => x.Resource == resource && missing.Contains(x.Key))
            .ToListAsync();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
                continue;

            // Get value with fallback logic
            var value = GetLanguageValueWithFallback(row, language);
            cached[row.Key] = value;

            _localizationService.SetTranslation(resource, language, row.Key, value);
        }

        // Keys not in DB at all - return key itself
        foreach (var key in missing.Where(k => !cached.ContainsKey(k)))
        {
            cached[key] = key;
            // ✅ Cache the "not found" result to prevent repeated DB queries
            _localizationService.SetTranslation(resource, language, key, key);
        }

        return cached;
    }

    /// <summary>
    /// Gets value for language with fallback: requested → en → mr → hi 
    /// Does NOT fall back to key - returns empty string if no translations exist
    /// </summary>
    private static string GetLanguageValueWithFallback(MultilingualResourceEntity entity, string language)
    {
        ReadOnlySpan<char> span = default;
        if (!string.IsNullOrWhiteSpace(language))
        {
            span = language.AsSpan().Trim();
            var dash = span.IndexOf('-');
            if (dash < 0) dash = span.IndexOf('_');
            if (dash > 0) span = span[..dash];
        }

        string? value = null;
        if (span.Equals("hi", StringComparison.OrdinalIgnoreCase)) value = entity.hi_IN;
        else if (span.Equals("mr", StringComparison.OrdinalIgnoreCase)) value = entity.mr_IN;
        else value = entity.en_US;

        if (!string.IsNullOrWhiteSpace(value))
            return value;

        // Fallback to any available language (handles both null and empty strings)
        if (!string.IsNullOrWhiteSpace(entity.en_US)) return entity.en_US;
        if (!string.IsNullOrWhiteSpace(entity.hi_IN)) return entity.hi_IN;
        if (!string.IsNullOrWhiteSpace(entity.mr_IN)) return entity.mr_IN;

        return string.Empty;
    }

    // =======================
    // CACHE SAFETY HELPERS
    // =======================

    /// <summary>
    /// Updates cache after save operation.
    /// If inside a transaction, invalidates the resource bucket instead of updating to prevent cache/DB divergence on rollback.
    /// </summary>
    private void UpdateCacheAfterSave(string resource, string language, string key, string value)
    {
        if (IsInsideTransaction())
        {
            // ⚠️ Inside transaction - invalidate entire resource bucket
            // This is safe: next read will reload fresh data from DB after transaction completes
            _localizationService.Invalidate(resource);
        }
        else
        {
            // ✅ No transaction - safe to update cache immediately
            _localizationService.SetTranslation(resource, language, key, value);
        }
    }

    /// <summary>
    /// Updates cache after batch save operation.
    /// If inside a transaction, invalidates affected resource buckets instead of updating.
    /// </summary>
    private void UpdateCacheAfterBatchSave(IEnumerable<LocalizationEntry> entries)
    {
        if (IsInsideTransaction())
        {
            // ⚠️ Inside transaction - invalidate all affected resources
            var affectedResources = entries.Select(e => e.Resource).Distinct();
            foreach (var resource in affectedResources)
            {
                _localizationService.Invalidate(resource);
            }
        }
        else
        {
            // ✅ No transaction - safe to update cache immediately
            foreach (var entry in entries)
            {
                _localizationService.SetTranslation(entry.Resource, entry.Language, entry.Key, entry.Value);
            }
        }
    }

    /// <summary>
    /// Checks if the current DbContext is inside an active transaction.
    /// </summary>
    private bool IsInsideTransaction()
    {
        // EF Core tracks the current transaction via Database.CurrentTransaction
        return _db.Database.CurrentTransaction != null;
    }

    // =======================
    // LANGUAGE HELPERS
    // =======================

    private static void SetLanguageValue(MultilingualResourceEntity entity, string language, string value)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            entity.en_US = value;
            return;
        }

        var span = language.AsSpan().Trim();
        var dash = span.IndexOf('-');
        if (dash < 0) dash = span.IndexOf('_');
        if (dash > 0) span = span[..dash];

        if (span.Equals("hi", StringComparison.OrdinalIgnoreCase))
            entity.hi_IN = value;
        else if (span.Equals("mr", StringComparison.OrdinalIgnoreCase))
            entity.mr_IN = value;
        else
            entity.en_US = value;
    }
}