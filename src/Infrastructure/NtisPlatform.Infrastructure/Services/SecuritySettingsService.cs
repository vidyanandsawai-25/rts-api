using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Dynamic security settings service (values from ConfigKeyMaster/ConfigValueMaster).
/// Caches settings for 15 minutes to reduce database load.
/// </summary>
public class SecuritySettingsService : ISecuritySettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecuritySettingsService> _logger;

    private const string SecurityCategoryCode = "SECURITY_AUTH";
    private const string CacheKey = "SecuritySettings:SECURITY_AUTH";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public SecuritySettingsService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<SecuritySettingsService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));

        var map = await GetCategoryMapAsync(cancellationToken);

        if (!map.TryGetValue(key, out var stringValue) || string.IsNullOrWhiteSpace(stringValue))
        {
            _logger.LogError(
                "Required security setting '{Key}' not found (or empty) in category '{CategoryCode}'. " +
                "This could disable security protections. Check database configuration.",
                key, SecurityCategoryCode);

            throw new InvalidOperationException(
                $"Required security setting '{key}' is missing or empty in category '{SecurityCategoryCode}'. " +
                "This misconfiguration could disable security protections. Please configure this setting in the database.");
        }

        try
        {
            return ConvertValue<T>(stringValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to convert required security setting '{Key}' with value '{Value}' to type {Type}. " +
                "This misconfiguration could disable security protections.",
                key, stringValue, typeof(T).Name);
            throw new InvalidOperationException(
                $"Required security setting '{key}' has invalid value '{stringValue}' for type {typeof(T).Name}. " +
                "This misconfiguration could disable security protections. Please fix the configuration in the database.", ex);
        }
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));

        var map = await GetCategoryMapAsync(cancellationToken);

        if (!map.TryGetValue(key, out var stringValue) || string.IsNullOrWhiteSpace(stringValue))
        {
            _logger.LogInformation(
                "Optional security setting '{Key}' not found (or empty) in category '{CategoryCode}'. Using provided default: {DefaultValue}",
                key, SecurityCategoryCode, defaultValue);

            return defaultValue;
        }

        _logger.LogDebug("Security setting '{Key}' retrieved: RawValue='{Value}', ConvertingTo={TargetType}", 
            key, stringValue, typeof(T).Name);

        try
        {
            // Always throw on conversion failure so catch block works properly
            return ConvertValue<T>(stringValue);
        }
        catch
        {
            // Conversion failed - return provided default for optional settings
            _logger.LogWarning(
                "Failed to convert optional security setting '{Key}' with value '{Value}' to type {Type}. Using provided default: {DefaultValue}",
                key, stringValue, typeof(T).Name, defaultValue);
            return defaultValue;
        }
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetAllAsync(CancellationToken cancellationToken = default)
        => await GetCategoryMapAsync(cancellationToken);

    public Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Security settings cache cleared for category '{CategoryCode}'.", SecurityCategoryCode);
        return Task.CompletedTask;
    }

    // ---------------- PRIVATE ----------------

    private async Task<Dictionary<string, string?>> GetCategoryMapAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, string?>? cached) && cached is not null)
            return cached;

        // Loads all keys for SECURITY_AUTH and resolves final value:
        // ConfigValueMaster.Value (global) > ConfigKeyMaster.DefaultValue
        var configValues = _context.ConfigValueMasters.AsNoTracking()
            .Where(cv => cv.IsActive
                      && cv.DepartmentId == null
                      && cv.ModuleId == null);

        var rows = await (
            from cc in _context.ConfigCategoryMasters.AsNoTracking()
            join ck in _context.ConfigKeyMasters.AsNoTracking()
                on cc.CategoryId equals ck.CategoryId
            where cc.CategoryCode == SecurityCategoryCode
                  && cc.IsActive
                  && ck.IsActive
            join cv in configValues
                on ck.ConfigKeyId equals cv.ConfigKeyId into cvJoin
            from cv in cvJoin.DefaultIfEmpty()
            select new
            {
                Key = ck.ConfigCode,
                Value = cv != null ? cv.Value : null,
                DefaultValue = ck.DefaultValue,
                ConfigKeyId = ck.ConfigKeyId,
                UpdatedDate = ck.UpdatedDate,
                CreatedDate = ck.CreatedDate
            }
        ).ToListAsync(cancellationToken);

        var dict = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Key)) // filter out null keys
            .GroupBy(x => x.Key!) // safety if duplicates exist
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    // Deterministic selection: take first entry by ConfigKeyId/CreatedDate to ensure consistency
                    var item = g.OrderBy(x => x.ConfigKeyId)
                                .ThenBy(x => x.CreatedDate ?? DateTime.MaxValue)
                                .First();
                    return (!string.IsNullOrWhiteSpace(item.Value) ? item.Value : item.DefaultValue) ?? string.Empty;
                },
                StringComparer.OrdinalIgnoreCase
            );

        _cache.Set(CacheKey, dict, CacheDuration);

        _logger.LogDebug("Loaded {Count} security settings for category '{CategoryCode}'.",
            dict.Count, SecurityCategoryCode);

        return dict;
    }

    private T ConvertValue<T>(string stringValue)
    {
        try
        {
            var targetType = typeof(T);

            if (targetType == typeof(string))
                return (T)(object)stringValue;

            if (targetType == typeof(int))
            {
                if (int.TryParse(stringValue, out var i))
                    return (T)(object)i;
                throw new FormatException($"Cannot convert '{stringValue}' to int.");
            }

            if (targetType == typeof(bool))
            {
                if (bool.TryParse(stringValue, out var b))
                    return (T)(object)b;

                if (stringValue.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return (T)(object)true;

                if (stringValue.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                    stringValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return (T)(object)false;

                throw new FormatException($"Cannot convert '{stringValue}' to bool.");
            }

            if (targetType == typeof(double))
            {
                if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return (T)(object)d;
                throw new FormatException($"Cannot convert '{stringValue}' to double.");
            }

            if (targetType == typeof(decimal))
            {
                if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                    return (T)(object)dec;
                throw new FormatException($"Cannot convert '{stringValue}' to decimal.");
            }

            if (targetType == typeof(long))
            {
                if (long.TryParse(stringValue, out var l))
                    return (T)(object)l;
                throw new FormatException($"Cannot convert '{stringValue}' to long.");
            }

            // Fallback: try Convert.ChangeType 
            return (T)Convert.ChangeType(stringValue, targetType);
        }
        catch (Exception ex) when (ex is not FormatException)
        {
            throw new InvalidOperationException(
                $"Failed to convert value '{stringValue}' to type {typeof(T).Name}.", ex);
        }
    }
}
