using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAssetAuthorityMasterService
    : ICommonCrudService<
        AssetAuthorityMasterEntity,
        AssetAuthorityMasterDto,
        CreateAssetAuthorityMasterDto,
        UpdateAssetAuthorityMasterDto,
        AssetAuthorityMasterQueryParameters,
        int>
{
}
