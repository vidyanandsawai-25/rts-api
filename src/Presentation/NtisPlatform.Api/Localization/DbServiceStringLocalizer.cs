using Microsoft.Extensions.Localization;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using System.Globalization;

namespace NtisPlatform.Api.Localization;

/// DB/service-backed localizer used by ASP.NET Core localization pipeline. This allows MVC/DataAnnotations to resolve validation keys from ILocalizationService (in-memory DB cache).
public sealed class DbServiceStringLocalizer : IStringLocalizer
{
    private readonly ILocalizationService _localizationService;
    private readonly string _resource;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbServiceStringLocalizer(ILocalizationService localizationService, string resource,
        IHttpContextAccessor httpContextAccessor)
    {
        _localizationService = localizationService;
        _resource = resource;
        _httpContextAccessor = httpContextAccessor;
    }

    /// Resolves a translation for the current UI culture. If not found, returns the key itself.
    public LocalizedString this[string name]
    {
        get
        {
            // May be "hi-IN", "mr-IN" etc. LocalizationService handles normalization + fallback chain.
            var culture = _httpContextAccessor.HttpContext?.Items[HttpContextKeys.CurrentLanguage] as string ?? "en";

            var value = _localizationService.GetTranslation(_resource, culture, name);

            // If translation not found, service returns the key itself, so mark as not found.
            var notFound = string.Equals(value, name, StringComparison.Ordinal);
            return new LocalizedString(name, value, resourceNotFound: notFound);
        }
    }

    /// Resolves a translation and formats it with arguments (string.Format behavior).
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var s = this[name];
            var formatted = string.Format(CultureInfo.CurrentCulture, s.Value, arguments);
            return new LocalizedString(name, formatted, s.ResourceNotFound);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => Array.Empty<LocalizedString>();

    public IStringLocalizer WithCulture(CultureInfo culture) => this;
}

/// Factory used by ASP.NET Core to create IStringLocalizer instances. Routes all lookups through DbServiceStringLocalizer (ILocalizationService).
public sealed class DbServiceStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILocalizationService _localizationService;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbServiceStringLocalizerFactory(ILocalizationService localizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _localizationService = localizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public IStringLocalizer Create(Type resourceSource)
        => Create(resourceSource.Name, location: null);

    public IStringLocalizer Create(string baseName, string? location)
        => new DbServiceStringLocalizer(_localizationService, baseName , _httpContextAccessor);
}
