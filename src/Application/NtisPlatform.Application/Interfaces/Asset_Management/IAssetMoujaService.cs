using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

public interface IAssetMoujaService : ICommonCrudService<AssetMoujaMasterEntity, AssetMoujaDto, CreateAssetMoujaDto, UpdateAssetMoujaDto, AssetMoujaQueryParameters, int>
{
}
