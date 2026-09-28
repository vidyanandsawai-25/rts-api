using NtisPlatform.Application.DTOs.Property.ApartmentQC;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Data-access for the ApartmentQC Search typeahead.
/// </summary>
public interface IApartmentQcSearchRepository
{
    /// <summary>
    /// Returns up to <paramref name="maxResults"/> suggestions for the given ward, each already
    /// resolved to its Society/Unit/IndividualProperty category.
    /// </summary>
    Task<List<ApartmentQcSearchSuggestionDto>> GetSuggestionsAsync(
        int wardId,
        string? propertyNo,
        string? partitionNo,
        int maxResults,
        CancellationToken cancellationToken = default);
}
