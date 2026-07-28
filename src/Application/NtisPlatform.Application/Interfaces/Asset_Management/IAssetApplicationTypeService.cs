using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IAssetApplicationTypeService : ICommonCrudService<
        AssetApplicationTypeEntity,
        AssetApplicationTypeDto,
        CreateAssetApplicationTypeDto,
        UpdateAssetApplicationTypeDto,
        AssetApplicationTypeQueryParameters,
        int>
    {
    }
}
