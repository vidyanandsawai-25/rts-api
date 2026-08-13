using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Endpoint: Activate Asset

        /// <summary>
        /// Activates asset, its field values, and child assets where ParentAssetId equals the given asset id.
        /// Also activates related hierarchy (floors, rooms, plots) if not marked for deletion.
        /// </summary>
        public async Task<bool> ActivateAssetAndFieldValuesAsync(int assetId, CancellationToken cancellationToken = default)
        {
            if (assetId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(assetId), "Asset id must be greater than zero.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var committed = false;
            try
            {
                var asset = await _repository.GetQueryable()
                    .FirstOrDefaultAsync(a => a.Id == assetId && !a.MarkedForDeletion, cancellationToken);

                if (asset == null)
                {
                    return false;
                }

                var now = DateTime.UtcNow;
                asset.IsActive = true;
                asset.UpdatedDate = now;

                var fieldValues = await _fieldValueRepository.GetQueryable()
                    .Where(fv => fv.AssetId == assetId && !fv.MarkedForDeletion)
                    .ToListAsync(cancellationToken);

                foreach (var fieldValue in fieldValues)
                {
                    fieldValue.IsActive = true;
                    fieldValue.UpdatedDate = now;
                }

                var details = await _detailsRepository.GetQueryable()
                    .FirstOrDefaultAsync(d => d.AssetId == assetId && !d.MarkedForDeletion, cancellationToken);
                if (details != null)
                {
                    details.IsActive = true;
                    details.UpdatedDate = now;
                    await _detailsRepository.UpdateAsync(details, cancellationToken);
                }

                var childAssets = await _repository.GetQueryable()
                    .Where(a => a.ParentAssetId == assetId && !a.MarkedForDeletion)
                    .ToListAsync(cancellationToken);

                foreach (var childAsset in childAssets)
                {
                    childAsset.IsActive = true;
                    childAsset.UpdatedDate = now;
                }

                var allAssetIds = new List<int> { assetId };
                allAssetIds.AddRange(childAssets.Select(x => x.Id));

                // Activate lease-rent details for parent/child assets.
                await ActivateLeaseRentDetailsAsync(allAssetIds, now, cancellationToken);

                // Fetch Floors
                var floors = await _floorDetailsRepository.GetQueryable()
                    .Where(x => allAssetIds.Contains(x.AssetId))
                    .ToListAsync(cancellationToken);

                var floorIds = floors.Select(x => x.Id).ToList();

                foreach (var floor in floors)
                {
                    if (!floor.MarkedForDeletion)
                    {
                        floor.IsActive = true;
                        floor.UpdatedDate = now;
                    }
                }

                // Fetch Rooms and Minus Data
                var rooms = await _roomWiseSubmissionRepository.GetQueryable()
                    .Include(x => x.RoomMinusData)
                    .Where(x => x.SubUnitsDetailsId.HasValue && floorIds.Contains(x.SubUnitsDetailsId.Value))
                    .ToListAsync(cancellationToken);

                foreach (var room in rooms)
                {
                    if (!room.MarkedForDeletion)
                    {
                        room.IsActive = true;
                        room.UpdatedDate = now;
                    }

                    if (room.RoomMinusData != null)
                    {
                        foreach (var minus in room.RoomMinusData)
                        {
                            if (!minus.MarkedForDeletion) minus.IsActive = true;
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                committed = true;

                _logger.LogInformation(
                    "Activated asset {AssetId}, field values, child assets, lease-rent details, floors, and rooms.",
                    assetId);

                return true;
            }
            finally
            {
                if (!committed)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                }
            }
        }

        #endregion
    }
}
