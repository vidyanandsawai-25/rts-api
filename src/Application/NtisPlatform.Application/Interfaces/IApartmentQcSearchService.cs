using NtisPlatform.Application.DTOs.Property.ApartmentQC;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application service for the ApartmentQC Search typeahead.
/// </summary>
public interface IApartmentQcSearchService
{
    Task<List<ApartmentQcSearchSuggestionDto>> GetSuggestionsAsync(
        int wardId,
        string? propertyNo,
        string? partitionNo,
        int maxResults,
        CancellationToken cancellationToken = default);
}
