using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services;

/// <inheritdoc cref="IApartmentQcSearchService"/>
public sealed class ApartmentQcSearchService : IApartmentQcSearchService
{
    private const int MaxSuggestionResults = 100;

    private readonly IApartmentQcSearchRepository _repository;

    public ApartmentQcSearchService(IApartmentQcSearchRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ApartmentQcSearchSuggestionDto>> GetSuggestionsAsync(
        int wardId, string? propertyNo, string? partitionNo, int maxResults, CancellationToken cancellationToken = default)
    {
        var clampedMaxResults = Math.Clamp(maxResults <= 0 ? 20 : maxResults, 1, MaxSuggestionResults);
        return _repository.GetSuggestionsAsync(wardId, propertyNo, partitionNo, clampedMaxResults, cancellationToken);
    }
}
