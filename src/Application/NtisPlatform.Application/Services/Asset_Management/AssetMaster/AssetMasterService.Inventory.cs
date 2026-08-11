using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryAsset;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Inventory Query Methods

        public async Task<InventoryBatchListResponseDto> BuildInventoryDataAsync(int parentAssetId, CancellationToken cancellationToken)
        {
            var parentAsset = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(a => a.Id == parentAssetId)
                .Select(a => new { a.Id, a.AssetName })
                .FirstOrDefaultAsync(cancellationToken);

            var batches = await _inventoryBatchRepository.GetQueryable()
                .AsNoTracking()
                .Where(b => b.ParentAssetId == parentAssetId && b.IsActive && !b.MarkedForDeletion)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync(cancellationToken);

            var batchIds = batches.Select(b => b.Id).ToList();
            var allUnits = batchIds.Count == 0
                ? new List<InventoryAssetDetailEntity>()
                : await _inventoryAssetDetailRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(d => batchIds.Contains(d.BatchId) && d.IsActive && !d.MarkedForDeletion)
                    .ToListAsync(cancellationToken);

            var categoryIds = batches.Select(b => b.InventoryItemCategoryId).Distinct().ToList();
            var nameIds = batches.Select(b => b.InventoryItemNameId).Distinct().ToList();
            var modelIds = batches.Select(b => b.InventoryItemModelId).Distinct().ToList();
            var conditionIds = batches.Select(b => b.ConditionId).Distinct().ToList();
            var departmentIds = batches.Select(b => b.OwningDepartmentId).Distinct().ToList();

            var categories = categoryIds.Count == 0
                ? new Dictionary<int, string>()
                : await _inventoryCategoryRepository.GetQueryable().AsNoTracking()
                    .Where(x => categoryIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.TypeName, cancellationToken);

            var names = nameIds.Count == 0
                ? new Dictionary<int, string>()
                : await _inventoryNameRepository.GetQueryable().AsNoTracking()
                    .Where(x => nameIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.SubTypeName, cancellationToken);

            var models = modelIds.Count == 0
                ? new Dictionary<int, string>()
                : await _inventoryModelRepository.GetQueryable().AsNoTracking()
                    .Where(x => modelIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.ModelName, cancellationToken);

            var conditions = conditionIds.Count == 0
                ? new Dictionary<int, string>()
                : await _conditionRepository.GetQueryable().AsNoTracking()
                    .Where(x => conditionIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.ConditionName, cancellationToken);

            var departments = departmentIds.Count == 0
                ? new Dictionary<int, string>()
                : await _inventoryDepartmentRepository.GetQueryable().AsNoTracking()
                    .Where(x => departmentIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.OwningDepartmentName, cancellationToken);

            // Batch-resolve documents for every batch in one round trip instead of one query per batch.
            var docsByBatchId = await _inventoryDocumentApplicationService.GetDocumentsByInventoryBatchesAsync(batchIds, cancellationToken);

            // Group units by batch once instead of re-scanning the full allUnits list per batch below.
            var unitsByBatchId = allUnits
                .GroupBy(u => u.BatchId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var batchDetails = new List<InventoryBatchDetailDto>();
            foreach (var batch in batches)
            {
                var units = unitsByBatchId.TryGetValue(batch.Id, out var batchUnits) ? batchUnits : new List<InventoryAssetDetailEntity>();
                var docs = docsByBatchId.TryGetValue(batch.Id, out var batchDocs) ? batchDocs : new List<InventoryDocumentDto>();
                batchDetails.Add(new InventoryBatchDetailDto
                {
                    BatchId = batch.Id,
                    ParentAssetId = batch.ParentAssetId,
                    Specifications = batch.Specifications,
                    PurchaseDate = batch.PurchaseDate,
                    Quantity = batch.Quantity,
                    UnitValue = batch.UnitValue,
                    TotalBatchValue = batch.TotalBatchValue,
                    TotalBatchCV = batch.TotalBatchCV ?? units.Sum(u => u.UnitCapitalValue ?? 0m),
                    InvoiceNumber = batch.InvoiceNumber,
                    InvoiceDate = batch.InvoiceDate,
                    InvoiceFileName = batch.InvoiceFileName,
                    PhotoFileName = batch.PhotoFileName,
                    CreatedDate = batch.CreatedDate ?? DateTime.UtcNow,
                    Names = new InventoryLookupNamesDto
                    {
                        InventoryType = categories.TryGetValue(batch.InventoryItemCategoryId, out var c) ? c : string.Empty,
                        ItemName = names.TryGetValue(batch.InventoryItemNameId, out var n) ? n : string.Empty,
                        ModelBrand = models.TryGetValue(batch.InventoryItemModelId, out var m) ? m : string.Empty,
                        Condition = conditions.TryGetValue(batch.ConditionId, out var cond) ? cond : null,
                        OwningDepartment = departments.TryGetValue(batch.OwningDepartmentId, out var dept) ? dept : null
                    },
                    Units = units.Select(u => new InventoryUnitResponseDto
                    {
                        AssetId = u.AssetId,
                        AssetNo = string.Empty,
                        AssetName = string.Empty,
                        UnitNumber = u.UnitNumber,
                        Condition = u.InventoryItemConditionId.HasValue && conditions.TryGetValue(u.InventoryItemConditionId.Value, out var unitCond) ? unitCond : null,
                        UnitPurchaseValue = u.UnitPurchaseValue,
                        UnitCapitalValue = u.UnitCapitalValue
                    }).OrderBy(u => u.UnitNumber).ToList(),
                    Documents = docs
                });
            }

            return new InventoryBatchListResponseDto
            {
                ParentAssetId = parentAssetId,
                ParentAssetName = parentAsset?.AssetName ?? string.Empty,
                TotalBatches = batchDetails.Count,
                TotalUnits = batchDetails.Sum(b => b.Quantity),
                TotalPurchaseValue = batchDetails.Sum(b => b.TotalBatchValue),
                TotalCapitalValue = batchDetails.Sum(b => b.TotalBatchCV),
                Batches = batchDetails
            };
        }

        public async Task<List<int>> GetAllocatedAssetIdsAsync(int parentAssetId, CancellationToken cancellationToken)
        {
            return await _inventoryAssetDetailRepository.GetQueryable()
                .Where(iad => _repository.GetQueryable()
                    .Any(a => a.ParentAssetId == parentAssetId && a.Id == iad.AssetId))
                .Select(iad => iad.AssetId)
                .ToListAsync(cancellationToken);
        }

        #endregion
    }
}
