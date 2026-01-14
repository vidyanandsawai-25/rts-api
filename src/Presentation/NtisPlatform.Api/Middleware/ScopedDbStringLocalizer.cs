using Microsoft.Extensions.Localization;
using NtisPlatform.Application.Interfaces;
using System.Globalization;

namespace NtisPlatform.Api.Middleware
{
    /// <summary>
    /// DB-backed IStringLocalizer implementation that resolves translations from a scoped provider.
    /// 
    /// Why scope is needed:
    /// - The localization factory (IStringLocalizerFactory) is registered as Singleton by ASP.NET Core.
    /// - Your translation provider (IMultilingualResourceProvider) is Scoped (it may use DbContext, repos, etc.).
    /// - Singleton cannot directly resolve Scoped services from the root container.
    /// 
    /// So for each localization lookup we create a scope, resolve the provider,
    /// fetch dictionary for (resource, culture) and return the translated value.
    /// 
    /// NOTE: Your provider should cache internally (IMemoryCache) so DB is not hit every time.
    /// </summary>
    internal sealed class ScopedDbStringLocalizer : IStringLocalizer
    {
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// "Resource" name used for localization lookup.
        /// Example: "ValidationMessages"
        /// This should match the Resource column in your MultilingualDetails table.
        /// </summary>
        private readonly string _resource;

        public ScopedDbStringLocalizer(IServiceScopeFactory scopeFactory, string resource)
        {
            _scopeFactory = scopeFactory;
            _resource = resource;
        }

        /// <summary>
        /// Resolve a localized string for the given key.
        /// 
        /// Example:
        ///   key = "FloorID_Required"
        ///   CultureInfo.CurrentUICulture = "hi"
        /// 
        /// Will attempt lookup in provider using:
        ///   resource = "ValidationMessages" (or whatever _resource is)
        ///   culture  = "hi"
        /// </summary>
        public LocalizedString this[string name]
        {
            get
            {
                // Create a scope so we can resolve Scoped services (provider/service/repo/DbContext).
                using var scope = _scopeFactory.CreateScope();

                // Provider is responsible for retrieving + caching translations.
                var provider = scope.ServiceProvider.GetRequiredService<IMultilingualResourceProvider>();

                // We are using the neutral culture: "en", "hi", "mr".
                // If your clients send "hi-IN" you can either:
                // - store "hi-IN" in DB, or
                // - update provider to fallback: hi-IN -> hi -> en.
                var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                // Provider returns dictionary of key->value for given resource & culture
                // (ideally cached in IMemoryCache).
                var dict = provider.GetAsync(_resource, culture).GetAwaiter().GetResult();

                // If found, return translation; if missing, return the key itself.
                return dict.TryGetValue(name, out var value)
                    ? new LocalizedString(name, value, resourceNotFound: false)
                    : new LocalizedString(name, name, resourceNotFound: true);
            }
        }

        /// <summary>
        /// Same as indexer, but supports string formatting:
        ///   _localizer["HelloUser", userName]
        /// </summary>
        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var s = this[name];
                var formatted = string.Format(CultureInfo.CurrentCulture, s.Value, arguments);
                return new LocalizedString(name, formatted, s.ResourceNotFound);
            }
        }

        /// <summary>
        /// Not required for validation scenarios. Can be implemented later if needed.
        /// </summary>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Array.Empty<LocalizedString>();

        /// <summary>
        /// We rely on CultureInfo.CurrentUICulture set by RequestLocalization middleware.
        /// </summary>
        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }

    /// <summary>
    /// Factory used by ASP.NET Core to create IStringLocalizer instances.
    /// 
    /// Important:
    /// - Registered as Singleton.
    /// - Uses IServiceScopeFactory to allow localizer to resolve Scoped dependencies safely.
    /// 
    /// How "resource" is chosen:
    /// - If you call factory.Create("ValidationMessages", null) from Program.cs,
    ///   then baseName = "ValidationMessages" (used as Resource column match in DB).
    /// </summary>
    public sealed class DbStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DbStringLocalizerFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Called when using IStringLocalizer&lt;T&gt;. We use the type name as resource name.
        /// (In your setup for validation you override this in DataAnnotationLocalizerProvider
        /// to always use "ValidationMessages".)
        /// </summary>
        public IStringLocalizer Create(Type resourceSource)
            => Create(resourceSource.Name, location: null);

        /// <summary>
        /// Called when using factory.Create("SomeResource", null).
        /// baseName becomes the DB Resource value used for lookup.
        /// </summary>
        public IStringLocalizer Create(string baseName, string? location)
            => new ScopedDbStringLocalizer(_scopeFactory, baseName);
    }
}
