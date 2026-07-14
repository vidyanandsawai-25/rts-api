using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.CommonDetails;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application-layer orchestrator for the CommonDetails / bulk-update feature. Holds the business
/// logic (validation, field whitelisting, value coercion, update orchestration, rollback policy and
/// history content) and reaches the database only through Core abstractions — repositories, the unit
/// of work, and <see cref="IDynamicEntityLoader"/> for the one dynamically-typed load. The table-name
/// → entity mapping lives in <see cref="BulkUpdateTargetRegistry"/>, not in this service.
/// </summary>
public class CommonDetailsService : ICommonDetailsService
{
    private readonly IRepository<BulkUpdateMasterEntity> _masterRepo;
    private readonly IRepository<BulkUpdateFieldConfigEntity> _fieldConfigRepo;
    private readonly IRepository<BulkUpdateHistoryEntity> _historyRepo;
    private readonly IRepository<PropertyEntity> _propertyRepo;
    private readonly IRepository<WardEntity> _wardRepo;
    private readonly IRepository<SocietyDetailsEntity> _societyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDynamicEntityLoader _entityLoader;
    private readonly ILogger<CommonDetailsService> _logger;

    public CommonDetailsService(
        IRepository<BulkUpdateMasterEntity> masterRepo,
        IRepository<BulkUpdateFieldConfigEntity> fieldConfigRepo,
        IRepository<BulkUpdateHistoryEntity> historyRepo,
        IRepository<PropertyEntity> propertyRepo,
        IRepository<WardEntity> wardRepo,
        IRepository<SocietyDetailsEntity> societyRepo,
        IUnitOfWork unitOfWork,
        IDynamicEntityLoader entityLoader,
        ILogger<CommonDetailsService> logger)
    {
        _masterRepo = masterRepo;
        _fieldConfigRepo = fieldConfigRepo;
        _historyRepo = historyRepo;
        _propertyRepo = propertyRepo;
        _wardRepo = wardRepo;
        _societyRepo = societyRepo;
        _unitOfWork = unitOfWork;
        _entityLoader = entityLoader;
        _logger = logger;
    }

