using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.MultilingualDetail;

public class MultilingualTranslationQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? Id { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? Resource { get; set; }

    /// <summary>
    /// Flag to enable/disable auto-translation.
    /// Translation only occurs when both this flag and backend config are enabled.
    /// </summary>
    public bool IsAutoTranslate { get; set; } = false;

    /// <summary>
    /// List of language column names to filter for empty/null values.
    /// Accepts short codes ("hi", "mr", "en") or full column names ("hi_IN", "mr_IN", "en_US").
    /// Records will be returned only if ALL specified language columns are empty/null.
    /// Example: FilterEmptyLanguages=hi&amp;FilterEmptyLanguages=mr returns records where both hi_IN AND mr_IN are empty.
    /// </summary>
    public List<string>? FilterEmptyLanguages { get; set; }

}
