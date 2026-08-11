using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Endpoint: Bulk Save Field Values

        public async Task<bool> BulkSaveFieldValuesAsync(int assetId, List<CreateAssetFieldValueDto> fieldValues, CancellationToken cancellationToken = default)
        {
            if (fieldValues == null || !fieldValues.Any())
                return true;

            var existingFields = await _fieldValueRepository.GetQueryable()
                .Where(fv => fv.AssetId == assetId && fv.FieldDefinitionId.HasValue && !fv.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var existingDict = existingFields.ToDictionary(f => f.FieldDefinitionId!.Value);

            var newEntities = new List<AssetFieldValueEntity>();
            var entitiesToUpdate = new List<AssetFieldValueEntity>();

            foreach (var dto in fieldValues)
            {
                if (dto.FieldDefinitionId.HasValue && existingDict.TryGetValue(dto.FieldDefinitionId.Value, out var existingField))
                {
                    existingField.FieldName = dto.FieldName;
                    existingField.FieldValue = dto.FieldValue;
                    existingField.UpdatedDate = DateTime.UtcNow;
                    existingField.UpdatedBy = dto.CreatedBy;

                    entitiesToUpdate.Add(existingField);
                    existingDict.Remove(dto.FieldDefinitionId.Value);
                }
                else
                {
                    var newEntity = new AssetFieldValueEntity
                    {
                        AssetId = assetId,
                        FieldDefinitionId = dto.FieldDefinitionId,
                        FieldName = dto.FieldName,
                        FieldValue = dto.FieldValue,
                        CreatedBy = dto.CreatedBy,
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    newEntities.Add(newEntity);
                }
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                if (newEntities.Any())
                {
                    await _fieldValueRepository.AddRangeAsync(newEntities.ToArray(), cancellationToken);
                }

                if (entitiesToUpdate.Any())
                {
                    foreach (var entity in entitiesToUpdate)
                    {
                        await _fieldValueRepository.UpdateAsync(entity, cancellationToken);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk save field values for AssetId: {AssetId}", assetId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return false;
            }
        }

        #endregion
    }
}
