using Microsoft.Extensions.Caching.Memory;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Resources
{
    /// <summary>
    /// Loads translations from DB (via IMultilingualDetailsService) and caches them in IMemoryCache.  
    /// Cached value: Dictionary&lt;string Key, string Value&gt; for a given (resource, culture).  
    /// This is used by the DB-based IStringLocalizer to translate validation keys like:   "FloorID_Required" -> "मंज़िल आवश्यक है" (hi)
    /// </summary>
    public sealed class MultilingualResourceProvider : IMultilingualResourceProvider
    {
        private readonly IMultilingualDetailsService _service;
        private readonly IMemoryCache _cache;

        public MultilingualResourceProvider(IMultilingualDetailsService service, IMemoryCache cache)
        {
            _service = service;
            _cache = cache;
        }

        public Task<Dictionary<string, string>> GetAsync(string resource, string culture, CancellationToken ct = default)
        {
            // Normalize culture: if caller passed hi-IN, keep only "hi" (matches your DB cultures).
            culture = NormalizeCulture(culture);

            // Cache key per resource & culture
            var cacheKey = $"loc::{resource}::{culture}";

            return _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                // Tune cache lifetime as needed (10–30 minutes is common).
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                // IMPORTANT: This must return ALL rows for that resource & culture (non-paged).
                var rows = await _service.GetAllForLocalizationAsync(resource, culture, ct);

                // Build dictionary: Key -> Value, excluding rows with null or empty keys or values
                return rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            })!;
        }

        public void Invalidate(string resource, string culture)
        {
            culture = NormalizeCulture(culture);
            _cache.Remove($"loc::{resource}::{culture}");
        }

        private static string NormalizeCulture(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
                return "en";

            // "hi-IN" -> "hi"
            var dashIndex = culture.IndexOf('-');
            if (dashIndex > 0)
                culture = culture[..dashIndex];

            return culture.Trim().ToLowerInvariant();
        }
    }
}
