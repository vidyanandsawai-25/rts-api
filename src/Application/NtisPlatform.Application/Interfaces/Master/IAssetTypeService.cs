using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAssetTypeService
    : ICommonCrudService<
        AssetTypeEntity,
        AssetTypeDto,
        CreateAssetTypeDto,
        UpdateAssetTypeDto,
        AssetTypeQueryParameters,
        int>
{
}