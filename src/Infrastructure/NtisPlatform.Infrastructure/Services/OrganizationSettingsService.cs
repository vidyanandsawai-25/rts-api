using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using System.Text.Json;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for managing runtime-configurable organization settings
/// Uses key-value storage for flexibility
/// </summary>
public class OrganizationSettingsService : IOrganizationSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "OrgSetting:";
    private const string AllSettingsCacheKey = "AllOrgSettings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public OrganizationSettingsService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Dictionary<string, string?>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(AllSettingsCacheKey, out Dictionary<string, string?>? cached) && cached != null)
        {
            return cached;
        }

        var settings = await _context.OrganizationSettings
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        _cache.Set(AllSettingsCacheKey, settings, CacheDuration);
        return settings;
    }

    public async Task<Dictionary<string, string?>> GetSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.OrganizationSettings
            .Where(s => s.Category == category)
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached;
        }

        var setting = await _context.OrganizationSettings
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting != null)
        {
            _cache.Set(cacheKey, setting, CacheDuration);
        }

        return setting;
    }

    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default)
    {
        var value = await GetSettingAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        try
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // Handle specific types
            if (underlyingType == typeof(bool))
            {
                return (T)(object)bool.Parse(value);
            }
            if (underlyingType == typeof(int))
            {
                return (T)(object)int.Parse(value);
            }
            if (underlyingType == typeof(long))
            {
                return (T)(object)long.Parse(value);
            }
            if (underlyingType == typeof(double))
            {
                return (T)(object)double.Parse(value);
            }
            if (underlyingType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(value);
            }
            if (underlyingType == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(value);
            }
            if (underlyingType == typeof(string))
            {
                return (T)(object)value;
            }

            // Try JSON deserialization for complex types
            return JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetSettingAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        var setting = await _context.OrganizationSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting == null)
        {
            throw new InvalidOperationException($"Setting key '{key}' not found. Please add it to the database first.");
        }

        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate caches
        _cache.Remove($"{CacheKeyPrefix}{key}");
        _cache.Remove(AllSettingsCacheKey);
    }

    public async Task UpdateSettingsAsync(Dictionary<string, string?> settings, CancellationToken cancellationToken = default)
    {
        var keys = settings.Keys.ToList();
        var existingSettings = await _context.OrganizationSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(cancellationToken);

        foreach (var kvp in settings)
        {
            var setting = existingSettings.FirstOrDefault(s => s.Key == kvp.Key);
            if (setting != null)
            {
                setting.Value = kvp.Value;
                setting.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate all caches
        foreach (var key in keys)
        {
            _cache.Remove($"{CacheKeyPrefix}{key}");
        }
        _cache.Remove(AllSettingsCacheKey);
    }

    public async Task<OrganizationSetting?> GetSettingEntityAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.OrganizationSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }
}