    public async Task<List<BulkUpdateMasterDto>> GetMenuAsync(CancellationToken ct)
    {
        return await _masterRepo.GetQueryable()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplaySequence)
            .Select(m => new BulkUpdateMasterDto
            {
                Id = m.Id,
                UpdateCode = m.UpdateCode,
                UpdateName = m.UpdateName,
                UpdateNameMarathi = m.UpdateNameMarathi,
                ReferenceTableName = m.ReferenceTableName,
                IsActive = m.IsActive,
                DisplaySequence = m.DisplaySequence,
                Description = m.Description,
                Category = m.Category,
                IsApprovalRequired = m.IsApprovalRequired
            })
            .ToListAsync(ct);
    }

    public async Task<List<BulkUpdateFieldConfigDto>> GetFormFieldsAsync(string updateCode, CancellationToken ct)
    {
        return await _fieldConfigRepo.GetQueryable()
            .Where(fc => fc.IsActive
                && fc.Master != null
                && fc.Master.UpdateCode == updateCode
                && fc.Master.IsActive)
            .OrderBy(fc => fc.SequenceNo)
            .Select(fc => new BulkUpdateFieldConfigDto
            {
                Id = fc.Id,
                BulkUpdateMasterId = fc.BulkUpdateMasterId,
                FieldName = fc.FieldName,
                DisplayName = fc.DisplayName,
                DisplayNameMarathi = fc.DisplayNameMarathi,
                ControlType = fc.ControlType,
                DataType = fc.DataType,
                Placeholder = fc.Placeholder,
                IsRequired = fc.IsRequired,
                MaxLength = fc.MaxLength,
                ValidationRegex = fc.ValidationRegex,
                DefaultValue = fc.DefaultValue,
                SequenceNo = fc.SequenceNo,
                IsActive = fc.IsActive,
                IsReadonly = fc.IsReadonly,
                BindApi = fc.BindApi
            })
            .ToListAsync(ct);
    }

    public async Task<List<PreviewGridColumnDto>> GetGridColumnsAsync(string updateCode, CancellationToken ct)
    {
        var fieldConfigs = await GetFormFieldsAsync(updateCode, ct);
        var columns = new List<PreviewGridColumnDto>
        {
            new() { Key = "wardNo", Label = "Ward No", LabelMarathi = "वॉर्ड क्र." },
            new() { Key = "propertyNo", Label = "Property No", LabelMarathi = "मालमत्ता क्र." },
            new() { Key = "partitionNo", Label = "Partition No", LabelMarathi = "विभाजन क्र." },
        };
        foreach (var config in fieldConfigs)
        {
            columns.Add(new PreviewGridColumnDto
            {
                Key = config.FieldName,
                Label = config.DisplayName,
                LabelMarathi = config.DisplayNameMarathi,
            });
        }
        return columns;
    }

    public async Task<PagedResult<PropertyPreviewDto>> FilterPropertiesAsync(
        FilterPropertiesRequestDto request, CancellationToken ct)
    {
        var master = await _masterRepo.GetQueryable()
            .FirstOrDefaultAsync(m => m.UpdateCode == request.UpdateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{request.UpdateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(request.UpdateCode, ct);
        var safeColumns = fieldConfigs.Select(f => f.FieldName).ToList();
        var targetTable = master.ReferenceTableName;

        var query = _propertyRepo.GetQueryable()
            .Where(pm => pm.WardId == request.WardId);

        if (!string.IsNullOrWhiteSpace(request.PropertyNo))
        {
            var propertyNo = request.PropertyNo.Trim();
            query = query.Where(pm => pm.PropertyNo == propertyNo);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.FromPropertyNo))
            {
                var fromPropertyNo = request.FromPropertyNo.Trim();
                query = query.Where(pm => string.Compare(pm.PropertyNo, fromPropertyNo) >= 0);
            }
            if (!string.IsNullOrWhiteSpace(request.ToPropertyNo))
            {
                var toPropertyNo = request.ToPropertyNo.Trim();
                query = query.Where(pm => string.Compare(pm.PropertyNo, toPropertyNo) <= 0);
            }
        }
        if (!string.IsNullOrWhiteSpace(request.Wing))
            query = query.Where(pm => _societyRepo.GetQueryable()
                .Any(sdm => sdm.PropertyId == pm.Id && sdm.WingName == request.Wing));

        var totalCount = await query.CountAsync(ct);

        var pagedProperties = await query
            .OrderBy(pm => pm.PropertyNo)
            .Skip(request.PageSize == -1 ? 0 : (request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize == -1 ? totalCount : request.PageSize)
            .Join(_wardRepo.GetQueryable(), pm => pm.WardId, w => w.Id,
                (pm, w) => new { pm, w.WardNo })
            .ToListAsync(ct);

        var propertyIds = pagedProperties.Select(x => x.pm.Id).ToList();
        var relatedEntities = await LoadTargetEntitiesAsync(targetTable, propertyIds, ct);
        var isPropertyMast = BulkUpdateTargetRegistry.TryResolve(targetTable, out var previewTarget)
            && BulkUpdateTargetRegistry.IsPropertyKeyedById(previewTarget);

        var items = pagedProperties.Select(x =>
        {
            var dto = new PropertyPreviewDto
            {
                Id = x.pm.Id,
                WardNo = x.WardNo,
                PropertyNo = x.pm.PropertyNo ?? string.Empty,
                PartitionNo = x.pm.PartitionNo ?? string.Empty,
            };
            var source = isPropertyMast ? (object?)x.pm : relatedEntities.GetValueOrDefault(x.pm.Id);
            if (source != null)
                PopulateCurrentValues(dto, source, safeColumns);
            return dto;
        }).ToList();

        return new PagedResult<PropertyPreviewDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    /// <summary>
    /// Loads the related rows (one per property, first match) used to show current values in the
    /// preview, for a target table keyed by <c>PropertyId</c>. Property-keyed-by-Id tables return
    /// empty (the property row itself is the source); unknown tables log a warning and return empty.
    /// </summary>
    private async Task<Dictionary<int, object>> LoadTargetEntitiesAsync(
        string targetTable, List<int> propertyIds, CancellationToken ct)
    {
        if (!BulkUpdateTargetRegistry.TryResolve(targetTable, out var target))
        {
            _logger.LogWarning("No entity mapped for target table {TargetTable}; CurrentValues will be empty.", targetTable);
            return [];
        }

        if (BulkUpdateTargetRegistry.IsPropertyKeyedById(target))
            return [];

        var rows = await _entityLoader.LoadByKeyAsync(
            target.EntityType, target.KeyProperty,
            propertyIds.Select(id => (long)id).ToList(), asNoTracking: true, ct);

        var keyProp = target.EntityType.GetProperty(
            target.KeyProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;

        var result = new Dictionary<int, object>();
        foreach (var row in rows)
        {
            var keyValue = keyProp.GetValue(row);
            if (keyValue is null)
                continue;
            var key = Convert.ToInt32(keyValue);
            // First row per property, matching the previous GroupBy(..).First() behavior.
            result.TryAdd(key, row);
        }
        return result;
    }

    private static void PopulateCurrentValues(PropertyPreviewDto dto, object entity, List<string> columns)
    {
        var type = entity.GetType();
        foreach (var col in columns)
        {
            var prop = type.GetProperty(col, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            dto.CurrentValues[col] = prop?.GetValue(entity);
        }
    }

    public async Task<BulkUpdateResultDto> BulkUpdateAsync(
        BulkUpdateRequestDto request, int updatedBy, string? ipAddress, CancellationToken ct)
    {
        var master = await _masterRepo.GetQueryable()
            .FirstOrDefaultAsync(m => m.UpdateCode == request.UpdateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{request.UpdateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(request.UpdateCode, ct);

        // Normalize to a case-insensitive dictionary once so that all downstream lookups
        // ("plotArea" vs "PlotArea") are treated the same regardless of client casing.
        var updateData = new Dictionary<string, object?>(request.UpdateData, StringComparer.OrdinalIgnoreCase);

        // Dynamic validation
        var errors = ValidateFieldValues(fieldConfigs, updateData);
        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors));

        // Whitelist field names against the configured fields.
        var allowedFields = fieldConfigs.Select(f => f.FieldName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fieldsToUpdate = updateData.Keys.Where(allowedFields.Contains).ToList();

        if (fieldsToUpdate.Count == 0)
            throw new ArgumentException("No valid fields to update.");

        // Resolve which entity this update targets via the central registry. An unknown reference
        // table is rejected here (the registry's keys are the supported-table allow-list).
        if (!BulkUpdateTargetRegistry.TryResolve(master.ReferenceTableName, out var target))
            throw new InvalidOperationException(
                $"Update type '{request.UpdateCode}' references an unrecognized table '{master.ReferenceTableName}'. " +
                "Add it to BulkUpdateTargetRegistry if this table is intentionally supported.");

        // Every field must map to a property on the target entity — EF works through entity
        // properties, so surface an unmapped field explicitly instead of silently skipping it.
        var unmappedFields = fieldsToUpdate
            .Where(f => target.EntityType.GetProperty(
                f, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is null)
            .ToList();
        if (unmappedFields.Count > 0)
            throw new InvalidOperationException(
                $"Configured field name(s) are not mapped to a property on entity '{target.EntityType.Name}': " +
                string.Join(", ", unmappedFields));

        var propertyIds = request.PropertyIds;
        var result = new BulkUpdateResultDto { TotalRequested = propertyIds.Count };

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // O(N) optimization: Batch load all entities in a single database call instead of N calls.
            var allTargets = await _entityLoader.LoadByKeyAsync(
                target.EntityType, target.KeyProperty,
                propertyIds.Select(id => (long)id).ToList(),
                asNoTracking: false, ct);

            // Group entities by their key property value for O(1) lookup per property.
            var keyProp = target.EntityType.GetProperty(
                target.KeyProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;

            var entitiesByPropertyId = allTargets
                .GroupBy(e => Convert.ToInt32(keyProp.GetValue(e)))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Pre-compute values that are identical across all properties.
            var newValueJson = JsonSerializer.Serialize(
                fieldsToUpdate.ToDictionary(f => f, f => updateData[f]));
            var updatedColumnsStr = string.Join(",", fieldsToUpdate);
            var now = DateTime.Now;

            foreach (var propertyId in propertyIds)
            {
                try
                {
                    // O(1) lookup from pre-loaded dictionary instead of O(1) database call.
                    var targets = entitiesByPropertyId.TryGetValue(Convert.ToInt32(propertyId), out var found)
                        ? found
                        : new List<BaseEntity>();

                    // Old-value snapshot mirrors the original single-row read: first row only.
                    var oldValue = targets.Count > 0
                        ? JsonSerializer.Serialize(SnapshotFields(targets[0], fieldsToUpdate))
                        : null;

                    foreach (var entity in targets)
                    {
                        ApplyFieldValues(entity, fieldsToUpdate, updateData);
                        entity.UpdatedBy = updatedBy;
                        entity.UpdatedDate = now;
                    }

                    await _historyRepo.AddAsync(new BulkUpdateHistoryEntity
                    {
                        BulkUpdateMasterId = master.Id,
                        PropertyId = propertyId,
                        OldValue = oldValue,
                        NewValue = newValueJson,
                        UpdatedColumns = updatedColumnsStr,
                        UpdatedBy = updatedBy,
                        IpAddress = ipAddress,
                        UpdatedDate = now,
                        CreatedDate = now,
                    }, ct);

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update property {PropertyId}", propertyId);
                    result.FailedCount++;
                    result.Errors.Add($"Property {propertyId}: {ex.Message}");
                    _unitOfWork.DiscardChanges();
                }
            }

            if (result.FailedCount > 0)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                result.Errors.Insert(0,
                    $"Transaction rolled back — no properties were updated. " +
                    $"{result.SuccessCount} of {result.TotalRequested} processed before the error(s) occurred, " +
                    "but all changes were reverted.");
                result.SuccessCount = 0;
            }
            else
            {
                // O(N) optimization: Single SaveChangesAsync call instead of N calls.
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return result;
    }

    public async Task<byte[]> ExportPropertiesToExcelAsync(FilterPropertiesRequestDto request, CancellationToken ct)
    {
        // Export the full filtered set (ignore paging), pre-filled with current values.
        var unpagedRequest = new FilterPropertiesRequestDto
        {
            WardId = request.WardId,
            FromPropertyNo = request.FromPropertyNo,
            ToPropertyNo = request.ToPropertyNo,
            PropertyNo = request.PropertyNo,
            Wing = request.Wing,
            UpdateCode = request.UpdateCode,
            SearchTerm = request.SearchTerm,
            SortBy = request.SortBy,
            SortOrder = request.SortOrder,
            FilterLogic = request.FilterLogic,
            PageNumber = 1,
            PageSize = -1
        };
        var paged = await FilterPropertiesAsync(unpagedRequest, ct);

        var fieldNames = (await GetFormFieldsAsync(request.UpdateCode, ct))
            .Select(f => f.FieldName)
            .ToList();

        var headers = new List<string> { "wardNo", "propertyNo", "partitionNo" };
        headers.AddRange(fieldNames);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Properties");

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = worksheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
        }

        var rows = paged.Items.ToList();
        for (var r = 0; r < rows.Count; r++)
        {
            var item = rows[r];
            worksheet.Cell(r + 2, 1).Value = item.WardNo;
            worksheet.Cell(r + 2, 2).Value = item.PropertyNo;
            worksheet.Cell(r + 2, 3).Value = item.PartitionNo;
            for (var f = 0; f < fieldNames.Count; f++)
                SetCellValue(worksheet.Cell(r + 2, 4 + f), item.CurrentValues.GetValueOrDefault(fieldNames[f]));
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<BulkUpdateResultDto> ImportPropertiesFromExcelAsync(string updateCode, Stream fileStream, int updatedBy, string? ipAddress, CancellationToken ct)
    {
        var master = await _masterRepo.GetQueryable()
            .FirstOrDefaultAsync(m => m.UpdateCode == updateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{updateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(updateCode, ct);

        if (!BulkUpdateTargetRegistry.TryResolve(master.ReferenceTableName, out var target))
            throw new InvalidOperationException(
                $"Update type '{updateCode}' references an unrecognized table '{master.ReferenceTableName}'. " +
                "Add it to BulkUpdateTargetRegistry if this table is intentionally supported.");

        var (headers, excelRows) = ExcelImportHelper.Read(fileStream);
        var headerSet = new HashSet<string>(
            headers.Where(h => !string.IsNullOrWhiteSpace(h)), StringComparer.OrdinalIgnoreCase);

        // Identity columns must be present so each row can be matched to a property.
        var missingIdentity = new[] { "wardNo", "propertyNo", "partitionNo" }
            .Where(h => !headerSet.Contains(h))
            .ToList();
        if (missingIdentity.Count > 0)
            throw new ArgumentException($"Missing required column(s): {string.Join(", ", missingIdentity)}.");

        // Value columns = configured fields whose header appears in the sheet (canonical FieldName casing).
        var presentConfigs = fieldConfigs.Where(f => headerSet.Contains(f.FieldName)).ToList();
        var valueFieldNames = presentConfigs.Select(f => f.FieldName).ToList();
        if (valueFieldNames.Count == 0)
            throw new ArgumentException("The uploaded file has no updatable value columns for this update type.");

        var unmappedFields = valueFieldNames
            .Where(f => target.EntityType.GetProperty(
                f, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is null)
            .ToList();
        if (unmappedFields.Count > 0)
            throw new InvalidOperationException(
                $"Configured field name(s) are not mapped to a property on entity '{target.EntityType.Name}': " +
                string.Join(", ", unmappedFields));

        var result = new BulkUpdateResultDto { TotalRequested = excelRows.Count };
        if (excelRows.Count == 0)
            throw new ArgumentException("The uploaded file has no data rows.");

        // Extract identity tuples from Excel rows for narrowed query filtering.
        var excelIdentities = excelRows
            .Select(r => new
            {
                WardNo = r.Cells.GetValueOrDefault("wardNo")?.Trim(),
                PropertyNo = r.Cells.GetValueOrDefault("propertyNo")?.Trim(),
                PartitionNo = r.Cells.GetValueOrDefault("partitionNo")?.Trim()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.WardNo))
            .ToList();

        // Distinct ward numbers for the initial filter.
        var wardNos = excelIdentities
            .Select(x => x.WardNo!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Distinct property numbers to narrow the query (avoids loading entire ward).
        var propertyNos = excelIdentities
            .Select(x => x.PropertyNo)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Distinct partition numbers for additional narrowing (when present in the sheet).
        var partitionNos = excelIdentities
            .Select(x => x.PartitionNo)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Build query with narrowed filters: WardNo + PropertyNo (+ PartitionNo if available).
        var candidatesQuery = _propertyRepo.GetQueryable()
            .Join(_wardRepo.GetQueryable(), pm => pm.WardId, w => w.Id,
                (pm, w) => new { pm.Id, w.WardNo, pm.PropertyNo, pm.PartitionNo })
            .Where(x => wardNos.Contains(x.WardNo));

        // Filter by PropertyNo when the Excel data has property numbers.
        if (propertyNos.Count > 0)
            candidatesQuery = candidatesQuery.Where(x => x.PropertyNo != null && propertyNos.Contains(x.PropertyNo));

        // Filter by PartitionNo when the Excel data has partition numbers.
        if (partitionNos.Count > 0)
            candidatesQuery = candidatesQuery.Where(x => x.PartitionNo != null && partitionNos.Contains(x.PartitionNo));

        var candidates = await candidatesQuery.ToListAsync(ct);

        var idsByKey = candidates
            .GroupBy(x => IdentityKey(x.WardNo, x.PropertyNo, x.PartitionNo))
            .ToDictionary(g => g.Key, g => g.Select(x => Convert.ToInt64(x.Id)).ToList());

        // Validate + resolve every row up front. All-or-nothing: touch the DB only if all rows are clean.
        var errors = new List<string>();
        var rowUpdates = new List<(long PropertyId, Dictionary<string, object?> Values)>();

        foreach (var row in excelRows)
        {
            var wardNo = row.Cells.GetValueOrDefault("wardNo")?.Trim();
            var propertyNo = row.Cells.GetValueOrDefault("propertyNo")?.Trim();
            var partitionNo = row.Cells.GetValueOrDefault("partitionNo")?.Trim();

            if (string.IsNullOrWhiteSpace(wardNo) || string.IsNullOrWhiteSpace(propertyNo))
            {
                errors.Add($"Row {row.RowNumber}: wardNo and propertyNo are required.");
                continue;
            }

            var key = IdentityKey(wardNo, propertyNo, partitionNo);

            if (!idsByKey.TryGetValue(key, out var ids))
            {
                errors.Add($"Row {row.RowNumber}: no property found for wardNo='{wardNo}', propertyNo='{propertyNo}', partitionNo='{partitionNo}'.");
                continue;
            }
            if (ids.Count > 1)
            {
                errors.Add($"Row {row.RowNumber}: multiple properties match wardNo='{wardNo}', propertyNo='{propertyNo}', partitionNo='{partitionNo}'.");
                continue;
            }

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in valueFieldNames)
                values[field] = row.Cells.GetValueOrDefault(field);

            var rowErrors = ValidateFieldValues(presentConfigs, values);
            if (rowErrors.Count > 0)
            {
                errors.AddRange(rowErrors.Select(e => $"Row {row.RowNumber}: {e}"));
                continue;
            }

            rowUpdates.Add((ids[0], values));
        }

        if (rowUpdates.Count < excelRows.Count)
        {
            result.FailedCount = excelRows.Count - rowUpdates.Count;
            result.Errors.Add("No changes were applied — fix the listed row error(s) and re-upload (all-or-nothing).");
            result.Errors.AddRange(errors);
            return result;
        }

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var propertyIds = rowUpdates.Select(u => u.PropertyId).ToList();
            var allTargets = await _entityLoader.LoadByKeyAsync(
                target.EntityType, target.KeyProperty, propertyIds, asNoTracking: false, ct);

            var keyProp = target.EntityType.GetProperty(
                target.KeyProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;

            var entitiesByPropertyId = allTargets
                .GroupBy(e => Convert.ToInt64(keyProp.GetValue(e)))
                .ToDictionary(g => g.Key, g => g.ToList());

            var now = DateTime.Now;

            foreach (var (propertyId, values) in rowUpdates)
            {
                try
                {
                    var targets = entitiesByPropertyId.TryGetValue(propertyId, out var found)
                        ? found
                        : new List<BaseEntity>();

                    var fieldsToUpdate = values.Keys.ToList();
                    var oldValue = targets.Count > 0
                        ? JsonSerializer.Serialize(SnapshotFields(targets[0], fieldsToUpdate))
                        : null;

                    foreach (var entity in targets)
                    {
                        ApplyFieldValues(entity, fieldsToUpdate, values);
                        entity.UpdatedBy = updatedBy;
                        entity.UpdatedDate = now;
                    }

                    await _historyRepo.AddAsync(new BulkUpdateHistoryEntity
                    {
                        BulkUpdateMasterId = master.Id,
                        PropertyId = propertyId,
                        OldValue = oldValue,
                        NewValue = JsonSerializer.Serialize(values),
                        UpdatedColumns = string.Join(",", fieldsToUpdate),
                        UpdatedBy = updatedBy,
                        IpAddress = ipAddress,
                        UpdatedDate = now,
                        CreatedDate = now,
                    }, ct);

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update property {PropertyId} from Excel row", propertyId);
                    result.FailedCount++;
                    result.Errors.Add($"Property {propertyId}: {ex.Message}");
                    _unitOfWork.DiscardChanges();
                }
            }

            if (result.FailedCount > 0)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                result.Errors.Insert(0,
                    "Transaction rolled back — no properties were updated; all changes were reverted.");
                result.SuccessCount = 0;
            }
            else
            {
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return result;
    }

    /// <summary>
    /// Normalized composite key for matching an Excel row to a property by ward/property/partition
    /// (trimmed, case-insensitive; null treated as empty).
    /// </summary>
    private static string IdentityKey(string? wardNo, string? propertyNo, string? partitionNo) =>
        $"{(wardNo ?? string.Empty).Trim()}|{(propertyNo ?? string.Empty).Trim()}|{(partitionNo ?? string.Empty).Trim()}"
            .ToLowerInvariant();

    /// <summary>
    /// Runs the configured field validation (required / max length / regex) against a value set and
    /// returns the collected error messages. Shared by <see cref="BulkUpdateAsync"/> and the Excel import.
    /// </summary>
    private static List<string> ValidateFieldValues(
        IEnumerable<BulkUpdateFieldConfigDto> configs, IDictionary<string, object?> data)
    {
        var errors = new List<string>();
        foreach (var config in configs.Where(f => f.IsActive))
        {
            data.TryGetValue(config.FieldName, out var raw);
            var convertedValue = ConvertJsonElementToValue(raw);
            var value = convertedValue?.ToString();

            if (config.IsRequired && string.IsNullOrWhiteSpace(value))
                errors.Add($"{config.DisplayName} is required.");

            if (config.MaxLength.HasValue && value?.Length > config.MaxLength)
                errors.Add($"{config.DisplayName} exceeds max length of {config.MaxLength}.");

            if (!string.IsNullOrEmpty(config.ValidationRegex) && !string.IsNullOrWhiteSpace(value))
                if (!Regex.IsMatch(value, config.ValidationRegex))
                    errors.Add($"{config.DisplayName} has invalid format.");
        }
        return errors;
    }

    /// <summary>
    /// Writes a reflected value to an Excel cell with type-aware formatting (mirrors the export writer
    /// in ApartmentQCService). Null clears the cell.
    /// </summary>
    private static void SetCellValue(IXLCell cell, object? raw)
    {
        if (raw is null) { cell.Clear(); return; }

        switch (raw)
        {
            case string s: cell.Value = s; break;
            case bool b: cell.Value = b; break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.NumberFormat.Format = "yyyy-MM-dd";
                break;
            case decimal d: cell.Value = d; break;
            case double db: cell.Value = db; break;
            case float fl: cell.Value = fl; break;
            case int i: cell.Value = i; break;
            case long l: cell.Value = l; break;
            case short sh: cell.Value = sh; break;
            case byte by: cell.Value = by; break;
            default: cell.Value = raw.ToString(); break;
        }
    }

    /// <summary>
    /// Reads the current value of each field from an entity, keyed by field name (case-insensitive
    /// reflection, same as <see cref="PopulateCurrentValues"/>).
    /// </summary>
    private static Dictionary<string, object?> SnapshotFields(BaseEntity entity, List<string> fields)
    {
        var type = entity.GetType();
        var snapshot = new Dictionary<string, object?>();
        foreach (var field in fields)
        {
            var prop = type.GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            snapshot[field] = prop?.GetValue(entity);
        }
        return snapshot;
    }

    /// <summary>
    /// Applies the requested field values to an entity via reflection, coercing each input to the
    /// target property's CLR type.
    /// </summary>
    private static void ApplyFieldValues(BaseEntity entity, List<string> fields, Dictionary<string, object?> data)
    {
        var type = entity.GetType();
        foreach (var field in fields)
        {
            var prop = type.GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanWrite)
                continue;

            var raw = ConvertJsonElementToValue(data[field]);
            prop.SetValue(entity, CoerceToPropertyType(raw, prop.PropertyType));
        }
    }

    /// <summary>
    /// Converts a normalized input value to the CLR type of the target entity property (handles
    /// <see cref="Nullable{T}"/>, string→bool/DateTime/Guid, and numeric widening/narrowing).
    /// </summary>
    private static object? CoerceToPropertyType(object? value, Type targetType)
    {
        if (value is null)
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Already compatible (e.g. bool -> bool?, int -> int).
        if (underlying.IsInstanceOfType(value))
            return value;

        if (underlying == typeof(string))
            return value.ToString();

        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType
                    ? null
                    : Activator.CreateInstance(underlying);

            if (underlying == typeof(bool))
                return s.Trim() switch { "1" => true, "0" => false, _ => bool.Parse(s) };
            if (underlying == typeof(DateTime))
                return DateTime.Parse(s, CultureInfo.InvariantCulture);
            if (underlying == typeof(Guid))
                return Guid.Parse(s);

            return Convert.ChangeType(s, underlying, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
    }

    private static object? ConvertJsonElementToValue(object? value)
    {
        if (value is not JsonElement jsonElement)
            return value;

        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number => ResolveNumber(jsonElement),
            JsonValueKind.True => (object)true,
            JsonValueKind.False => (object)false,
            JsonValueKind.Null => null,
            _ => jsonElement.ToString()
        };
    }

    private static object ResolveNumber(JsonElement element)
    {
        if (element.TryGetInt32(out int intVal)) return intVal;
        if (element.TryGetInt64(out long longVal)) return longVal;
        if (element.TryGetDecimal(out decimal decimalVal)) return decimalVal;
        return element.GetDouble();
    }
}
