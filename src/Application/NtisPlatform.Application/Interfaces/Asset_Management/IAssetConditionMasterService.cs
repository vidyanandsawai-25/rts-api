using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAssetConditionMasterService
    : ICommonCrudService<
        AssetConditionMasterEntity,
        AssetConditionMasterDto,
        CreateAssetConditionMasterDto,
        UpdateAssetConditionMasterDto,
        AssetConditionMasterQueryParameters,
        int>
{
}
