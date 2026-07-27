using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

public interface IAssetTypeOfUseGroupService : ICommonCrudService<AssetTypeOfUseGroupEntity, AssetTypeOfUseGroupDto, CreateAssetTypeOfUseGroupDto, UpdateAssetTypeOfUseGroupDto, AssetTypeOfUseGroupQueryParameters, int>
{
}
