using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Core;
using NtisPlatform.Core.Constants;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace NtisPlatform.Application.Services;

public class LocalizationProcessor
{
    private readonly ILocalization _localizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _defaultLanguage;

    /// <summary>
    /// Per-type cache: avoids repeated reflection to discover [IsLocalizable] properties.
    /// Populated once per DTO type on first access, reused for all subsequent calls.
    /// 
    /// NOTE: No size limit is needed because:
    /// 1. DTO types are fixed at compile time (not dynamically generated)
    /// 2. Cache size = number of unique DTO types with [IsLocalizable] properties
    /// 3. Typical apps have <100 DTO types, so memory impact is negligible (~KB)
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<LocalizablePropertyAccessor>> _accessorCache = new();

    public LocalizationProcessor(ILocalization localizationService, IHttpContextAccessor httpContextAccessor)
        : this(localizationService, httpContextAccessor, Microsoft.Extensions.Options.Options.Create(new LocalizationOptions { DefaultLanguage = "en" }))
    {
    }

    public LocalizationProcessor(
        ILocalization localizationService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<LocalizationOptions> localizationOptions)
    {
        _localizationService = localizationService;
        _httpContextAccessor = httpContextAccessor;
        _defaultLanguage = localizationOptions.Value.DefaultLanguage;
    }

    private string GetLanguage()
        => _httpContextAccessor.HttpContext?.Items[HttpContextKeys.CurrentLanguage] as string ?? "en";

    // =======================
    // SAVE FLOW (Create/Update)
    // =======================
    /// <summary>
    /// Persists localizable property values for the given DTO.
    ///
    /// Key strategy:
    ///   - If <paramref name="existingKeys"/> contains an entry for a property, that key is reused (UPDATE).
    ///   - Otherwise a fresh key of the form {Resource}_{GUID:N}_{PropertyName} is generated (CREATE).
    ///
    /// This decouples key generation from the entity's primary key, removing the need to insert the
    /// entity first to obtain an auto-generated ID before persisting localization rows.
    /// </summary>
    public virtual async Task ProcessSaveAsync<TDto>(TDto dto, IReadOnlyDictionary<string, string>? existingKeys = null)
    {
        if (dto == null)
            return;

        // Always write to the configured default language column (LocalizationOptions.DefaultLanguage),
        // regardless of the user's current language. Read operations handle language selection with
        // fallback to that configured default language.
        var language = _defaultLanguage;
        var accessors = GetOrCreateAccessors(typeof(TDto));

        if (accessors.Count == 0)
            return;

        // Single loop: collect entries to save
        var (entries, propsToUpdate) = CollectSaveEntries(dto, accessors, existingKeys, language);

        if (entries.Count == 0)
            return;

        // Single batch DB call
        var keyMap = await _localizationService.SaveBatchAsync(entries);

        // Single loop: apply key replacements back to DTO
        ApplyKeyReplacements(dto, propsToUpdate, keyMap);
    }

    // =======================
    // READ FLOW
    // =======================
    public virtual async Task ProcessGetAsync<TDto>(IEnumerable<TDto> dtos)
    {
        if (dtos == null)
            return;

        var list = dtos.ToList();
        if (list.Count == 0)
            return;

        var language = GetLanguage();
        var accessors = GetOrCreateAccessors(typeof(TDto));
        if (accessors.Count == 0)
            return;

        // Step 1: Collect unique keys per resource and batch-fetch translations
        var translationMap = await CollectAndFetchTranslationsAsync(list, accessors, language);
        if (translationMap.Count == 0)
            return;

        // Step 2: Apply translations to DTOs
        ApplyTranslationsToDtos(list, accessors, translationMap);
    }

    // =======================
    // PIPELINE HELPERS (each has max 1 loop level)
    // =======================

