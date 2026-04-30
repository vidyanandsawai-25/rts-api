using NtisPlatform.Core.Constants;
using System.Globalization;

namespace NtisPlatform.Api.Middleware;

public class LanguageMiddleware
{
    private readonly RequestDelegate _next;

    public LanguageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var language = NormalizeLanguage(context.Request.Headers["Accept-Language"].ToString()) ?? "en";

        // Store language for localization lookups
        context.Items[HttpContextKeys.CurrentLanguage] = language;

        // Set thread culture for framework operations
        var culture = MapLanguageToCulture(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        await _next(context);
    }

    private static string? NormalizeLanguage(string? acceptLanguageHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
            return null;

        // Take first token: "hi-IN;q=0.9, en-US;q=0.8" -> "hi-IN;q=0.9"
        var token = acceptLanguageHeader.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        // Remove q-value: "hi-IN;q=0.9" -> "hi-IN"
        var noQ = token.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        // Normalize separators and reduce to base language: "hi-IN"/"hi_IN" -> "hi"
        var baseLang = noQ.Replace('_', '-')
                          .Split('-', StringSplitOptions.RemoveEmptyEntries)[0]
                          .ToLowerInvariant();

        // Allow only supported languages
        return baseLang switch
        {
            "hi" => "hi",
            "mr" => "mr",
            "en" => "en",
            _ => "en"
        };
    }

    /// <summary>
    /// Maps normalized language code to full CultureInfo.
    /// </summary>
    private static CultureInfo MapLanguageToCulture(string language)
    {
        return language switch
        {
            "hi" => new CultureInfo("hi-IN"),
            "mr" => new CultureInfo("mr-IN"),
            _ => new CultureInfo("en-US")
        };
    }
}