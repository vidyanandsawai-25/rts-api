namespace NtisPlatform.Application.Models;

/// <summary>
/// Represents a single localization entry for batch operations.
/// </summary>
public class LocalizationEntry
{
    public string Resource { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Language { get; set; } = "en";

    /// <summary>
    /// Generated key: {Resource}_{EntityId}_{PropertyName}
    /// </summary>
    public string Key => $"{Resource}_{EntityId}_{PropertyName}";
}
