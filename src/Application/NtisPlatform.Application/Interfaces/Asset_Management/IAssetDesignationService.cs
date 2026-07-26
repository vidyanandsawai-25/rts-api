using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IAssetDesignationService : ICommonCrudService<
        AssetDesignationEntity,
        AssetDesignationDto,
        CreateAssetDesignationDto,
        UpdateAssetDesignationDto,
        AssetDesignationQueryParameters,
        int>
    {
    }
}
