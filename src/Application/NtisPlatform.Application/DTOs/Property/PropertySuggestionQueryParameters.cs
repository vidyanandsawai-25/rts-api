namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Query parameters for the Property-Wise Search typeahead: as the user progressively fills in
/// WardId, then a partial PropertyNo (and optionally PartitionNo), returns suggestions whose
/// value contains the given term(s) anywhere (SQL LIKE '%term%' semantics), not just as a prefix.
/// </summary>
public class PropertySuggestionQueryParameters
{
    /// <summary>
    /// Ward to scope suggestions to. Required.
    /// </summary>
    public int WardId { get; set; }

    /// <summary>
    /// Partial PropertyNo term. Matches anywhere in the value, e.g. "1" matches "123", "812", "10".
    /// </summary>
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Partial PartitionNo term. Matches anywhere in the value.
    /// </summary>
    public string? PartitionNo { get; set; }

    /// <summary>
    /// Maximum number of suggestions to return. Defaults to 20, capped server-side at 100.
    /// </summary>
    public int MaxResults { get; set; } = 200;
}
