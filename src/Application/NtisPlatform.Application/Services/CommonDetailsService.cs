using System.Collections.Concurrent;
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
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.CommonDetails;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
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
    private readonly IRepository<BulkUpdateActivityEntity, int> _activityRepo;
    private readonly IRepository<PropertyEntity> _propertyRepo;
    private readonly IRepository<WardEntity> _wardRepo;
    private readonly IRepository<SocietyDetailsEntity> _societyRepo;
    private readonly IRepository<UserEntity> _userRepo;
    private readonly IRepository<SourceTableEntity> _sourceTableRepo;
    private readonly IRepository<SourceTableDetailsEntity> _sourceTableDetailsRepo;
    private readonly IRepository<ModuleMasterEntity> _moduleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDynamicEntityLoader _entityLoader;
    private readonly IPropertySearchService _propertySearchService;
    private readonly ILogger<CommonDetailsService> _logger;

    public CommonDetailsService(
        IRepository<BulkUpdateMasterEntity> masterRepo,
        IRepository<BulkUpdateFieldConfigEntity> fieldConfigRepo,
        IRepository<BulkUpdateHistoryEntity> historyRepo,
        IRepository<BulkUpdateActivityEntity, int> activityRepo,
        IRepository<PropertyEntity> propertyRepo,
        IRepository<WardEntity> wardRepo,
        IRepository<SocietyDetailsEntity> societyRepo,
        IRepository<UserEntity> userRepo,
        IRepository<SourceTableEntity> sourceTableRepo,
        IRepository<SourceTableDetailsEntity> sourceTableDetailsRepo,
        IRepository<ModuleMasterEntity> moduleRepo,
        IUnitOfWork unitOfWork,
        IDynamicEntityLoader entityLoader,
        IPropertySearchService propertySearchService,
        ILogger<CommonDetailsService> logger)
    {
        _masterRepo = masterRepo;
        _fieldConfigRepo = fieldConfigRepo;
        _historyRepo = historyRepo;
        _activityRepo = activityRepo;
        _propertyRepo = propertyRepo;
        _wardRepo = wardRepo;
        _societyRepo = societyRepo;
        _userRepo = userRepo;
        _sourceTableRepo = sourceTableRepo;
        _sourceTableDetailsRepo = sourceTableDetailsRepo;
        _moduleRepo = moduleRepo;
        _unitOfWork = unitOfWork;
        _entityLoader = entityLoader;
        _propertySearchService = propertySearchService;
        _logger = logger;
    }

    public async Task<List<BulkUpdateMasterDto>> GetMenuAsync(CancellationToken ct)
    {
        return await _masterRepo.GetQueryable()
            .Where(m => m.IsActive)
            .OrderBy(m => m.UpdateName)
            .Select(m => new BulkUpdateMasterDto
            {
                Id = m.Id,
                UpdateCode = m.UpdateCode,
                UpdateName = m.UpdateName,
                ReferenceTableName = m.ReferenceTableName,
                IsActive = m.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<List<SourceTableLookupDto>> GetSourceTablesAsync(CancellationToken ct)
    {
        var query = from st in _sourceTableRepo.GetQueryable()
                    where st.IsActive
                    join mm in _moduleRepo.GetQueryable() on st.ModuleId equals mm.Id into mmJoined
                    from mm in mmJoined.DefaultIfEmpty()
                    select new SourceTableLookupDto
                    {
                        Id = st.Id,
                        ModuleLabel = mm != null ? mm.ModuleLabel : null,
                        TableName = st.TableAliasName,
                        ReferenceTableName = st.TableName
                    };

        return await query.ToListAsync(ct);
    }

    public async Task<List<SourceTableFieldLookupDto>> GetSourceTableFieldsAsync(int sourceTableId, CancellationToken ct)
    {
        return await _sourceTableDetailsRepo.GetQueryable()
            .Where(std => std.SourceTableId == sourceTableId && std.IsActive)
            .Select(std => new SourceTableFieldLookupDto
            {
                Id = std.Id,
                TableFieldName = string.IsNullOrWhiteSpace(std.DisplayName) ? std.FieldName : std.DisplayName,
                FieldName = std.FieldName
            })
            .ToListAsync(ct);
    }

    public async Task<BulkUpdateDefinitionResultDto> CreateFromSourceTableAsync(
        CreateBulkUpdateDefinitionFromSourceDto request, int createdBy, CancellationToken ct)
    {
        var sourceTable = await _sourceTableRepo.GetQueryable()
            .FirstOrDefaultAsync(st => st.Id == request.TableId && st.IsActive, ct)
            ?? throw new ArgumentException($"Source table '{request.TableId}' not found or inactive.");

        var updateCode = Regex.Replace(request.UpdateName.Trim().ToUpperInvariant(), "[^A-Z0-9]+", "_").Trim('_');
        if (updateCode.Length == 0)
            throw new ArgumentException("UpdateName must contain at least one alphanumeric character.");
        if (updateCode.Length > 100)
            throw new ArgumentException($"Derived UpdateCode '{updateCode}' exceeds 100 characters.");

        if (await _masterRepo.GetQueryable().AnyAsync(m => m.UpdateCode == updateCode, ct))
            throw new ArgumentException($"A bulk update definition with code '{updateCode}' already exists.");

        var distinctIds = request.TableFieldIds.Distinct().ToList();
        var detailRows = await _sourceTableDetailsRepo.GetQueryable()
            .Where(d => distinctIds.Contains(d.Id) && d.SourceTableId == request.TableId)
            .ToListAsync(ct);

        var detailsById = detailRows.ToDictionary(d => d.Id);
        var missingIds = distinctIds.Where(id => !detailsById.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
            throw new ArgumentException(
                $"Source table field id(s) not found for table {request.TableId}: {string.Join(", ", missingIds)}.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var master = new BulkUpdateMasterEntity
            {
                UpdateCode = updateCode,
                UpdateName = request.UpdateName.Trim(),
                ReferenceTableName = sourceTable.TableName,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedDate = DateTime.Now
            };
            await _masterRepo.AddAsync(master, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var fieldConfigs = request.TableFieldIds.Distinct()
                .Select(id => detailsById[id])
                .Select(d =>
                {
                    var displayName = string.IsNullOrWhiteSpace(d.DisplayName) ? d.FieldName : d.DisplayName;
                    return new BulkUpdateFieldConfigEntity
                    {
                        BulkUpdateMasterId = master.Id,
                        FieldName = d.FieldName,
                        DisplayName = displayName,
                        // ControlType/DataType have no guaranteed value on SourceTableDetails; "text"/"string"
                        // are placeholder defaults — tune per field via BulkUpdateFieldConfig afterwards if needed.
                        ControlType = string.IsNullOrWhiteSpace(d.ControlType) ? "text" : d.ControlType,
                        DataType = string.IsNullOrWhiteSpace(d.DataType) ? "string" : d.DataType,
                        Placeholder = d.Placeholder,
                        IsRequired = d.IsRequired,
                        MaxLength = d.MaxLength,
                        ValidationRegex = d.ValidationRegex,
                        DefaultValue = d.DefaultValue,
                        SequenceNo = d.SequenceNo,
                        BindApi = d.BindApi,
                        ApiResponse = d.ApiResponse,
                        IsActive = true,
                        CreatedBy = createdBy,
                        CreatedDate = DateTime.Now
                    };
                })
                .ToList();

            await _fieldConfigRepo.AddRangeAsync(fieldConfigs, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            return new BulkUpdateDefinitionResultDto
            {
                Master = new BulkUpdateMasterDto
                {
                    Id = master.Id,
                    UpdateCode = master.UpdateCode,
                    UpdateName = master.UpdateName,
                    ReferenceTableName = master.ReferenceTableName,
                    IsActive = master.IsActive,
                    CreatedDate = master.CreatedDate,
                    UpdatedDate = master.UpdatedDate
                },
                FieldConfigs = fieldConfigs.Select(fc => new BulkUpdateFieldConfigDto
                {
                    Id = fc.Id,
                    BulkUpdateMasterId = fc.BulkUpdateMasterId,
                    FieldName = fc.FieldName,
                    DisplayName = fc.DisplayName,
                    ControlType = fc.ControlType,
                    DataType = fc.DataType,
                    Placeholder = fc.Placeholder,
                    IsRequired = fc.IsRequired,
                    MaxLength = fc.MaxLength,
                    ValidationRegex = fc.ValidationRegex,
                    DefaultValue = fc.DefaultValue,
                    SequenceNo = fc.SequenceNo,
                    BindApi = fc.BindApi,
                    ApiResponse = fc.ApiResponse,
                    IsActive = fc.IsActive,
                    CreatedDate = fc.CreatedDate,
                    UpdatedDate = fc.UpdatedDate
                }).ToList()
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
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
                ControlType = fc.ControlType,
                DataType = fc.DataType,
                Placeholder = fc.Placeholder,
                IsRequired = fc.IsRequired,
                MaxLength = fc.MaxLength,
                ValidationRegex = fc.ValidationRegex,
                DefaultValue = fc.DefaultValue,
                SequenceNo = fc.SequenceNo,
                IsActive = fc.IsActive,
                BindApi = fc.BindApi,
                ApiResponse = fc.ApiResponse
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
                Label = config.DisplayName ?? config.FieldName,
                LabelMarathi = string.Empty,
            });
        }
        return columns;
    }

    /// <summary>
    /// Resolves the bulk-update target table and its configured, whitelisted field names for a
    /// given <paramref name="updateCode"/> - shared by <see cref="FilterPropertiesAsync"/> and
    /// <see cref="FilterPropertiesByCategoryAsync"/>, which differ only in how they select the
    /// candidate properties, not in how they resolve/whitelist the preview columns.
    /// </summary>
    private async Task<(string TargetTable, List<string> SafeColumns)> ResolveUpdateCodeContextAsync(
        string updateCode, CancellationToken ct)
    {
        var master = await _masterRepo.GetQueryable()
            .FirstOrDefaultAsync(m => m.UpdateCode == updateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{updateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(updateCode, ct);
        var safeColumns = fieldConfigs.Select(f => f.FieldName).ToList();

        return (master.ReferenceTableName ?? string.Empty, safeColumns);
    }

    public async Task<PagedResult<PropertyPreviewDto>> FilterPropertiesAsync(
        FilterPropertiesRequestDto request, CancellationToken ct)
    {
        var updateCodes = request.UpdateCode
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (updateCodes.Count == 0)
            throw new ArgumentException("At least one UpdateCode is required.");

        var contexts = new List<(string TargetTable, List<string> SafeColumns)>();
        foreach (var code in updateCodes)
            contexts.Add(await ResolveUpdateCodeContextAsync(code, ct));

        var query = _propertyRepo.GetQueryable()
            .Where(pm => pm.WardId == request.WardId);

        // PropertyNo - exact match (optional)
        if (!string.IsNullOrWhiteSpace(request.PropertyNo))
        {
            var propertyNo = request.PropertyNo.Trim();
            query = query.Where(pm => pm.PropertyNo == propertyNo);
        }

        // FromPropertyNo and ToPropertyNo - range filtering (both required together)
        if (!string.IsNullOrWhiteSpace(request.FromPropertyNo) && !string.IsNullOrWhiteSpace(request.ToPropertyNo))
        {
            var fromPropertyNo = request.FromPropertyNo.Trim();
            var toPropertyNo = request.ToPropertyNo.Trim();

            if (long.TryParse(fromPropertyNo, out _) && long.TryParse(toPropertyNo, out _))
            {
                int fromLen = fromPropertyNo.Length;
                int toLen = toPropertyNo.Length;

                if (fromLen == toLen)
                {
                    query = query.Where(pm => pm.PropertyNo != null &&
                        pm.PropertyNo.Length == fromLen &&
                        string.Compare(pm.PropertyNo, fromPropertyNo) >= 0 &&
                        string.Compare(pm.PropertyNo, toPropertyNo) <= 0);
                }
                else
                {
                    query = query.Where(pm => pm.PropertyNo != null &&
                        pm.PropertyNo.Length >= fromLen &&
                        pm.PropertyNo.Length <= toLen &&
                        (pm.PropertyNo.Length > fromLen || string.Compare(pm.PropertyNo, fromPropertyNo) >= 0) &&
                        (pm.PropertyNo.Length < toLen || string.Compare(pm.PropertyNo, toPropertyNo) <= 0));
                }
            }
            else
            {
                query = query.Where(pm => string.Compare(pm.PropertyNo, fromPropertyNo) >= 0
                                       && string.Compare(pm.PropertyNo, toPropertyNo) <= 0);
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

        // Preload each distinct target table once, even if several UpdateCodes share one.
        var relatedEntitiesByTable = new Dictionary<string, Dictionary<int, object>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (targetTable, _) in contexts)
            if (!relatedEntitiesByTable.ContainsKey(targetTable))
                relatedEntitiesByTable[targetTable] = await LoadTargetEntitiesAsync(targetTable, propertyIds, ct);

        var items = pagedProperties.Select(x =>
        {
            var dto = new PropertyPreviewDto
            {
                Id = x.pm.Id,
                WardNo = x.WardNo,
                PropertyNo = x.pm.PropertyNo ?? string.Empty,
                PartitionNo = x.pm.PartitionNo ?? string.Empty,
            };
            foreach (var (targetTable, safeColumns) in contexts)
            {
                var isPropertyMast = BulkUpdateTargetRegistry.TryResolve(targetTable, out var previewTarget)
                    && BulkUpdateTargetRegistry.IsPropertyKeyedById(previewTarget);
                var source = isPropertyMast ? (object?)x.pm : relatedEntitiesByTable[targetTable].GetValueOrDefault(x.pm.Id);
                if (source != null)
                    PopulateCurrentValues(dto, source, safeColumns); // later UpdateCodes overwrite earlier ones on a shared field name
            }
            return dto;
        }).ToList();

        return new PagedResult<PropertyPreviewDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    /// <summary>
    /// Same preview/current-values behavior as <see cref="FilterPropertiesAsync"/>, but selects the
    /// candidate properties via the shared SearchCategory scoping model (Zone/Ward/Building/Range -
    /// see <see cref="PropertySearchByCategoryQueryParameters"/>) instead of the flat Ward+range
    /// filter - delegated entirely to <see cref="IPropertySearchService.SearchByCategoryAsync"/>,
    /// which owns SearchCategory validation (throws <see cref="Exceptions.PropertyValidationException"/>)
    /// and natural-sort pagination, the same way <c>LockUnlockService.GetPropertyLocksByCategoryAsync</c> does.
    /// </summary>
    public async Task<PagedResult<PropertyPreviewDto>> FilterPropertiesByCategoryAsync(
        FilterPropertiesByCategoryRequestDto request, CancellationToken ct)
    {
        var updateCodes = request.UpdateCode
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (updateCodes.Count == 0)
            throw new ArgumentException("At least one UpdateCode is required.");

        var contexts = new List<(string TargetTable, List<string> SafeColumns)>();
        foreach (var code in updateCodes)
            contexts.Add(await ResolveUpdateCodeContextAsync(code, ct));

        var searchResult = await _propertySearchService.SearchByCategoryAsync(request, ct);
        var propertyIds = searchResult.Items.Select(p => p.PropertyId).ToList();

        // When a target table IS PropertyMast, current values must come from the full PropertyEntity
        // row - PropertySearchByCategoryResponseDto only carries a narrow projection (Zone/Ward/
        // PartType/Category/etc.), not every bulk-update-configurable field. Unlike FilterPropertiesAsync
        // (whose own paged query already returns the full entity), this path has to load it separately,
        // keyed by the resolved PropertyIds. Preload each distinct target table only once, even if
        // several UpdateCodes share one.
        var propertyEntitiesById = new Dictionary<int, PropertyEntity>();
        var loadedPropertyMast = false;
        var relatedEntitiesByTable = new Dictionary<string, Dictionary<int, object>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (targetTable, _) in contexts)
        {
            var isPropertyMast = BulkUpdateTargetRegistry.TryResolve(targetTable, out var previewTarget)
                && BulkUpdateTargetRegistry.IsPropertyKeyedById(previewTarget);
            if (isPropertyMast)
            {
                if (!loadedPropertyMast)
                {
                    propertyEntitiesById = await _propertyRepo.GetQueryable()
                        .Where(pm => propertyIds.Contains(pm.Id))
                        .ToDictionaryAsync(pm => pm.Id, ct);
                    loadedPropertyMast = true;
                }
            }
            else if (!relatedEntitiesByTable.ContainsKey(targetTable))
            {
                relatedEntitiesByTable[targetTable] = await LoadTargetEntitiesAsync(targetTable, propertyIds, ct);
            }
        }

        var items = searchResult.Items.Select(p =>
        {
            var dto = new PropertyPreviewDto
            {
                Id = p.PropertyId,
                WardNo = p.WardNo ?? string.Empty,
                PropertyNo = p.PropertyNo ?? string.Empty,
                PartitionNo = p.PartitionNo ?? string.Empty,
            };
            foreach (var (targetTable, safeColumns) in contexts)
            {
                var isPropertyMast = BulkUpdateTargetRegistry.TryResolve(targetTable, out var previewTarget)
                    && BulkUpdateTargetRegistry.IsPropertyKeyedById(previewTarget);
                object? source = isPropertyMast
                    ? propertyEntitiesById.GetValueOrDefault(p.PropertyId)
                    : relatedEntitiesByTable[targetTable].GetValueOrDefault(p.PropertyId);
                if (source != null)
                    PopulateCurrentValues(dto, source, safeColumns); // later UpdateCodes overwrite earlier ones on a shared field name
            }
            return dto;
        }).ToList();

        return new PagedResult<PropertyPreviewDto>(items, searchResult.TotalCount, searchResult.PageNumber, searchResult.PageSize);
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

    /// <summary>
    /// Inserts the BulkUpdateActivity row up front, before the properties transaction begins, and
    /// commits it immediately via its own SaveChangesAsync call (no explicit transaction wraps it).
    /// This ordering is required by FK_BulkUpdateHistory_BulkUpdateActivity - History rows written
    /// inside the properties transaction reference this row's Id, so the parent row must already
    /// exist in the database before that transaction starts. Starts pessimistically as "Failed" so a
    /// process crash between here and <see cref="FinalizeActivityAsync"/> still leaves a correct record.
    /// </summary>
    private async Task<BulkUpdateActivityEntity> BeginActivityAsync(
        string activityType, string? updateName, int updatedBy,
        string? ipAddress, string? remarks, int records, DateTime startTime, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(updatedBy, ct);
        var activity = new BulkUpdateActivityEntity
        {
            ActivityType = activityType,
            ActivityStatus = "Failed",
            DateAndTime = startTime,
            Records = records,
            IPAddress = ipAddress,
            Remarks = remarks,
            UpdateName = updateName,
            DoneBy = user?.UserName,
            StartTime = startTime,
        };
        await _activityRepo.AddAsync(activity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return activity;
    }

    /// <summary>
    /// Updates the previously-inserted activity row with its outcome. Uses UpdateAsync (not the
    /// tracked instance directly) because the properties transaction's per-property catch may have
    /// already called DiscardChanges() (ChangeTracker.Clear()), detaching <paramref name="activity"/>.
    /// </summary>
    private async Task FinalizeActivityAsync(
        BulkUpdateActivityEntity activity, bool success, string activityRemark, DateTime endTime, CancellationToken ct)
    {
        try
        {
            activity.ActivityStatus = success ? "Success" : "Failed";
            activity.ActivityRemark = activityRemark;
            activity.EndTime = endTime;
            activity.Duration = (int)Math.Round((endTime - (activity.StartTime ?? endTime)).TotalSeconds);
            await _activityRepo.UpdateAsync(activity, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to finalize BulkUpdateActivity {ActivityId}", activity.Id);
        }
    }

    private static string FormatActivityRemark(string updateCode, string message) => $"[{updateCode}] {message}";

    public async Task<BulkUpdateResultDto> BulkUpdateAsync(
        BulkUpdateRequestDto request, int updatedBy, string? ipAddress, CancellationToken ct)
    {
        var master = await _masterRepo.GetQueryable()
            .FirstOrDefaultAsync(m => m.UpdateCode == request.UpdateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{request.UpdateCode}' not found.");

        // Activity is created up front (before validation) so that even a request rejected by
        // validation still leaves a Failed BulkUpdateActivity row with the rejection reason as
        // ActivityRemark - "record every attempt" isn't limited to failures inside the transaction.
        var propertyIds = request.PropertyIds;
        var startTime = DateTime.Now;
        var activity = await BeginActivityAsync("Screen", master.UpdateName, updatedBy,
            ipAddress, request.Remarks, propertyIds.Count, startTime, ct);

        try
        {
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
        if (!BulkUpdateTargetRegistry.TryResolve(master.ReferenceTableName ?? string.Empty, out var target))
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

        var result = new BulkUpdateResultDto { UpdateCode = request.UpdateCode, TotalRequested = propertyIds.Count };
        var hadFailures = false;

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

                    var targetEntity = targets.FirstOrDefault();
                    var perPropertyErrors = ValidateFieldValues(fieldConfigs, updateData, targetEntity);
                    if (perPropertyErrors.Count > 0)
                    {
                        throw new ArgumentException($"Property {propertyId}: {string.Join("; ", perPropertyErrors)}");
                    }

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
                        ActivityId = activity.Id,
                        BulkUpdateMasterId = master.Id,
                        PropertyId = (int)propertyId,
                        OldValue = oldValue,
                        NewValue = newValueJson,
                        UpdatedColumns = updatedColumnsStr,
                        UpdatedBy = updatedBy,
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
                hadFailures = true;
                await _unitOfWork.RollbackTransactionAsync(ct);
                // Properties that succeeded before the failure left their BulkUpdateHistoryEntity rows
                // tracked as Added (rollback only undoes the DB transaction, not the change tracker) -
                // discard them so the FinalizeActivityAsync save below doesn't resurrect and persist them.
                _unitOfWork.DiscardChanges();
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
            _unitOfWork.DiscardChanges();
            throw;
        }

        var remark = FormatActivityRemark(request.UpdateCode, hadFailures
            ? string.Join("; ", result.Errors)
            : $"Updated {result.SuccessCount} of {result.TotalRequested} propert{(result.TotalRequested == 1 ? "y" : "ies")} successfully.");
        await FinalizeActivityAsync(activity, !hadFailures, remark, DateTime.Now, ct);
        return result;
        }
        catch (Exception ex)
        {
            await FinalizeActivityAsync(activity, false, FormatActivityRemark(request.UpdateCode, ex.Message), DateTime.Now, ct);
            throw;
        }
    }

    public async Task<List<BulkUpdateResultDto>> BulkUpdateBatchAsync(
        List<BulkUpdateRequestDto> requests, int updatedBy, string? ipAddress, CancellationToken ct)
    {
        var results = new List<BulkUpdateResultDto>(requests.Count);
        foreach (var request in requests)
        {
            try
            {
                results.Add(await BulkUpdateAsync(request, updatedBy, ipAddress, ct));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk update batch item failed for UpdateCode {UpdateCode}", request.UpdateCode);
                results.Add(new BulkUpdateResultDto
                {
                    UpdateCode = request.UpdateCode,
                    TotalRequested = request.PropertyIds.Count,
                    SuccessCount = 0,
                    FailedCount = request.PropertyIds.Count,
                    Errors = [ex.Message]
                });
            }
        }
        return results;
    }

    public async Task<byte[]> ExportPropertiesToExcelAsync(ExportPropertiesRequestDto request, CancellationToken ct)
    {
        var (_, safeColumns) = await ResolveUpdateCodeContextAsync(request.UpdateCode, ct);
        var fieldConfigs = await GetFormFieldsAsync(request.UpdateCode, ct);
        var fieldConfigMap = fieldConfigs.ToDictionary(f => f.FieldName, StringComparer.OrdinalIgnoreCase);

        // Build header labels using DisplayName (or stripped FieldName)
        var headerLabels = safeColumns.Select(col =>
        {
            if (fieldConfigMap.TryGetValue(col, out var cfg) && !string.IsNullOrWhiteSpace(cfg.DisplayName))
                return cfg.DisplayName.Trim();
            return col.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && col.Length > 2 ? col[..^2] : col;
        }).ToList();

        var headers = new List<string> { "wardNo", "propertyNo", "partitionNo" };
        headers.AddRange(headerLabels);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Properties");

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = worksheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
        }

        // No WardId supplied -> deliberate header-only export; skip the property query entirely.
        if (request.WardId.HasValue)
        {
            var filterRequest = new FilterPropertiesRequestDto
            {
                WardId = request.WardId.Value,
                FromPropertyNo = request.FromPropertyNo,
                ToPropertyNo = request.ToPropertyNo,
                PropertyNo = request.PropertyNo,
                Wing = request.Wing,
                UpdateCode = [request.UpdateCode],
                PageNumber = 1,
                PageSize = -1
            };
            var paged = await FilterPropertiesAsync(filterRequest, ct);

            var rows = paged.Items.ToList();
            var lookupMaps = await GetMasterLookupMapsAsync(fieldConfigs, ct);

            for (var r = 0; r < rows.Count; r++)
            {
                var item = rows[r];
                worksheet.Cell(r + 2, 1).Value = item.WardNo;
                worksheet.Cell(r + 2, 2).Value = item.PropertyNo;
                worksheet.Cell(r + 2, 3).Value = item.PartitionNo;
                for (var f = 0; f < safeColumns.Count; f++)
                {
                    var colName = safeColumns[f];
                    var rawVal = item.CurrentValues.GetValueOrDefault(colName);
                    object? cellVal = rawVal;

                    if (rawVal != null && lookupMaps.TryGetValue(colName, out var mapInfo) && TryExtractLong(rawVal, out long rawId))
                    {
                        if (mapInfo.IdToNameMap.TryGetValue(rawId, out var desc))
                        {
                            cellVal = desc;
                        }
                    }

                    SetCellValue(worksheet.Cell(r + 2, 4 + f), cellVal);
                }
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Resolves the value columns present in an uploaded sheet and matches every wardNo/propertyNo/
    /// partitionNo identity to at most one candidate <c>PropertyMast</c> row, keyed by <see cref="IdentityKey"/>.
    /// Shared by <see cref="ImportPropertiesFromExcelAsync"/> (which applies updates for matched rows) and
    /// <see cref="ValidateImportExcelAsync"/> (which only reports problems) - both need the identical
    /// column-resolution and candidate-matching logic so a row flagged as clean/invalid means the same
    /// thing in both places.
    /// </summary>
    private async Task<(List<BulkUpdateFieldConfigDto> PresentConfigs, List<string> ValueFieldNames,
        Dictionary<string, List<long>> IdsByKey)> PrepareExcelMatchContextAsync(
        BulkUpdateTarget target, List<BulkUpdateFieldConfigDto> fieldConfigs,
        List<string> headers, List<ExcelRow> excelRows, CancellationToken ct)
    {
        var headerSet = new HashSet<string>(
            headers.Where(h => !string.IsNullOrWhiteSpace(h)), StringComparer.OrdinalIgnoreCase);

        // Identity columns must be present so each row can be matched to a property.
        var missingIdentity = new[] { "wardNo", "propertyNo", "partitionNo" }
            .Where(h => !headerSet.Contains(h))
            .ToList();
        if (missingIdentity.Count > 0)
            throw new ArgumentException($"Missing required column(s): {string.Join(", ", missingIdentity)}.");

        // Value columns = configured fields whose header appears in the sheet (flexible matching by DisplayName, FieldName, or stripped FieldName).
        var presentConfigs = new List<BulkUpdateFieldConfigDto>();
        foreach (var cfg in fieldConfigs)
        {
            var matchedHeader = headers.FirstOrDefault(h =>
                string.Equals(h, cfg.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(h, cfg.FieldName, StringComparison.OrdinalIgnoreCase) ||
                (cfg.FieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(h, cfg.FieldName[..^2], StringComparison.OrdinalIgnoreCase)));

            if (matchedHeader != null)
            {
                presentConfigs.Add(cfg);
            }
        }

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

        // Filter by PartitionNo when the Excel data has partition numbers, but keep blank-partition
        // ("Main Property") candidates in scope too - the file may legitimately mix Main and
        // Partition rows in one upload.
        if (partitionNos.Count > 0)
            candidatesQuery = candidatesQuery.Where(x =>
                string.IsNullOrWhiteSpace(x.PartitionNo) || partitionNos.Contains(x.PartitionNo));

        var candidates = await candidatesQuery.ToListAsync(ct);

        var idsByKey = candidates
            .GroupBy(x => IdentityKey(x.WardNo, x.PropertyNo, x.PartitionNo))
            .ToDictionary(g => g.Key, g => g.Select(x => Convert.ToInt64(x.Id)).ToList());

        return (presentConfigs, valueFieldNames, idsByKey);
    }

    public async Task<BulkUpdateResultDto> ImportPropertiesFromExcelAsync(string updateCode, Stream fileStream, int updatedBy, string? ipAddress, string? remarks, CancellationToken ct)
    {
        var master = await _masterRepo.GetQueryable()
            .FirstOrDefaultAsync(m => m.UpdateCode == updateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{updateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(updateCode, ct);

        if (!BulkUpdateTargetRegistry.TryResolve(master.ReferenceTableName ?? string.Empty, out var target))
            throw new InvalidOperationException(
                $"Update type '{updateCode}' references an unrecognized table '{master.ReferenceTableName}'. " +
                "Add it to BulkUpdateTargetRegistry if this table is intentionally supported.");

        var (headers, excelRows) = ExcelImportHelper.Read(fileStream);
        var startTime = DateTime.Now;
        var activity = await BeginActivityAsync("Excel", master.UpdateName, updatedBy,
            ipAddress, remarks, excelRows.Count, startTime, ct);

        try
        {
        var (presentConfigs, valueFieldNames, idsByKey) =
            await PrepareExcelMatchContextAsync(target, fieldConfigs, headers, excelRows, ct);

        var lookupMaps = await GetMasterLookupMapsAsync(presentConfigs, ct);
        var result = new BulkUpdateResultDto { UpdateCode = updateCode, TotalRequested = excelRows.Count };

        // Preload existing entities for DB validation fallback (e.g. AssessmentYear vs ConstructionYear)
        var allPropertyIds = idsByKey.Values.SelectMany(x => x).Distinct().ToList();
        var existingEntitiesMap = new Dictionary<long, BaseEntity>();
        if (allPropertyIds.Count > 0)
        {
            var loadedTargets = await _entityLoader.LoadByKeyAsync(
                target.EntityType, target.KeyProperty, allPropertyIds, asNoTracking: true, ct);
            var keyProp = target.EntityType.GetProperty(
                target.KeyProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (keyProp != null)
            {
                foreach (var e in loadedTargets)
                {
                    var val = keyProp.GetValue(e);
                    if (val != null && long.TryParse(val.ToString(), out var pid))
                        existingEntitiesMap.TryAdd(pid, e);
                }
            }
        }

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

            BaseEntity? existingEntity = null;
            if (ids.Count == 1)
            {
                existingEntitiesMap.TryGetValue(ids[0], out existingEntity);
            }

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var lookupErrors = ProcessAndValidateLookupRowValues(presentConfigs, lookupMaps, row, values);
            if (lookupErrors.Count > 0)
            {
                errors.AddRange(lookupErrors.Select(e => $"Row {row.RowNumber}: {e}"));
                continue;
            }

            var rowErrors = ValidateFieldValues(presentConfigs, values, existingEntity);
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
            await FinalizeActivityAsync(activity, false, FormatActivityRemark(updateCode, string.Join("; ", result.Errors)), DateTime.Now, ct);
            return result;
        }

        await _unitOfWork.BeginTransactionAsync(ct);
        var hadFailures = false;
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
                        ActivityId = activity.Id,
                        BulkUpdateMasterId = master.Id,
                        PropertyId = (int)propertyId,
                        OldValue = oldValue,
                        NewValue = JsonSerializer.Serialize(values),
                        UpdatedColumns = string.Join(",", fieldsToUpdate),
                        UpdatedBy = updatedBy,
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
                hadFailures = true;
                await _unitOfWork.RollbackTransactionAsync(ct);
                _unitOfWork.DiscardChanges();
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
            _unitOfWork.DiscardChanges();
            throw;
        }

        var remark = FormatActivityRemark(updateCode, hadFailures
            ? string.Join("; ", result.Errors)
            : $"Updated {result.SuccessCount} of {result.TotalRequested} propert{(result.TotalRequested == 1 ? "y" : "ies")} successfully.");
        await FinalizeActivityAsync(activity, !hadFailures, remark, DateTime.Now, ct);
        return result;
        }
        catch (Exception ex)
        {
            await FinalizeActivityAsync(activity, false, FormatActivityRemark(updateCode, ex.Message), DateTime.Now, ct);
            throw;
        }
    }

    /// <summary>
    /// Dry-run counterpart to <see cref="ImportPropertiesFromExcelAsync"/>: runs the identical
    /// identity-matching and field-validation checks against the uploaded sheet, but never touches the
    /// database - no property updates, no BulkUpdateHistory/BulkUpdateActivity rows. Returns only the
    /// rows that would fail, each carrying a ValidationRemark explaining why.
    /// </summary>
    public async Task<ExcelValidationResultDto> ValidateImportExcelAsync(string updateCode, Stream fileStream, CancellationToken ct)
    {
        var master = await _masterRepo.GetQueryable()
            .FirstOrDefaultAsync(m => m.UpdateCode == updateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{updateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(updateCode, ct);

        if (!BulkUpdateTargetRegistry.TryResolve(master.ReferenceTableName ?? string.Empty, out var target))
            throw new InvalidOperationException(
                $"Update type '{updateCode}' references an unrecognized table '{master.ReferenceTableName}'. " +
                "Add it to BulkUpdateTargetRegistry if this table is intentionally supported.");

        var (headers, excelRows) = ExcelImportHelper.Read(fileStream);

        var (presentConfigs, valueFieldNames, idsByKey) =
            await PrepareExcelMatchContextAsync(target, fieldConfigs, headers, excelRows, ct);

        var lookupMaps = await GetMasterLookupMapsAsync(presentConfigs, ct);
        var flaggedRows = new List<(ExcelRow Row, List<string> Issues, Dictionary<string, object?> ResolvedValues)>();

        // Preload existing entities for DB validation fallback (e.g. AssessmentYear vs ConstructionYear)
        var allPropertyIds = idsByKey.Values.SelectMany(x => x).Distinct().ToList();
        var existingEntitiesMap = new Dictionary<long, BaseEntity>();
        if (allPropertyIds.Count > 0)
        {
            var loadedTargets = await _entityLoader.LoadByKeyAsync(
                target.EntityType, target.KeyProperty, allPropertyIds, asNoTracking: true, ct);
            var keyProp = target.EntityType.GetProperty(
                target.KeyProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (keyProp != null)
            {
                foreach (var e in loadedTargets)
                {
                    var val = keyProp.GetValue(e);
                    if (val != null && long.TryParse(val.ToString(), out var pid))
                        existingEntitiesMap.TryAdd(pid, e);
                }
            }
        }

        foreach (var row in excelRows)
        {
            var wardNo = row.Cells.GetValueOrDefault("wardNo")?.Trim();
            var propertyNo = row.Cells.GetValueOrDefault("propertyNo")?.Trim();
            var partitionNo = row.Cells.GetValueOrDefault("partitionNo")?.Trim();
            var issues = new List<string>();

            List<long>? ids = null;
            if (string.IsNullOrWhiteSpace(wardNo) || string.IsNullOrWhiteSpace(propertyNo))
            {
                issues.Add("wardNo and propertyNo are required.");
            }
            else
            {
                var key = IdentityKey(wardNo, propertyNo, partitionNo);
                if (!idsByKey.TryGetValue(key, out ids))
                    issues.Add($"No property found for wardNo='{wardNo}', propertyNo='{propertyNo}', partitionNo='{partitionNo}'.");
                else if (ids.Count > 1)
                    issues.Add($"Multiple properties match wardNo='{wardNo}', propertyNo='{propertyNo}', partitionNo='{partitionNo}'.");
            }

            BaseEntity? existingEntity = null;
            if (ids != null && ids.Count == 1)
            {
                existingEntitiesMap.TryGetValue(ids[0], out existingEntity);
            }

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            issues.AddRange(ProcessAndValidateLookupRowValues(presentConfigs, lookupMaps, row, values));
            issues.AddRange(ValidateFieldValues(presentConfigs, values, existingEntity));

            if (issues.Count > 0)
                flaggedRows.Add((row, issues, values));
        }

        var columns = new List<string> { "wardNo", "propertyNo", "partitionNo" };
        foreach (var cfg in presentConfigs)
        {
            var label = !string.IsNullOrWhiteSpace(cfg.DisplayName) ? cfg.DisplayName.Trim() : cfg.FieldName;
            columns.Add(label);
        }
        columns.Add("ValidationRemark");

        var rows = flaggedRows.Select(fr =>
        {
            var (row, issues, _) = fr;
            var rowData = new Dictionary<string, object?>
            {
                ["wardNo"] = row.Cells.GetValueOrDefault("wardNo"),
                ["propertyNo"] = row.Cells.GetValueOrDefault("propertyNo"),
                ["partitionNo"] = row.Cells.GetValueOrDefault("partitionNo"),
            };
            foreach (var cfg in presentConfigs)
            {
                var label = !string.IsNullOrWhiteSpace(cfg.DisplayName) ? cfg.DisplayName.Trim() : cfg.FieldName;
                rowData[label] = GetCellValueFromRow(row, cfg);
            }
            rowData["ValidationRemark"] = string.Join("; ", issues);
            return rowData;
        }).ToList();

        return new ExcelValidationResultDto
        {
            Columns = columns,
            Rows = rows,
            TotalRows = excelRows.Count,
            FlaggedRowCount = flaggedRows.Count
        };
    }

    private static string? GetCellValueFromRow(ExcelRow row, BulkUpdateFieldConfigDto cfg)
    {
        if (row.Cells.TryGetValue(cfg.FieldName, out var v1) && !string.IsNullOrWhiteSpace(v1))
            return v1;
        if (!string.IsNullOrWhiteSpace(cfg.DisplayName) && row.Cells.TryGetValue(cfg.DisplayName, out var v2) && !string.IsNullOrWhiteSpace(v2))
            return v2;
        if (cfg.FieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && cfg.FieldName.Length > 2)
        {
            var stripped = cfg.FieldName[..^2];
            if (row.Cells.TryGetValue(stripped, out var v3) && !string.IsNullOrWhiteSpace(v3))
                return v3;
        }
        return row.Cells.GetValueOrDefault(cfg.FieldName);
    }

    private class MasterLookupMapInfo
    {
        public Dictionary<string, long> NameToIdMap { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<long, string> IdToNameMap { get; } = new();
        public List<string> ValidNames { get; } = new();
    }

    private async Task<Dictionary<string, MasterLookupMapInfo>> GetMasterLookupMapsAsync(
        List<BulkUpdateFieldConfigDto> configs, CancellationToken ct)
    {
        var result = new Dictionary<string, MasterLookupMapInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.BindApi))
                continue;

            var entityType = FindEntityTypeFromBindApi(config.BindApi);
            if (entityType == null)
                continue;

            var (_, valPropName) = ParseApiResponse(config.ApiResponse);
            var mapInfo = new MasterLookupMapInfo();

            try
            {
                var entities = await _entityLoader.LoadAllAsync(
                    entityType, asNoTracking: true, ct);

                foreach (var entity in entities)
                {
                    var id = entity.Id;
                    var name = GetDisplayValueForEntity(entity, valPropName);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        var trimmed = name.Trim();
                        mapInfo.NameToIdMap[trimmed] = id;
                        mapInfo.IdToNameMap[id] = trimmed;
                        if (!mapInfo.ValidNames.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                        {
                            mapInfo.ValidNames.Add(trimmed);
                        }
                    }
                }

                result[config.FieldName] = mapInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build lookup map for field '{FieldName}' (BindApi: '{BindApi}').", config.FieldName, config.BindApi);
            }
        }

        return result;
    }

    private static List<string> ProcessAndValidateLookupRowValues(
        List<BulkUpdateFieldConfigDto> presentConfigs,
        Dictionary<string, MasterLookupMapInfo> lookupMaps,
        ExcelRow row,
        Dictionary<string, object?> targetValues)
    {
        var issues = new List<string>();

        foreach (var config in presentConfigs)
        {
            var field = config.FieldName;
            var rawVal = GetCellValueFromRow(row, config);
            var label = !string.IsNullOrWhiteSpace(config.DisplayName) ? config.DisplayName.Trim() : field;

            if (lookupMaps.TryGetValue(field, out var mapInfo))
            {
                if (string.IsNullOrWhiteSpace(rawVal))
                {
                    if (config.IsRequired)
                    {
                        issues.Add($"{label} is required.");
                    }
                    else
                    {
                        targetValues[field] = null;
                    }
                    continue;
                }

                var valStr = rawVal.Trim();

                if (mapInfo.NameToIdMap.TryGetValue(valStr, out long resolvedId))
                {
                    targetValues[field] = resolvedId;
                }
                else if (TryExtractLong(rawVal, out long numericId) && mapInfo.IdToNameMap.ContainsKey(numericId))
                {
                    targetValues[field] = numericId;
                }
                else
                {
                    var optionsText = mapInfo.ValidNames.Count > 0
                        ? $" Valid options: {string.Join(", ", mapInfo.ValidNames.Take(3))}{(mapInfo.ValidNames.Count > 5 ? "..." : "")}."
                        : "";
                    issues.Add($"Invalid {label} '{valStr}'.{optionsText}");
                }
            }
            else
            {
                targetValues[field] = rawVal;
            }
        }

        return issues;
    }

    private static bool TryExtractLong(object? val, out long id)
    {
        id = 0;
        if (val == null) return false;

        if (val is long l) { id = l; return true; }
        if (val is int i) { id = i; return true; }
        if (val is short s) { id = s; return true; }
        if (val is byte b) { id = b; return true; }

        if (val is JsonElement elem)
        {
            return TryExtractId(elem, out id);
        }

        return long.TryParse(val.ToString(), out id);
    }

    public async Task<PagedResult<UpdateHistoryDto>> GetUpdateHistoryAsync(
        UpdateHistoryQueryParameters request, CancellationToken ct)
    {
        var query =
            from h in _historyRepo.GetQueryable()
            join m in _masterRepo.GetQueryable() on h.BulkUpdateMasterId equals m.Id into mj
            from m in mj.DefaultIfEmpty()
            join pm in _propertyRepo.GetQueryable() on h.PropertyId equals pm.Id into pmj
            from pm in pmj.DefaultIfEmpty()
            join w in _wardRepo.GetQueryable()
                on (pm != null ? (int?)pm.WardId : null) equals (int?)w.Id into wj
            from w in wj.DefaultIfEmpty()
            join u in _userRepo.GetQueryable()
                on(h.CreatedBy ?? h.UpdatedBy) equals(int ?)u.Id into uj
            from u in uj.DefaultIfEmpty()
            join a in _activityRepo.GetQueryable() on h.ActivityId equals a.Id into aj
            from a in aj.DefaultIfEmpty()
            select new { h, m, pm, w, u, a };

        if (request.Id.HasValue)
            query = query.Where(x => x.h.Id == request.Id.Value);
        if (request.ActivityId.HasValue)
            query = query.Where(x => x.h.ActivityId == request.ActivityId.Value);
        if (!string.IsNullOrWhiteSpace(request.UpdateName))
            query = query.Where(x => x.m != null && x.m.UpdateName == request.UpdateName);
        if (!string.IsNullOrWhiteSpace(request.WardNo))
            query = query.Where(x => x.w != null && x.w.WardNo == request.WardNo);
        if (!string.IsNullOrWhiteSpace(request.PropertyNo))
            query = query.Where(x => x.pm != null && x.pm.PropertyNo == request.PropertyNo);
        if (!string.IsNullOrWhiteSpace(request.PartitionNo))
            query = query.Where(x => x.pm != null && x.pm.PartitionNo == request.PartitionNo);
        if (!string.IsNullOrWhiteSpace(request.Property))
        {
            var propertyTerm = request.Property.Trim();
            query = query.Where(x =>
                (((x.w != null ? x.w.WardNo : null) ?? "") + "-" +
                 ((x.pm != null ? x.pm.PropertyNo : null) ?? "") + "-" +
                 ((x.pm != null ? x.pm.PartitionNo : null) ?? "")).Contains(propertyTerm));
        }
        if (!string.IsNullOrWhiteSpace(request.UpdatedColumns))
            query = query.Where(x => x.h.UpdatedColumns != null && x.h.UpdatedColumns.Contains(request.UpdatedColumns));
        if (request.IsActive.HasValue)
            query = query.Where(x => x.h.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.DoneBy))
            query = query.Where(x => x.u != null && x.u.UserName == request.DoneBy);
        if (!string.IsNullOrWhiteSpace(request.ActivityType))
            query = query.Where(x => x.a != null && x.a.ActivityType == request.ActivityType);
        if (!string.IsNullOrWhiteSpace(request.ActivityStatus))
            query = query.Where(x => x.a != null && x.a.ActivityStatus == request.ActivityStatus);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(x =>
                (((x.w != null ? x.w.WardNo : null) ?? "") + "-" +
                 ((x.pm != null ? x.pm.PropertyNo : null) ?? "") + "-" +
                 ((x.pm != null ? x.pm.PartitionNo : null) ?? "")).Contains(term)
                || (x.m != null && x.m.UpdateName != null && x.m.UpdateName.Contains(term))
                || (x.h.UpdatedColumns != null && x.h.UpdatedColumns.Contains(term))
                || (x.a != null && x.a.Remarks != null && x.a.Remarks.Contains(term))
                || (x.a != null && x.a.ActivityRemark != null && x.a.ActivityRemark.Contains(term))
                || (x.u != null && x.u.UserName != null && x.u.UserName.Contains(term)));
        }

        query = query.OrderByDescending(x => x.h.CreatedDate ?? x.h.UpdatedDate).ThenByDescending(x => x.h.Id);

        var totalCount = await query.CountAsync(ct);

        int pageNumber;
        int pageSize;
        if (request.PageSize == -1)
        {
            pageNumber = 1;
            pageSize = totalCount > 0 ? totalCount : 1;
        }
        else
        {
            pageNumber = request.PageNumber;
            pageSize = request.PageSize;
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        var items = await query
            .Select(x => new UpdateHistoryDto
            {
                Id = x.h.Id,
                PropertyId = x.h.PropertyId,
                UpdateName = x.m != null ? x.m.UpdateName : null,
                WardNo = x.w != null ? x.w.WardNo : null,
                PropertyNo = x.pm != null ? x.pm.PropertyNo : null,
                PartitionNo = x.pm != null ? x.pm.PartitionNo : null,
                OldValue = x.h.OldValue,
                NewValue = x.h.NewValue,
                UpdatedColumns = x.h.UpdatedColumns,
                IsActive = x.h.IsActive,
                Remarks = x.a != null ? x.a.Remarks : null,
                IPAddress = x.a != null ? x.a.IPAddress : null,
                DoneBy = x.u != null ? x.u.UserName : null,
                CreatedDate = x.h.CreatedDate ?? x.h.UpdatedDate,
                ActivityId = x.h.ActivityId,
                ActivityType = x.a != null ? x.a.ActivityType : null,
                ActivityStatus = x.a != null ? x.a.ActivityStatus : null,
                ActivityDoneBy = x.a != null ? x.a.DoneBy : null,
                Records = x.a != null ? x.a.Records : null,
                StartTime = x.a != null ? x.a.StartTime : null,
                EndTime = x.a != null ? x.a.EndTime : null,
                Duration = x.a != null ? x.a.Duration : null,
                ActivityRemark = x.a != null ? x.a.ActivityRemark : null
            })
            .ToListAsync(ct);

        foreach (var item in items)
            item.Property = string.Join("-", new[] { item.WardNo, item.PropertyNo, item.PartitionNo }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        await EnrichUpdateHistoryItemsAsync(items, ct);

        return new PagedResult<UpdateHistoryDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<byte[]> ExportUpdateHistoryToExcelAsync(
        UpdateHistoryQueryParameters request, CancellationToken ct)
    {
        // Export the full filtered set (ignore paging).
        var unpagedRequest = new UpdateHistoryQueryParameters
        {
            Id = request.Id,
            ActivityId = request.ActivityId,
            UpdateName = request.UpdateName,
            WardNo = request.WardNo,
            PropertyNo = request.PropertyNo,
            PartitionNo = request.PartitionNo,
            Property = request.Property,
            UpdatedColumns = request.UpdatedColumns,
            IsActive = request.IsActive,
            DoneBy = request.DoneBy,
            ActivityType = request.ActivityType,
            ActivityStatus = request.ActivityStatus,
            SearchTerm = request.SearchTerm,
            PageNumber = 1,
            PageSize = -1
        };
        var paged = await GetUpdateHistoryAsync(unpagedRequest, ct);

        var headers = new[]
        {
            "Id", "UpdateName", "PropertyId", "WardNo", "PropertyNo", "PartitionNo", "Property",
            "OldValue", "NewValue", "UpdatedColumns", "IsActive", "Remarks", "IPAddress", "DoneBy", "CreatedDate",
            "ActivityId", "ActivityType", "ActivityStatus", "ActivityDoneBy", "Records", "StartTime", "EndTime", "Duration",
            "ActivityRemark"
        };

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("UpdateHistory");

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = worksheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
        }

        var rows = paged.Items.ToList();
        for (var r = 0; r < rows.Count; r++)
        {
            var item = rows[r];
            object?[] values =
            {
                item.Id, item.UpdateName, item.PropertyId, item.WardNo, item.PropertyNo, item.PartitionNo, item.Property,
                item.OldValue, item.NewValue, item.UpdatedColumns, item.IsActive, item.Remarks,
                item.IPAddress, item.DoneBy, item.CreatedDate,
                item.ActivityId, item.ActivityType, item.ActivityStatus, item.ActivityDoneBy,
                item.Records, item.StartTime, item.EndTime, item.Duration,
                item.ActivityRemark
            };
            for (var c = 0; c < values.Length; c++)
            {
                var v = values[c] is string s && s.Length > 0 && "=+-@".Contains(s[0]) ? "'" + s : values[c];
                SetCellValue(worksheet.Cell(r + 2, c + 1), v);
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<PagedResult<UpdateActivityDto>> GetUpdateActivityAsync(
        UpdateActivityQueryParameters request, CancellationToken ct)
    {
        var query = _activityRepo.GetQueryable();

        if (request.Id.HasValue)
            query = query.Where(a => a.Id == request.Id.Value);
        if (!string.IsNullOrWhiteSpace(request.ActivityType))
            query = query.Where(a => a.ActivityType == request.ActivityType);
        if (!string.IsNullOrWhiteSpace(request.ActivityStatus))
            query = query.Where(a => a.ActivityStatus == request.ActivityStatus);
        if (request.CreatedDateFrom.HasValue)
            query = query.Where(a => a.DateAndTime >= request.CreatedDateFrom.Value);
        if (request.CreatedDateTo.HasValue)
            query = query.Where(a => a.DateAndTime <= request.CreatedDateTo.Value);
        if (!string.IsNullOrWhiteSpace(request.DoneBy))
            query = query.Where(a => a.DoneBy == request.DoneBy);
        if (!string.IsNullOrWhiteSpace(request.Remarks))
            query = query.Where(a => a.Remarks != null && a.Remarks.Contains(request.Remarks));
        if (!string.IsNullOrWhiteSpace(request.ActivityRemark))
            query = query.Where(a => a.ActivityRemark != null && a.ActivityRemark.Contains(request.ActivityRemark));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(a =>
                (a.UpdateName != null && a.UpdateName.Contains(term))
                || (a.DoneBy != null && a.DoneBy.Contains(term))
                || (a.Remarks != null && a.Remarks.Contains(term))
                || (a.ActivityRemark != null && a.ActivityRemark.Contains(term)));
        }

        query = query.OrderByDescending(a => a.DateAndTime).ThenByDescending(a => a.Id);

        var totalCount = await query.CountAsync(ct);

        int pageNumber, pageSize;
        if (request.PageSize == -1)
        {
            pageNumber = 1;
            pageSize = totalCount > 0 ? totalCount : 1;
        }
        else
        {
            pageNumber = request.PageNumber;
            pageSize = request.PageSize;
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        var items = await query.Select(a => new UpdateActivityDto
        {
            Id = a.Id,
            ActivityType = a.ActivityType,
            ActivityStatus = a.ActivityStatus,
            CreatedDate = a.DateAndTime,
            Records = a.Records,
            IPAddress = a.IPAddress,
            Remarks = a.Remarks,
            UpdateName = a.UpdateName,
            DoneBy = a.DoneBy,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Duration = a.Duration,
            ActivityRemark = a.ActivityRemark
        }).ToListAsync(ct);

        return new PagedResult<UpdateActivityDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<byte[]> ExportUpdateActivityToExcelAsync(
        UpdateActivityQueryParameters request, CancellationToken ct)
    {
        // Export the full filtered set (ignore paging).
        var unpagedRequest = new UpdateActivityQueryParameters
        {
            Id = request.Id,
            ActivityType = request.ActivityType,
            ActivityStatus = request.ActivityStatus,
            CreatedDateFrom = request.CreatedDateFrom,
            CreatedDateTo = request.CreatedDateTo,
            DoneBy = request.DoneBy,
            Remarks = request.Remarks,
            ActivityRemark = request.ActivityRemark,
            SearchTerm = request.SearchTerm,
            PageNumber = 1,
            PageSize = -1
        };
        var paged = await GetUpdateActivityAsync(unpagedRequest, ct);

        var headers = new[]
        {
            "Id", "ActivityType", "ActivityStatus", "CreatedDate", "Records", "IPAddress",
            "Remarks", "UpdateName", "DoneBy", "StartTime", "EndTime", "Duration", "ActivityRemark"
        };

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("UpdateActivity");

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = worksheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
        }

        var rows = paged.Items.ToList();
        for (var r = 0; r < rows.Count; r++)
        {
            var item = rows[r];
            object?[] values =
            {
                item.Id, item.ActivityType, item.ActivityStatus, item.CreatedDate,
                item.Records, item.IPAddress, item.Remarks, item.UpdateName, item.DoneBy,
                item.StartTime, item.EndTime, item.Duration, item.ActivityRemark
            };
            for (var c = 0; c < values.Length; c++)
            {
                var v = values[c] is string s && s.Length > 0 && "=+-@".Contains(s[0]) ? "'" + s : values[c];
                SetCellValue(worksheet.Cell(r + 2, c + 1), v);
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
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
    private static bool MatchesYearVariable(string variableName, string fieldName, string? displayName)
    {
        var name = fieldName ?? "";
        var disp = displayName ?? "";
        var normName = name.Replace("_", "").Replace(" ", "");
        var normDisp = disp.Replace("_", "").Replace(" ", "");

        var pattern = $"^{variableName}[A-Za-z0-9]*$";

        return Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase) ||
               Regex.IsMatch(disp, pattern, RegexOptions.IgnoreCase) ||
               Regex.IsMatch(normName, pattern, RegexOptions.IgnoreCase) ||
               Regex.IsMatch(normDisp, pattern, RegexOptions.IgnoreCase);
    }

    private static bool IsAssessmentYearField(string fieldName, string? displayName)
    {
        const string variableName = "AssessmentYear";
        return MatchesYearVariable(variableName, fieldName, displayName);
    }

    private static bool IsConstructionYearField(string fieldName, string? displayName)
    {
        const string variableName = "ConstructionYear";
        return MatchesYearVariable(variableName, fieldName, displayName);
    }

    /// <summary>
    /// Runs the configured field validation (required / max length / regex / year rules) against a value set and
    /// returns the collected error messages. Shared by <see cref="BulkUpdateAsync"/> and the Excel import.
    /// Supports checking existing DB entity values when AssessmentYear or ConstructionYear are updated separately.
    /// </summary>
    private static List<string> ValidateFieldValues(
        IEnumerable<BulkUpdateFieldConfigDto> configs,
        IDictionary<string, object?> data,
        BaseEntity? existingEntity = null)
    {
        var errors = new List<string>();
        var activeConfigs = configs.Where(f => f.IsActive).ToList();

        BulkUpdateFieldConfigDto? assessmentConfig = null;
        BulkUpdateFieldConfigDto? constructionConfig = null;

        foreach (var config in activeConfigs)
        {
            data.TryGetValue(config.FieldName, out var raw);
            var convertedValue = ConvertJsonElementToValue(raw);
            var value = convertedValue?.ToString()?.Trim();

            var label = !string.IsNullOrWhiteSpace(config.DisplayName) ? config.DisplayName.Trim() : config.FieldName;

            if (config.IsRequired && string.IsNullOrWhiteSpace(value))
                errors.Add($"{label} is required.");

            if (config.MaxLength.HasValue && value?.Length > config.MaxLength)
                errors.Add($"{label} exceeds max length of {config.MaxLength}.");

            if (!string.IsNullOrEmpty(config.ValidationRegex) && !string.IsNullOrWhiteSpace(value))
            {
                var cleanRegex = config.ValidationRegex;
                if (cleanRegex.Contains("condition", StringComparison.OrdinalIgnoreCase))
                {
                    cleanRegex = Regex.Replace(cleanRegex, "condition", "", RegexOptions.IgnoreCase);
                }

                if (!string.IsNullOrWhiteSpace(cleanRegex) && !Regex.IsMatch(value, cleanRegex))
                    errors.Add($"{label} has invalid format.");
            }

            // Year-specific validation rules
            var isAssessOrConstYear = IsAssessmentYearField(config.FieldName, config.DisplayName)
                || IsConstructionYearField(config.FieldName, config.DisplayName);
            var isYearControl = string.Equals(config.ControlType, "year", StringComparison.OrdinalIgnoreCase)
                || isAssessOrConstYear;

            if (isYearControl && !string.IsNullOrWhiteSpace(value))
            {
                // Rule 1: 4-digit format check ('^[1-9]\d{3}$')
                if (!Regex.IsMatch(value, @"^[1-9]\d{3}$"))
                {
                    errors.Add($"{label} must be a valid 4-digit year.");
                }
                else if (isAssessOrConstYear && int.TryParse(value, out var numYear))
                {
                    // Rule 2: Cannot exceed current year check - strictly for ConstructionYear and AssessmentYear
                    var currentYear = DateTime.Now.Year;
                    if (numYear > currentYear)
                    {
                        errors.Add($"{label} cannot exceed current year ({currentYear}).");
                    }
                }
            }

            if (IsAssessmentYearField(config.FieldName, config.DisplayName))
                assessmentConfig = config;
            else if (IsConstructionYearField(config.FieldName, config.DisplayName))
                constructionConfig = config;
        }

        // Rule 3: AssessmentYear >= ConstructionYear (from data or DB fallback)
        bool isAssessInUpdate = false;
        bool isConstInUpdate = false;
        string? valAssessStr = null;
        string? valConstStr = null;

        if (assessmentConfig != null)
        {
            if (data.TryGetValue(assessmentConfig.FieldName, out var rawAssess) && rawAssess != null)
            {
                valAssessStr = ConvertJsonElementToValue(rawAssess)?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(valAssessStr))
                    isAssessInUpdate = true;
            }
        }

        if (string.IsNullOrWhiteSpace(valAssessStr))
        {
            foreach (var kvp in data)
            {
                if (IsAssessmentYearField(kvp.Key, null))
                {
                    var str = ConvertJsonElementToValue(kvp.Value)?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        valAssessStr = str;
                        isAssessInUpdate = true;
                        break;
                    }
                }
            }
        }

        if (constructionConfig != null)
        {
            if (data.TryGetValue(constructionConfig.FieldName, out var rawConst) && rawConst != null)
            {
                valConstStr = ConvertJsonElementToValue(rawConst)?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(valConstStr))
                    isConstInUpdate = true;
            }
        }

        if (string.IsNullOrWhiteSpace(valConstStr))
        {
            foreach (var kvp in data)
            {
                if (IsConstructionYearField(kvp.Key, null))
                {
                    var str = ConvertJsonElementToValue(kvp.Value)?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        valConstStr = str;
                        isConstInUpdate = true;
                        break;
                    }
                }
            }
        }

        // DB Fallback if one of the year fields is missing from data
        if (existingEntity != null)
        {
            var entityType = existingEntity.GetType();

            if (string.IsNullOrWhiteSpace(valAssessStr))
            {
                var dbAssessProp = entityType.GetProperty("AssessmentYear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                valAssessStr = dbAssessProp?.GetValue(existingEntity)?.ToString()?.Trim();
            }

            if (string.IsNullOrWhiteSpace(valConstStr))
            {
                var dbConstProp = entityType.GetProperty("ConstructionYear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                valConstStr = dbConstProp?.GetValue(existingEntity)?.ToString()?.Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(valAssessStr) && !string.IsNullOrWhiteSpace(valConstStr))
        {
            if (int.TryParse(valAssessStr, out var numAssess) && int.TryParse(valConstStr, out var numConst))
            {
                var currentYear = DateTime.Now.Year;
                if (Regex.IsMatch(valAssessStr, @"^[1-9]\d{3}$") && Regex.IsMatch(valConstStr, @"^[1-9]\d{3}$") &&
                    numAssess <= currentYear && numConst <= currentYear)
                {
                    if (numConst > numAssess)
                    {
                        if (isAssessInUpdate && isConstInUpdate)
                        {
                            errors.Add("Assessment Year must be greater than or equal to Construction Year.");
                        }
                        else if (isConstInUpdate)
                        {
                            errors.Add("Construction Year cannot be greater than Assessment Year.");
                        }
                        else
                        {
                            errors.Add("Assessment Year cannot be less than Construction Year.");
                        }
                    }
                }
            }
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

    private async Task EnrichUpdateHistoryItemsAsync(List<UpdateHistoryDto> items, CancellationToken ct)
    {
        if (items == null || items.Count == 0)
            return;

        // 1. Dynamically build field configuration map from BulkUpdateFieldConfig & SourceTableDetails metadata in DB
        var fieldSpecs = new Dictionary<string, (Type? EntityType, string KeyProp, string ValueProp, string DisplayLabel)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var dbConfigs = await _fieldConfigRepo.GetQueryable()
                .Where(fc => fc.IsActive)
                .Select(fc => new { fc.FieldName, fc.DisplayName, fc.BindApi, fc.ApiResponse })
                .ToListAsync(ct);

            foreach (var cfg in dbConfigs)
            {
                var entityType = FindEntityTypeFromBindApi(cfg.BindApi);
                var (keyProp, valProp) = ParseApiResponse(cfg.ApiResponse);
                RegisterDynamicFieldSpec(fieldSpecs, cfg.FieldName, cfg.DisplayName, entityType, keyProp, valProp);
            }

            var stdConfigs = await _sourceTableDetailsRepo.GetQueryable()
                .Where(std => std.IsActive)
                .Select(std => new { std.FieldName, std.DisplayName, std.BindApi, std.ApiResponse })
                .ToListAsync(ct);

            foreach (var cfg in stdConfigs)
            {
                var entityType = FindEntityTypeFromBindApi(cfg.BindApi);
                var (keyProp, valProp) = ParseApiResponse(cfg.ApiResponse);
                RegisterDynamicFieldSpec(fieldSpecs, cfg.FieldName, cfg.DisplayName, entityType, keyProp, valProp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load dynamic field configurations for history enrichment.");
        }

        // 2. Gather distinct IDs per EntityType across all items' OldValue and NewValue JSON strings
        var idsToLoad = new Dictionary<Type, HashSet<long>>();

        foreach (var item in items)
        {
            ExtractIdsFromJson(item.OldValue, fieldSpecs, idsToLoad);
            ExtractIdsFromJson(item.NewValue, fieldSpecs, idsToLoad);
        }

        // 3. Query DB via IDynamicEntityLoader for all gathered IDs
        var resolvedMap = new Dictionary<(Type EntityType, long Id), string>();

        if (idsToLoad.Count > 0)
        {
            foreach (var (entityType, idSet) in idsToLoad)
            {
                if (idSet.Count == 0) continue;
                try
                {
                    var loadedEntities = await _entityLoader.LoadByKeyAsync(
                        entityType, "Id", idSet.ToList(), asNoTracking: true, cancellationToken: ct);

                    foreach (var entity in loadedEntities)
                    {
                        var desc = GetDisplayValueForEntity(entity, fieldSpecs);
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            resolvedMap[(entityType, entity.Id)] = desc;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load master lookup entities of type {EntityType} for history enrichment.", entityType.Name);
                }
            }
        }

        // 4. Transform OldValue and NewValue in all items
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.OldValue))
            {
                item.OldValue = TransformJsonValues(item.OldValue, fieldSpecs, resolvedMap);
            }
            if (!string.IsNullOrWhiteSpace(item.NewValue))
            {
                item.NewValue = TransformJsonValues(item.NewValue, fieldSpecs, resolvedMap);
            }
        }
    }

    private static void RegisterDynamicFieldSpec(
        Dictionary<string, (Type? EntityType, string KeyProp, string ValueProp, string DisplayLabel)> specs,
        string rawFieldName, string? rawDisplayName, Type? entityType, string keyProp, string valueProp)
    {
        if (string.IsNullOrWhiteSpace(rawFieldName)) return;

        var cleanName = rawFieldName.Trim('[', ']', ' ');
        string displayLabel;

        if (!string.IsNullOrWhiteSpace(rawDisplayName))
        {
            displayLabel = rawDisplayName.Trim();
        }
        else if (cleanName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && cleanName.Length > 2)
        {
            displayLabel = cleanName[..^2];
        }
        else
        {
            displayLabel = cleanName;
        }

        var spec = (entityType, keyProp, valueProp, displayLabel);

        specs[cleanName] = spec;
        specs[rawFieldName] = spec;

        if (!cleanName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        {
            specs[cleanName + "Id"] = spec;
        }
    }

    private static (string KeyProp, string ValueProp) ParseApiResponse(string? apiResponse)
    {
        if (string.IsNullOrWhiteSpace(apiResponse))
            return ("Id", "Description");

        var tokens = apiResponse.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var keyProp = tokens.Length > 0 ? tokens[0] : "Id";
        var valProp = tokens.Length > 1 ? tokens[1] : "Description";
        return (keyProp, valProp);
    }

    private static readonly Lazy<List<Type>> BaseEntityTypes = new(() =>
        typeof(BaseEntity).Assembly.GetTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList());

    private static readonly ConcurrentDictionary<string, Type?> BindApiTypeCache = new(StringComparer.OrdinalIgnoreCase);

    private static Type? FindEntityTypeFromBindApi(string? bindApi)
    {
        if (string.IsNullOrWhiteSpace(bindApi)) return null;

        return BindApiTypeCache.GetOrAdd(bindApi.Trim(), ResolveEntityTypeFromBindApi);
    }

    private static Type? ResolveEntityTypeFromBindApi(string bindApi)
    {
        var segment = bindApi.Trim('/').Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(segment)) return null;

        var normSegment = segment.Replace("-", "").Replace("_", "");

        var candidateNames = new[]
        {
            segment + "Entity",
            segment + "MasterEntity",
            segment,
            segment + "MastEntity",
            normSegment + "Entity",
            normSegment + "MasterEntity",
            normSegment,
            normSegment + "MastEntity"
        };

        return BaseEntityTypes.Value
            .FirstOrDefault(t =>
                candidateNames.Any(c => string.Equals(t.Name, c, StringComparison.OrdinalIgnoreCase)) ||
                t.Name.StartsWith(normSegment, StringComparison.OrdinalIgnoreCase) ||
                t.Name.StartsWith(segment, StringComparison.OrdinalIgnoreCase));
    }

    private static void ExtractIdsFromJson(
        string? jsonString,
        Dictionary<string, (Type? EntityType, string KeyProp, string ValueProp, string DisplayLabel)> fieldSpecs,
        Dictionary<Type, HashSet<long>> idsToLoad)
    {
        if (string.IsNullOrWhiteSpace(jsonString)) return;
        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (fieldSpecs.TryGetValue(prop.Name, out var spec) && spec.EntityType != null && TryExtractId(prop.Value, out long id) && id > 0)
                {
                    if (!idsToLoad.TryGetValue(spec.EntityType, out var set))
                    {
                        set = new HashSet<long>();
                        idsToLoad[spec.EntityType] = set;
                    }
                    set.Add(id);
                }
            }
        }
        catch
        {
            // Invalid JSON, skip
        }
    }

    private static string TransformJsonValues(
        string jsonString,
        Dictionary<string, (Type? EntityType, string KeyProp, string ValueProp, string DisplayLabel)> fieldSpecs,
        Dictionary<(Type EntityType, long Id), string> resolvedMap)
    {
        if (string.IsNullOrWhiteSpace(jsonString)) return jsonString;
        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return jsonString;

            var dict = new Dictionary<string, object?>();
            bool modified = false;

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var key = prop.Name;
                var valElement = prop.Value;

                if (fieldSpecs.TryGetValue(key, out var spec))
                {
                    var displayKey = !string.IsNullOrWhiteSpace(spec.DisplayLabel) ? spec.DisplayLabel : key;

                    if (spec.EntityType != null && TryExtractId(valElement, out long rawId) && rawId > 0)
                    {
                        if (resolvedMap.TryGetValue((spec.EntityType, rawId), out var desc) && !string.IsNullOrWhiteSpace(desc))
                        {
                            dict[displayKey] = desc;
                            modified = true;
                            continue;
                        }
                    }

                    if (!string.Equals(key, displayKey, StringComparison.Ordinal))
                    {
                        modified = true;
                    }

                    dict[displayKey] = ExtractJsonValue(valElement);
                    continue;
                }

                var fallbackKey = (key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && key.Length > 2)
                    ? key[..^2]
                    : key;

                if (!string.Equals(key, fallbackKey, StringComparison.Ordinal))
                {
                    modified = true;
                }

                dict[fallbackKey] = ExtractJsonValue(valElement);
            }

            if (modified)
            {
                return JsonSerializer.Serialize(dict);
            }
        }
        catch
        {
            // Ignore parsing error, return original string
        }

        return jsonString;
    }

    private static object? ExtractJsonValue(JsonElement valElement)
    {
        return valElement.ValueKind switch
        {
            JsonValueKind.String => valElement.GetString(),
            JsonValueKind.Number => valElement.TryGetInt64(out long lVal) ? lVal : (valElement.TryGetDouble(out double dVal) ? dVal : valElement.GetRawText()),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => valElement.GetRawText()
        };
    }

    private static bool TryExtractId(JsonElement element, out long id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out id))
            return true;
        if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out id))
            return true;
        id = 0;
        return false;
    }

    private static string? GetDisplayValueForEntity(BaseEntity entity, string valPropName)
    {
        var type = entity.GetType();
        if (!string.IsNullOrWhiteSpace(valPropName))
        {
            var propInfo = type.GetProperty(valPropName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo != null)
            {
                var val = propInfo.GetValue(entity)?.ToString();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
        }

        // Dynamic fallback: return the first non-empty string property on the entity (excluding metadata fields)
        var stringProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead);

        foreach (var p in stringProps)
        {
            if (p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("UpdatedBy", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("IPAddress", StringComparison.OrdinalIgnoreCase))
                continue;

            var val = p.GetValue(entity)?.ToString();
            if (!string.IsNullOrWhiteSpace(val)) return val;
        }

        return null;
    }

    private static string? GetDisplayValueForEntity(
        BaseEntity entity,
        Dictionary<string, (Type? EntityType, string KeyProp, string ValueProp, string DisplayLabel)> fieldSpecs)
    {
        var type = entity.GetType();

        // 1. Try matching ValueProp from ApiResponse configured for this entity type
        foreach (var spec in fieldSpecs.Values)
        {
            if (spec.EntityType != null && (spec.EntityType == type || type.IsSubclassOf(spec.EntityType)))
            {
                var propInfo = type.GetProperty(spec.ValueProp, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propInfo != null)
                {
                    var val = propInfo.GetValue(entity)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) return val;
                }
            }
        }

        // 2. Dynamic fallback: return the first non-empty string property on the entity (excluding metadata fields)
        var stringProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead);

        foreach (var p in stringProps)
        {
            if (p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("UpdatedBy", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("IPAddress", StringComparison.OrdinalIgnoreCase))
                continue;

            var val = p.GetValue(entity)?.ToString();
            if (!string.IsNullOrWhiteSpace(val)) return val;
        }

        return null;
    }
}
