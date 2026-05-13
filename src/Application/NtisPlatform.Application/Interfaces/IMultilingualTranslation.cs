using NtisPlatform.Application.DTOs.Master.MultilingualDetail;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IMultilingualTranslation : ICommonCrudService<MultilingualResourceEntity, MultilingualTranslationDtos, CreateMultilingualTranslationDtos, UpdateMultilingualTranslationDtos, MultilingualTranslationQueryParameters, int>
{
    Task<IEnumerable<string>> GetResourcesAsync(CancellationToken cancellationToken = default);
}

