using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.Master.MultilingualDetail;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing multilingual translations with optional auto-translation support.
/// </summary>
public class MultilingualTranslationService : BaseCommonCrudService<MultilingualResourceEntity, MultilingualTranslationDtos, CreateMultilingualTranslationDtos, UpdateMultilingualTranslationDtos, MultilingualTranslationQueryParameters, int>, IMultilingualTranslation
{
    private readonly TranslationServiceOptions _translateOptions;
    private readonly ITranslationService _translationService;
    private readonly ILogger<MultilingualTranslationService> _logger;
    private readonly string _defaultLanguage;

    public MultilingualTranslationService(
        IRepository<MultilingualResourceEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<TranslationServiceOptions> googleTranslateOptions,
        ITranslationService translationService,
        ILogger<MultilingualTranslationService> logger,
        IOptions<LocalizationOptions> localizationOptions)
        : base(repository, unitOfWork, mapper)
    {
        _translateOptions = googleTranslateOptions.Value;
        _translationService = translationService;
        _logger = logger;
        _defaultLanguage = localizationOptions.Value.DefaultLanguage;
    }

    /// <summary>
    /// Gets all translations with optional filtering for empty translations and auto-translation.
    /// </summary>
    public override async Task<PagedResult<MultilingualTranslationDtos>> GetAllAsync(
    MultilingualTranslationQueryParameters queryParameters,
    CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        // Apply filters
        query = query.ApplyFilters(queryParameters);

        // Apply search
        query = query.ApplySearch(queryParameters);

        // Apply dynamic filters for empty translations (ALL specified languages must be empty)
        if (queryParameters.FilterEmptyLanguages != null && queryParameters.FilterEmptyLanguages.Count > 0)
        {
            foreach (var lang in queryParameters.FilterEmptyLanguages)
            {
                var propertyName = ResolveLanguageColumnName(lang);
                if (propertyName == null) continue;

                // Dynamically build: x => x.{propertyName} == null || x.{propertyName} == ""
                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(MultilingualResourceEntity), "x");
                var property = System.Linq.Expressions.Expression.Property(parameter, propertyName);
                var nullCheck = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null, typeof(string)));
                var emptyCheck = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(""));
                var orExpr = System.Linq.Expressions.Expression.OrElse(nullCheck, emptyCheck);
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<MultilingualResourceEntity, bool>>(orExpr, parameter);

                query = query.Where(lambda);
            }
        }

        // Apply sorting
        query = query.ApplySort(queryParameters);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize)
            .ProjectTo<MultilingualTranslationDtos>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        bool shouldTranslate = _translateOptions.IsActive && queryParameters.IsAutoTranslate;

        if (shouldTranslate && items.Count > 0)
        {
            await ApplyTranslationsAsync(items, cancellationToken);
        }

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? (totalCount > 0 ? totalCount : 1) : queryParameters.PageSize;
        return new PagedResult<MultilingualTranslationDtos>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IEnumerable<string>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetQueryable()
            .Where(x => !string.IsNullOrEmpty(x.Resource))
            .Select(x => x.Resource)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Dynamically applies translations to items with missing translations in any target language.
    /// Uses reflection to discover language columns and the configured default language as source.
    /// </summary>
    private async Task ApplyTranslationsAsync(
        List<MultilingualTranslationDtos> items,
        CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the source language column (e.g., "en" -> "en_US")
            var sourceColumn = ResolveLanguageColumnName(_defaultLanguage);
            if (sourceColumn == null) return;

            var sourceProperty = typeof(MultilingualTranslationDtos).GetProperty(sourceColumn);
            if (sourceProperty == null) return;

            // Discover all target language columns (exclude the source language)
            var targetColumns = _languageColumnMap.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(col => !col.Equals(sourceColumn, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (targetColumns.Count == 0) return;

            // For each target language, find items needing translation and fire parallel API calls
            var translationTasks = new List<(string TargetColumn, string TargetLangCode, Task<Dictionary<string, string>> Task, List<MultilingualTranslationDtos> ItemsNeeding)>();

            foreach (var targetColumn in targetColumns)
            {
                var targetProperty = typeof(MultilingualTranslationDtos).GetProperty(targetColumn);
                if (targetProperty == null) continue;

                // Find items where source has a value but target is empty
                var itemsNeeding = items
                    .Where(item =>
                    {
                        var sourceVal = sourceProperty.GetValue(item) as string;
                        var targetVal = targetProperty.GetValue(item) as string;
                        return !string.IsNullOrWhiteSpace(sourceVal) && string.IsNullOrWhiteSpace(targetVal);
                    })
                    .ToList();

                if (itemsNeeding.Count == 0) continue;

                var sourceTexts = itemsNeeding
                    .Select(item => sourceProperty.GetValue(item) as string ?? "")
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .ToList();

                if (sourceTexts.Count == 0) continue;

                // Extract short language codes from locale/column identifiers (e.g., "hi_IN" -> "hi")
                var targetLangCode = targetColumn.Split('_', '-')[0];
                var sourceLangCode = _defaultLanguage.Split('_', '-')[0];

                var task = _translationService.TranslateBatchAsync(
                    sourceTexts, sourceLangCode, targetLangCode, cancellationToken);

                translationTasks.Add((targetColumn, targetLangCode, task, itemsNeeding));
            }

            if (translationTasks.Count == 0) return;

            // Run all translation API calls in parallel
            await Task.WhenAll(translationTasks.Select(t => t.Task));

            // Apply translations back to items using reflection
            foreach (var (targetColumn, _, task, itemsNeeding) in translationTasks)
            {
                var translations = await task;
                if (translations.Count == 0) continue;

                var targetProperty = typeof(MultilingualTranslationDtos).GetProperty(targetColumn)!;

                foreach (var item in itemsNeeding)
                {
                    var sourceText = sourceProperty.GetValue(item) as string;
                    if (sourceText != null && translations.TryGetValue(sourceText, out var translated))
                    {
                        targetProperty.SetValue(item, translated);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed for {Count} items", items.Count);
        }

    }

    /// <summary>
    /// Cached lookup of language short codes to entity property names.
    /// Built from reflection on MultilingualResourceEntity's string properties that follow
    /// the locale naming convention (e.g., en_US, hi_IN, mr_IN).
    /// Adding a new column to the entity automatically makes it discoverable here.
    /// </summary>
    private static readonly Dictionary<string, string> _languageColumnMap = BuildLanguageColumnMap();

    private static Dictionary<string, string> BuildLanguageColumnMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Discover all string properties on the entity that match locale pattern (xx_YY)
        var properties = typeof(MultilingualResourceEntity)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.Name.Contains('_') && p.Name.Length == 5);

        foreach (var prop in properties)
        {
            // Map full column name: "hi_IN" -> "hi_IN"
            map[prop.Name] = prop.Name;

            // Map short code: "hi" -> "hi_IN"
            var shortCode = prop.Name[..2];
            map.TryAdd(shortCode, prop.Name);

            // Map dash variant: "hi-IN" -> "hi_IN"
            var dashVariant = prop.Name.Replace('_', '-');
            map.TryAdd(dashVariant, prop.Name);
        }

        return map;
    }

    /// <summary>
    /// Resolves a language code (e.g., "hi", "hi_IN", "hi-IN") to the corresponding
    /// entity property name. Returns null if unrecognized.
    /// </summary>
    private static string? ResolveLanguageColumnName(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return null;

        return _languageColumnMap.TryGetValue(languageCode.Trim(), out var columnName)
            ? columnName
            : null;
    }

}