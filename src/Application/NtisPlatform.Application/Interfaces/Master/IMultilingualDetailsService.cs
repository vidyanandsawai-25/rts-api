using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IMultilingualDetailsService : ICommonCrudService<MultilingualDetailsEntity, MultilingualDetailsDtos, CreateMultilingualDetailsDtos, UpdateMultilingualDetailsDtos, MultilingualDetailsQueryParameters, int>
{
    Task<List<MultilingualDetailsDtos>> GetAllForLocalizationAsync(string resource, string culture, CancellationToken ct);

}

public interface IMultilingualResourceProvider
{
    Task<Dictionary<string, string>> GetAsync(string resource, string culture, CancellationToken ct = default);
    void Invalidate(string resource, string culture);
}
