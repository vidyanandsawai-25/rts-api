using NtisPlatform.Core.Exceptions;

namespace NtisPlatform.Core.Exceptions;

/// <summary>
/// Exception thrown when an asset photo is not found
/// </summary>
public class AssetPhotoNotFoundException : EntityNotFoundException
{
    public AssetPhotoNotFoundException(int photoId)
        : base("AssetPhoto", photoId, "ASSET_PHOTO_NOT_FOUND")
    {
    }
}
