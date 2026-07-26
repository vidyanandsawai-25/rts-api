namespace NtisPlatform.Core.Models;

/// <summary>
/// Shared result row for the Property-Wise Search screen - used by both the exact
/// WardId/PropertyNo/PartitionNo lookup and the LIKE-style suggestion/typeahead endpoint,
/// trimmed to just the fields the UI needs (not the full projection used by
/// <see cref="PropertySearchResponseDto"/> or <see cref="PropertySearchByCategoryResponseDto"/>).
/// </summary>
public class PropertySuggestionDto
{
    public int PropertyId { get; set; }
    public int ZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? UpicId { get; set; }

    /// <summary>
    /// Human-readable label for the dropdown, e.g. "123" or "123-A9".
    /// </summary>
    public string DisplayLabel { get; set; } = string.Empty;
}
