using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAssetOrganizationMasterService
    : ICommonCrudService<
        AssetOrganizationMasterEntity,
        AssetOrganizationMasterDto,
        CreateAssetOrganizationMasterDto,
        UpdateAssetOrganizationMasterDto,
        AssetOrganizationMasterQueryParameters,
        int>
{
}
