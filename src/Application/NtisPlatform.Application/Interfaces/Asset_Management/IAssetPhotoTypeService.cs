using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

public interface IAssetPhotoTypeService :
    ICommonCrudService<AssetPhotoTypeEntity, AssetPhotoTypeDto, CreateAssetPhotoTypeDto, UpdateAssetPhotoTypeDto, AssetPhotoTypeQueryParameters, int>
{
}