    /// <summary>
    /// Collects localization entries from a single DTO for saving. Single loop over properties.
    /// Reuses a key from <paramref name="existingKeys"/> when present (UPDATE);
    /// otherwise mints a new {Resource}_{GUID:N}_{PropertyName} key (CREATE).
    /// </summary>
    private static (List<LocalizationEntry> Entries, List<LocalizablePropertyAccessor> Props) CollectSaveEntries<TDto>(
        TDto dto,
        IReadOnlyList<LocalizablePropertyAccessor> accessors,
        IReadOnlyDictionary<string, string>? existingKeys,
        string language)
    {
        var entries = new List<LocalizationEntry>(accessors.Count);
        var propsToUpdate = new List<LocalizablePropertyAccessor>(accessors.Count);

        for (int i = 0; i < accessors.Count; i++)
        {
            var accessor = accessors[i];
            var value = accessor.GetValue(dto!) as string;

            if (string.IsNullOrWhiteSpace(value) || IsLocalizationKey(value, accessor.Resource))
                continue;

            string key;
            if (existingKeys != null
                && existingKeys.TryGetValue(accessor.PropertyName, out var existing)
                && !string.IsNullOrWhiteSpace(existing)
                && IsLocalizationKey(existing, accessor.Resource))
            {
                key = existing;
            }
            else
            {
                key = $"{accessor.Resource}_{Guid.NewGuid():N}_{accessor.PropertyName}";
            }

            entries.Add(new LocalizationEntry
            {
                Resource = accessor.Resource,
                Key = key,
                PropertyName = accessor.PropertyName,
                Value = value,
                Language = language
            });

            propsToUpdate.Add(accessor);
        }

        return (entries, propsToUpdate);
    }

    /// <summary>
    /// Applies generated keys back to DTO properties after save. Single loop.
    /// </summary>
    private static void ApplyKeyReplacements<TDto>(
        TDto dto,
        List<LocalizablePropertyAccessor> propsToUpdate,
        Dictionary<string, string> keyMap)
    {
        for (int i = 0; i < propsToUpdate.Count; i++)
        {
            if (keyMap.TryGetValue(propsToUpdate[i].PropertyName, out var key))
                propsToUpdate[i].SetValue(dto!, key);
        }
    }

    /// <summary>
    /// Collects unique keys per resource and batch-fetches translations.
    /// Single-level loop over distinct resources.
    /// </summary>
    private async Task<Dictionary<string, string>> CollectAndFetchTranslationsAsync<TDto>(
        List<TDto> dtos,
        IReadOnlyList<LocalizablePropertyAccessor> accessors,
        string language)
    {
        var translationMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var resources = GetDistinctResources(accessors);

        foreach (var resource in resources)
        {
            var uniqueKeys = CollectKeysForResource(dtos, accessors, resource);
            if (uniqueKeys.Count == 0)
                continue;

            var localizedValues = await _localizationService.GetAsync(resource, uniqueKeys, language);

            foreach (var kvp in localizedValues)
                translationMap[kvp.Key] = kvp.Value;
        }

        return translationMap;
    }

    /// <summary>
    /// Extracts distinct resource names. LINQ expression — no explicit loop.
    /// </summary>
    private static List<string> GetDistinctResources(IReadOnlyList<LocalizablePropertyAccessor> accessors)
    {
        return accessors
            .Select(a => a.Resource)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Collects unique localization keys for a specific resource from all DTOs.
    /// Single-level loop over DTOs, delegates per-DTO extraction to helper.
    /// </summary>
    private static List<string> CollectKeysForResource<TDto>(
        List<TDto> dtos,
        IReadOnlyList<LocalizablePropertyAccessor> accessors,
        string resource)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < dtos.Count; i++)
            ExtractKeysFromDto(dtos[i]!, accessors, resource, keys);

