using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services.Localization;

/// <summary>
/// Service for querying localized values from the multilingual table.
/// Used to support filtering/searching on localized fields.
/// </summary>
public sealed class LocalizedQueryService : ILocalizedQueryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public LocalizedQueryService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string?> GetLocalizedValueAsync(
        string resource,  // ✅ Added resource parameter
        string key,
        string language,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.MultilingualResourceEntity
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.Resource == resource && x.Key == key, cancellationToken);

        if (entity == null)
            return null;

        return GetLanguageValueWithFallback(entity, language);
    }

    public async Task<Dictionary<string, string>> GetLocalizedValuesAsync(
        string resource,  // ✅ Added resource parameter
        IEnumerable<string> keys,
        string language,
        CancellationToken cancellationToken = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return new Dictionary<string, string>();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var entities = await db.MultilingualResourceEntity
            .AsNoTracking()
            .Where(x => x.IsActive && x.Resource == resource && keyList.Contains(x.Key))
            .ToListAsync(cancellationToken);

        return entities.ToDictionary(
            e => e.Key,
            e => GetLanguageValueWithFallback(e, language) ?? e.Key);
    }

    // SearchLocalizedKeysAsync already filters by resource ✅
    public async Task<IReadOnlyList<string>> SearchLocalizedKeysAsync(
        string resource,
        string searchTerm,
        string language,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return [];

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedLanguage = NormalizeLanguage(language);
        var likePattern = $"%{EscapeLikePattern(searchTerm)}%";

        var baseQuery = db.MultilingualResourceEntity
            .AsNoTracking()
            .Where(x => x.IsActive && x.Resource == resource);

        var languageQuery = normalizedLanguage switch
        {
            "mr" => baseQuery.Where(x => x.mr_IN != null && EF.Functions.Like(x.mr_IN, likePattern)),
            "hi" => baseQuery.Where(x => x.hi_IN != null && EF.Functions.Like(x.hi_IN, likePattern)),
            _ => baseQuery.Where(x => x.en_US != null && EF.Functions.Like(x.en_US, likePattern))
        };

        var keys = await languageQuery
            .Select(x => x.Key)
            .ToListAsync(cancellationToken);

        if (keys.Count == 0 && normalizedLanguage != "en")
        {
            keys = await baseQuery
                .Where(x => x.en_US != null && EF.Functions.Like(x.en_US, likePattern))
                .Select(x => x.Key)
                .ToListAsync(cancellationToken);
        }

        return keys;
    }

    // GetKeysByLocalizedValueAsync already filters by resource ✅
    public async Task<IReadOnlyList<string>> GetKeysByLocalizedValueAsync(
        string resource,
        string value,
        string language,
        bool exactMatch = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedLanguage = NormalizeLanguage(language);
        var baseQuery = db.MultilingualResourceEntity
            .AsNoTracking()
            .Where(x => x.IsActive && x.Resource == resource);

        IQueryable<Core.Entities.MultilingualResourceEntity> languageQuery;

        if (exactMatch)
        {
            languageQuery = normalizedLanguage switch
            {
                "mr" => baseQuery.Where(x => x.mr_IN != null && x.mr_IN == value),
                "hi" => baseQuery.Where(x => x.hi_IN != null && x.hi_IN == value),
                _ => baseQuery.Where(x => x.en_US != null && x.en_US == value)
            };
        }
        else
        {
            var likePattern = $"%{EscapeLikePattern(value)}%";
            languageQuery = normalizedLanguage switch
            {
                "mr" => baseQuery.Where(x => x.mr_IN != null && EF.Functions.Like(x.mr_IN, likePattern)),
                "hi" => baseQuery.Where(x => x.hi_IN != null && EF.Functions.Like(x.hi_IN, likePattern)),
                _ => baseQuery.Where(x => x.en_US != null && EF.Functions.Like(x.en_US, likePattern))
            };
        }

        var keys = await languageQuery
            .Select(x => x.Key)
            .ToListAsync(cancellationToken);

        if (keys.Count == 0 && normalizedLanguage != "en")
        {
            if (exactMatch)
            {
                keys = await baseQuery
                    .Where(x => x.en_US != null && x.en_US == value)
                    .Select(x => x.Key)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                var likePattern = $"%{EscapeLikePattern(value)}%";
                keys = await baseQuery
                    .Where(x => x.en_US != null && EF.Functions.Like(x.en_US, likePattern))
                    .Select(x => x.Key)
                    .ToListAsync(cancellationToken);
            }
        }

        return keys;
    }

    public async Task<Dictionary<string, IReadOnlyList<string>>> GetKeysByLocalizedValuesBatchAsync(
        string resource,
        IEnumerable<string> values,
        string language,
        bool exactMatch = true,
        CancellationToken cancellationToken = default)
    {
        var valueList = values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();
        if (valueList.Count == 0)
            return new Dictionary<string, IReadOnlyList<string>>();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedLanguage = NormalizeLanguage(language);
        var baseQuery = db.MultilingualResourceEntity
            .AsNoTracking()
            .Where(x => x.IsActive && x.Resource == resource);

        // Select key + the language column value so we can group by value afterwards
        var rows = normalizedLanguage switch
        {
            "mr" => exactMatch
                ? await baseQuery.Where(x => x.mr_IN != null && valueList.Contains(x.mr_IN))
                    .Select(x => new { x.Key, Value = x.mr_IN! }).ToListAsync(cancellationToken)
                : await baseQuery.Where(x => x.mr_IN != null && valueList.Any(v => EF.Functions.Like(x.mr_IN!, "%" + v + "%")))
                    .Select(x => new { x.Key, Value = x.mr_IN! }).ToListAsync(cancellationToken),
            "hi" => exactMatch
                ? await baseQuery.Where(x => x.hi_IN != null && valueList.Contains(x.hi_IN))
                    .Select(x => new { x.Key, Value = x.hi_IN! }).ToListAsync(cancellationToken)
                : await baseQuery.Where(x => x.hi_IN != null && valueList.Any(v => EF.Functions.Like(x.hi_IN!, "%" + v + "%")))
                    .Select(x => new { x.Key, Value = x.hi_IN! }).ToListAsync(cancellationToken),
            _ => exactMatch
                ? await baseQuery.Where(x => x.en_US != null && valueList.Contains(x.en_US))
                    .Select(x => new { x.Key, Value = x.en_US! }).ToListAsync(cancellationToken)
                : await baseQuery.Where(x => x.en_US != null && valueList.Any(v => EF.Functions.Like(x.en_US!, "%" + v + "%")))
                    .Select(x => new { x.Key, Value = x.en_US! }).ToListAsync(cancellationToken)
        };

        // If no results in requested language, fallback to English
        if (rows.Count == 0 && normalizedLanguage != "en")
        {
            rows = exactMatch
                ? await baseQuery.Where(x => x.en_US != null && valueList.Contains(x.en_US))
                    .Select(x => new { x.Key, Value = x.en_US! }).ToListAsync(cancellationToken)
                : await baseQuery.Where(x => x.en_US != null && valueList.Any(v => EF.Functions.Like(x.en_US!, "%" + v + "%")))
                    .Select(x => new { x.Key, Value = x.en_US! }).ToListAsync(cancellationToken);
        }

        // Group results: for exact match, group by the matched value directly
        // For LIKE match, we need to associate each result with matching input values
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        if (exactMatch)
        {
            foreach (var group in rows.GroupBy(r => r.Value, StringComparer.OrdinalIgnoreCase))
            {
                result[group.Key] = group.Select(r => r.Key).ToList();
            }
        }
        else
        {
            // For LIKE matches, associate each input value with keys whose translation contains it
            foreach (var inputValue in valueList)
            {
                var matchingKeys = rows
                    .Where(r => r.Value.Contains(inputValue, StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.Key)
                    .ToList();

                if (matchingKeys.Count > 0)
                {
                    result[inputValue] = matchingKeys;
                }
            }
        }

        return result;
    }

    private static string? GetLanguageValueWithFallback(Core.Entities.MultilingualResourceEntity entity, string language)
    {
        var normalized = NormalizeLanguage(language);

        var value = normalized switch
        {
            "mr" => entity.mr_IN,
            "hi" => entity.hi_IN,
            _ => entity.en_US
        };

        if (string.IsNullOrWhiteSpace(value) && normalized != "en")
        {
            value = entity.en_US;
        }

        return value;
    }

    private static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en";

        var lower = language.ToLowerInvariant();
        if (lower.StartsWith("mr"))
            return "mr";
        if (lower.StartsWith("hi"))
            return "hi";
        return "en";
    }

    private static string EscapeLikePattern(string input)
    {
        return input
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }
}