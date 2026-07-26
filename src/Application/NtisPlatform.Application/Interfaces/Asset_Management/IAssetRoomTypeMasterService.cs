using NtisPlatform.Application.DTOs.Master.AssetRoomType;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAssetRoomTypeMasterService
    : ICommonCrudService<AssetRoomTypeMasterEntity, AssetRoomTypeMasterDto, CreateAssetRoomTypeDto, UpdateAssetRoomTypeDto, AssetRoomTypeQueryParameters, int>
{
}