        return keys.ToList();
    }

    /// <summary>
    /// Extracts localization keys from a single DTO. Single loop over properties.
    /// </summary>
    private static void ExtractKeysFromDto(
        object dto,
        IReadOnlyList<LocalizablePropertyAccessor> accessors,
        string resource,
        HashSet<string> keys)
    {
        for (int i = 0; i < accessors.Count; i++)
        {
            if (!string.Equals(accessors[i].Resource, resource, StringComparison.OrdinalIgnoreCase))
                continue;

            var val = accessors[i].GetValue(dto) as string;
            if (!string.IsNullOrWhiteSpace(val) && IsLocalizationKey(val, resource))
                keys.Add(val);
        }
    }

    /// <summary>
    /// Applies translations to all DTOs. Single loop, delegates to per-DTO helper.
    /// </summary>
    private static void ApplyTranslationsToDtos<TDto>(
        List<TDto> dtos,
        IReadOnlyList<LocalizablePropertyAccessor> accessors,
        Dictionary<string, string> translationMap)
    {
        for (int i = 0; i < dtos.Count; i++)
            LocalizeDto(dtos[i]!, accessors, translationMap);
    }

    /// <summary>
    /// Applies translations to a single DTO. Single loop over properties.
    /// Uses compiled delegates — no reflection overhead.
    /// </summary>
    private static void LocalizeDto(
        object dto,
        IReadOnlyList<LocalizablePropertyAccessor> accessors,
        Dictionary<string, string> translationMap)
    {
        for (int i = 0; i < accessors.Count; i++)
        {
            var key = accessors[i].GetValue(dto) as string;
            if (key != null && translationMap.TryGetValue(key, out var localizedValue))
                accessors[i].SetValue(dto, localizedValue);
        }
    }



    // =======================
    // DELETE FLOW
    // =======================
    /// <summary>
    /// Soft delete: Deactivates localization entries (sets IsActive = false)
    /// </summary>
    public virtual async Task ProcessDeactivateAsync(string resource, IEnumerable<string> keys)
    {
        if (string.IsNullOrWhiteSpace(resource) || keys == null || !keys.Any())
            return;

        await _localizationService.DeactivateByKeysAsync(resource, keys);
    }

    // =======================
    // COMPILED PROPERTY ACCESSOR (replaces slow PropertyInfo.GetValue/SetValue)
    // =======================

    /// <summary>
    /// Holds compiled getter/setter delegates for a single [IsLocalizable] property.
    /// Created once per type and cached forever. 
    /// GetValue/SetValue run at near-native speed (~2ns vs ~100ns for reflection).
    /// </summary>
    internal sealed class LocalizablePropertyAccessor
    {
        public string PropertyName { get; }
        public string Resource { get; }

        private readonly Func<object, object?> _getter;
        private readonly Action<object, object?> _setter;

        public LocalizablePropertyAccessor(PropertyInfo prop, IsLocalizableAttribute attr)
        {
            PropertyName = prop.Name;
            Resource = attr.Resource;

            // Compile getter: (object obj) => (object?)((TOwner)obj).Property
            var objParam = Expression.Parameter(typeof(object), "obj");
            var castObj = Expression.Convert(objParam, prop.DeclaringType!);
            var propAccess = Expression.Property(castObj, prop);
            var boxed = Expression.Convert(propAccess, typeof(object));
            _getter = Expression.Lambda<Func<object, object?>>(boxed, objParam).Compile();

            // Compile setter: (object obj, object? val) => ((TOwner)obj).Property = (string)val
            var valParam = Expression.Parameter(typeof(object), "val");
            var castVal = Expression.Convert(valParam, prop.PropertyType);
            var assign = Expression.Assign(Expression.Property(Expression.Convert(objParam, prop.DeclaringType!), prop), castVal);
            _setter = Expression.Lambda<Action<object, object?>>(assign, objParam, valParam).Compile();
        }

        public object? GetValue(object dto) => _getter(dto);
        public void SetValue(object dto, object? value) => _setter(dto, value);
    }

    // =======================
    // ACCESSOR CACHE
    // =======================

    /// <summary>
    /// Gets or creates compiled property accessors for the given DTO type.
    /// Result is cached per type — reflection happens only once per DTO type in the app lifetime.
    /// </summary>
    private static IReadOnlyList<LocalizablePropertyAccessor> GetOrCreateAccessors(Type type)
    {
        return _accessorCache.GetOrAdd(type, static t =>
        {
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => (Prop: p, Attr: p.GetCustomAttribute<IsLocalizableAttribute>()))
                .Where(x => x.Attr != null && x.Prop.PropertyType.IsAssignableTo(typeof(string)))
                .Select(x => new LocalizablePropertyAccessor(x.Prop, x.Attr!))
                .ToList();
        });
    }

    // =======================
    // STATIC HELPERS
    // =======================

    /// <summary>
    /// Gets the resource from a DTO type by inspecting [IsLocalizable] attributes.
    /// </summary>
    public static string GetResource<TDto>()
    {
        var accessors = _accessorCache.GetOrAdd(typeof(TDto), static t =>
        {
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => (Prop: p, Attr: p.GetCustomAttribute<IsLocalizableAttribute>()))
                .Where(x => x.Attr != null && x.Prop.PropertyType.IsAssignableTo(typeof(string)))
                .Select(x => new LocalizablePropertyAccessor(x.Prop, x.Attr!))
                .ToList();
        });

        var resources = accessors
            .Select(a => a.Resource)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (resources.Count == 1)
            return resources[0];

        if (resources.Count == 0)
            throw new InvalidOperationException(
                $"No [IsLocalizable] attribute found on any property of {typeof(TDto).Name}");

        throw new InvalidOperationException(
            $"Multiple localization resources found on {typeof(TDto).Name}: {string.Join(", ", resources)}");
    }

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
}
