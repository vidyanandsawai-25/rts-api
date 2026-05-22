using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAssetCategoryService
    : ICommonCrudService<
        AssetCategoryEntity,
        AssetCategoryDto,
        CreateAssetCategoryDto,
        UpdateAssetCategoryDto,
        AssetCategoryQueryParameters,
        int>
{
}