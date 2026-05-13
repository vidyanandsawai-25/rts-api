namespace NtisPlatform.Application.Options;

/// <summary>
/// Configuration options for the localization system.
/// Bound to the "Localization" section in appsettings.json.
/// </summary>
public class LocalizationOptions
{
    /// <summary>
    /// The default language used for all write operations (Create/Update).
    /// All user-supplied values are always stored in this language column.
    /// Read operations use the user's requested language with fallback to this default.
    /// Example: "en" writes to en_US column.
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";
}
