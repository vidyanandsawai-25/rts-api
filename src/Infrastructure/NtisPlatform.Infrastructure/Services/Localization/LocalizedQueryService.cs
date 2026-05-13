using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Infrastructure.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace NtisPlatform.Infrastructure.Services.Localization;

/// <summary>
/// Service for querying localized values from the multilingual table.
/// Used to support filtering/searching on localized fields.
/// </summary>
public sealed class LocalizedQueryService : ILocalizedQueryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly string _defaultLanguage;

    public LocalizedQueryService(IDbContextFactory<ApplicationDbContext> dbFactory)
        : this(dbFactory, Microsoft.Extensions.Options.Options.Create(new LocalizationOptions { DefaultLanguage = "en" }))
    {
    }

    public LocalizedQueryService(IDbContextFactory<ApplicationDbContext> dbFactory, IOptions<LocalizationOptions> localizationOptions)
    {
        _dbFactory = dbFactory;
        _defaultLanguage = NormalizeLanguage(localizationOptions.Value.DefaultLanguage);
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

        if (keys.Count == 0 && normalizedLanguage != _defaultLanguage)
        {
            if (_defaultLanguage == "mr")
            {
                keys = await baseQuery.Where(x => x.mr_IN != null && EF.Functions.Like(x.mr_IN, likePattern)).Select(x => x.Key).ToListAsync(cancellationToken);
            }
            else if (_defaultLanguage == "hi")
            {
                keys = await baseQuery.Where(x => x.hi_IN != null && EF.Functions.Like(x.hi_IN, likePattern)).Select(x => x.Key).ToListAsync(cancellationToken);
            }
            else
            {
                keys = await baseQuery.Where(x => x.en_US != null && EF.Functions.Like(x.en_US, likePattern)).Select(x => x.Key).ToListAsync(cancellationToken);
            }
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

        if (keys.Count == 0 && normalizedLanguage != _defaultLanguage)
        {
            string defaultCol = _defaultLanguage == "mr" ? "mr_IN" : (_defaultLanguage == "hi" ? "hi_IN" : "en_US");
            if (exactMatch)
            {
                if (defaultCol == "mr_IN")
                    keys = await baseQuery.Where(x => x.mr_IN != null && x.mr_IN == value).Select(x => x.Key).ToListAsync(cancellationToken);
                else if (defaultCol == "hi_IN")
                    keys = await baseQuery.Where(x => x.hi_IN != null && x.hi_IN == value).Select(x => x.Key).ToListAsync(cancellationToken);
                else
                    keys = await baseQuery.Where(x => x.en_US != null && x.en_US == value).Select(x => x.Key).ToListAsync(cancellationToken);
            }
            else
            {
                var likePattern = $"%{EscapeLikePattern(value)}%";
                if (defaultCol == "mr_IN")
                    keys = await baseQuery.Where(x => x.mr_IN != null && EF.Functions.Like(x.mr_IN, likePattern)).Select(x => x.Key).ToListAsync(cancellationToken);
                else if (defaultCol == "hi_IN")
                    keys = await baseQuery.Where(x => x.hi_IN != null && EF.Functions.Like(x.hi_IN, likePattern)).Select(x => x.Key).ToListAsync(cancellationToken);
                else
                    keys = await baseQuery.Where(x => x.en_US != null && EF.Functions.Like(x.en_US, likePattern)).Select(x => x.Key).ToListAsync(cancellationToken);
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

        IQueryable<Core.Entities.MultilingualResourceEntity> languageQuery;

        if (exactMatch)
        {
            languageQuery = normalizedLanguage switch
            {
                "mr" => baseQuery.Where(x => x.mr_IN != null && valueList.Contains(x.mr_IN)),
                "hi" => baseQuery.Where(x => x.hi_IN != null && valueList.Contains(x.hi_IN)),
                _ => baseQuery.Where(x => x.en_US != null && valueList.Contains(x.en_US))
            };
        }
        else
        {
            // Build dynamic OR expression for LIKE to avoid EF Core translation crash
            var columnName = normalizedLanguage switch
            {
                "mr" => "mr_IN",
                "hi" => "hi_IN",
                _ => "en_US"
            };
            languageQuery = ApplyLikeAny(baseQuery, columnName, valueList);
        }

        var rows = await languageQuery
            .Select(x => new { x.Key, Value = normalizedLanguage == "mr" ? x.mr_IN : (normalizedLanguage == "hi" ? x.hi_IN : x.en_US) })
            .ToListAsync(cancellationToken);

        // If no results in requested language, fallback to DefaultLanguage
        if (rows.Count == 0 && normalizedLanguage != _defaultLanguage)
        {
            string defaultCol = _defaultLanguage == "mr" ? "mr_IN" : (_defaultLanguage == "hi" ? "hi_IN" : "en_US");
            var fallbackQuery = exactMatch
                ? (defaultCol == "mr_IN" ? baseQuery.Where(x => x.mr_IN != null && valueList.Contains(x.mr_IN)) :
                   defaultCol == "hi_IN" ? baseQuery.Where(x => x.hi_IN != null && valueList.Contains(x.hi_IN)) :
                   baseQuery.Where(x => x.en_US != null && valueList.Contains(x.en_US)))
                : ApplyLikeAny(baseQuery, defaultCol, valueList);

            rows = await fallbackQuery
                .Select(x => new { x.Key, Value = defaultCol == "mr_IN" ? x.mr_IN : (defaultCol == "hi_IN" ? x.hi_IN : x.en_US) })
                .ToListAsync(cancellationToken);
        }

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        if (exactMatch)
        {
            foreach (var group in rows.GroupBy(r => r.Value!, StringComparer.OrdinalIgnoreCase))
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
                    .Where(r => r.Value != null && r.Value.Contains(inputValue, StringComparison.OrdinalIgnoreCase))
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

    private static readonly MethodInfo _likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

    private IQueryable<Core.Entities.MultilingualResourceEntity> ApplyLikeAny(
        IQueryable<Core.Entities.MultilingualResourceEntity> query,
        string columnName,
        List<string> values)
    {
        var parameter = Expression.Parameter(typeof(Core.Entities.MultilingualResourceEntity), "x");
        var property = Expression.Property(parameter, columnName);
        
        Expression? combined = null;
        var functionsProp = typeof(EF).GetProperty("Functions")!;
        var functions = Expression.Property(null, functionsProp);

        foreach (var v in values)
        {
            var pattern = Expression.Constant($"%{EscapeLikePattern(v)}%");
            var likeCall = Expression.Call(null, _likeMethod, functions, property, pattern);

            combined = combined == null ? likeCall : Expression.OrElse(combined, likeCall);
        }

        if (combined == null) return query;
        return query.Where(Expression.Lambda<Func<Core.Entities.MultilingualResourceEntity, bool>>(combined, parameter));
    }

    private string? GetLanguageValueWithFallback(Core.Entities.MultilingualResourceEntity entity, string language)
    {
        var normalized = NormalizeLanguage(language);

        var value = normalized switch
        {
            "mr" => entity.mr_IN,
            "hi" => entity.hi_IN,
            _ => entity.en_US
        };

        if (string.IsNullOrWhiteSpace(value) && normalized != _defaultLanguage)
        {
            value = _defaultLanguage switch
            {
                "mr" => entity.mr_IN,
                "hi" => entity.hi_IN,
                _ => entity.en_US
            };
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