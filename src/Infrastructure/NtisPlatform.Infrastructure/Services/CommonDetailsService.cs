using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

public partial class CommonDetailsService : ICommonDetailsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommonDetailsService> _logger;

    /// <summary>
    /// Exhaustive list of tables that bulk-update SQL may target.
    /// Any <c>BulkUpdateMaster.ReferenceTableName</c> not in this set is rejected
    /// before SQL is built, preventing an attacker from redirecting updates to arbitrary tables
    /// even if the configuration table is compromised.
    /// </summary>
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "PTIS.PropertyMast",
        "PTIS.SocietyDetailsMast",
        "PTIS.PropertyMastDetails",
        "PTIS.PropertyDetails",
    };

    /// <summary>
    /// Matches a plain SQL identifier: starts with a letter or underscore, followed by
    /// letters, digits, or underscores only. Rejects anything containing dots, brackets,
    /// spaces, semicolons, quotes, or other characters that could break out of an identifier
    /// context even when wrapped in square brackets.
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex SafeIdentifierRegex();

    public CommonDetailsService(ApplicationDbContext context, ILogger<CommonDetailsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BulkUpdateMasterDto>> GetMenuAsync(CancellationToken ct)
    {
        return await _context.BulkUpdateMasters
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplaySequence)
            .Select(m => new BulkUpdateMasterDto
            {
                Id = m.Id,
                UpdateCode = m.UpdateCode,
                UpdateName = m.UpdateName,
                UpdateNameMarathi = m.UpdateNameMarathi,
                IconName = m.IconName,
                ReferenceTableName = m.ReferenceTableName,
                IsActive = m.IsActive,
                DisplaySequence = m.DisplaySequence,
                ApiRoute = m.ApiRoute,
                Description = m.Description
            })
            .ToListAsync(ct);
    }

    public async Task<List<BulkUpdateFieldConfigDto>> GetFormFieldsAsync(string updateCode, CancellationToken ct)
    {
        return await _context.BulkUpdateFieldConfigs
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
        var master = await _context.BulkUpdateMasters
            .FirstOrDefaultAsync(m => m.UpdateCode == request.UpdateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{request.UpdateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(request.UpdateCode, ct);
        var safeColumns = fieldConfigs.Select(f => f.FieldName).ToList();
        var targetTable = master.ReferenceTableName;

        var query = _context.PropertyMast
            .Where(pm => pm.WardId == request.WardId);

        if (!string.IsNullOrEmpty(request.FromPropertyNo))
            query = query.Where(pm => string.Compare(pm.PropertyNo, request.FromPropertyNo) >= 0);
        if (!string.IsNullOrEmpty(request.ToPropertyNo))
            query = query.Where(pm => string.Compare(pm.PropertyNo, request.ToPropertyNo) <= 0);
        if (!string.IsNullOrWhiteSpace(request.Wing))
            query = query.Where(pm => _context.SocietyDetailsMast
                .Any(sdm => sdm.PropertyId == pm.Id && sdm.WingName == request.Wing));

        var totalCount = await query.CountAsync(ct);

        var pagedProperties = await query
            .OrderBy(pm => pm.PropertyNo)
            //.Skip((request.PageNumber - 1) * request.PageSize)
            .Skip(request.PageSize == -1 ? 0 : (request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize == -1 ? totalCount : request.PageSize)
            .Join(_context.WardMaster, pm => pm.WardId, w => w.Id,
                (pm, w) => new { pm, w.WardNo })
            .ToListAsync(ct);

        var propertyIds = pagedProperties.Select(x => x.pm.Id).ToList();
        var relatedEntities = await LoadTargetEntitiesAsync(targetTable, propertyIds, ct);
        var isPropertyMast = targetTable.Equals("PTIS.PropertyMast", StringComparison.OrdinalIgnoreCase);

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

    private async Task<Dictionary<int, object>> LoadTargetEntitiesAsync(
        string targetTable, List<int> propertyIds, CancellationToken ct)
    {
        if (targetTable.Equals("PTIS.PropertyMast", StringComparison.OrdinalIgnoreCase))
            return [];

        if (targetTable.Equals("PTIS.SocietyDetailsMast", StringComparison.OrdinalIgnoreCase))
        {
            var list = await _context.SocietyDetailsMast
                .Where(s => s.PropertyId != null && propertyIds.Contains(s.PropertyId.Value))
                .ToListAsync(ct);
            return list
                .Where(s => s.PropertyId.HasValue)
                .GroupBy(s => s.PropertyId!.Value)
                .ToDictionary(g => g.Key, g => (object)g.First());
        }

        if (targetTable.Equals("PTIS.PropertyMastDetails", StringComparison.OrdinalIgnoreCase))
            return await _context.PropertyMastDetails
                .Where(a => propertyIds.Contains(a.PropertyId))
                .ToDictionaryAsync(a => a.PropertyId, a => (object)a, ct);

        if (targetTable.Equals("PTIS.PropertyDetails", StringComparison.OrdinalIgnoreCase))
        {
            var list = await _context.PropertyDetails
                .Where(d => propertyIds.Contains(d.PropertyId))
                .ToListAsync(ct);
            return list
                .GroupBy(d => d.PropertyId)
                .ToDictionary(g => g.Key, g => (object)g.First());
        }

        _logger.LogWarning("No EF entity mapped for target table {TargetTable}; CurrentValues will be empty.", targetTable);
        return [];
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
        var master = await _context.BulkUpdateMasters
            .FirstOrDefaultAsync(m => m.UpdateCode == request.UpdateCode && m.IsActive, ct)
            ?? throw new ArgumentException($"Update type '{request.UpdateCode}' not found.");

        var fieldConfigs = await GetFormFieldsAsync(request.UpdateCode, ct);

        // Normalize to a case-insensitive dictionary once so that all downstream lookups
        // ("plotArea" vs "PlotArea") are treated the same regardless of client casing.
        var updateData = new Dictionary<string, object?>(request.UpdateData, StringComparer.OrdinalIgnoreCase);

        // Dynamic validation
        var errors = new List<string>();
        foreach (var config in fieldConfigs.Where(f => f.IsActive))
        {
            updateData.TryGetValue(config.FieldName, out var raw);
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
        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors));

        // Whitelist field names (SQL injection guard)
        var allowedFields = fieldConfigs.Select(f => f.FieldName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fieldsToUpdate = updateData.Keys.Where(allowedFields.Contains).ToList();

        if (fieldsToUpdate.Count == 0)
            throw new ArgumentException("No valid fields to update.");

        // Guard 1: every column name must be a plain SQL identifier.
        // Field names come from the DB config, but this defence-in-depth check stops a
        // compromised config row (e.g. FieldName = "x]; DROP TABLE...") from ever reaching
        // the SQL interpolation step.
        var unsafeFields = fieldsToUpdate.Where(f => !SafeIdentifierRegex().IsMatch(f)).ToList();
        if (unsafeFields.Count > 0)
            throw new InvalidOperationException(
                $"Configured field name(s) contain characters that are unsafe for SQL interpolation: " +
                string.Join(", ", unsafeFields));

        var targetTable = master.ReferenceTableName;

        // Guard 2: table name must be a member of the known-safe set.
        // ReferenceTableName is DB-sourced but interpolated directly into SQL, so we never
        // trust it unconditionally — an attacker with DB write access could redirect updates
        // to any table if this check were absent.
        if (!AllowedTables.Contains(targetTable))
            throw new InvalidOperationException(
                $"Update type '{request.UpdateCode}' references an unrecognized table '{targetTable}'. " +
                "Add it to AllowedTables if this table is intentionally supported.");

        var isPropertyMast = targetTable.Equals("PTIS.PropertyMast", StringComparison.OrdinalIgnoreCase);
        var pkColumn = isPropertyMast ? "Id" : "PropertyId";

        var setClauses = fieldsToUpdate.Select(f => $"[{f}] = @{f}").ToList();
        setClauses.Add("UpdatedBy = @UpdatedBy");
        setClauses.Add("UpdatedDate = GETDATE()");
        var updateSql = $"UPDATE {targetTable} SET {string.Join(", ", setClauses)} WHERE [{pkColumn}] = @PropertyId";

        var columnList = string.Join(", ", fieldsToUpdate.Select(f => $"[{f}]"));
        var selectSql = $"SELECT {columnList} FROM {targetTable} WHERE [{pkColumn}] = @PropertyId";

        const string histSql = @"INSERT INTO PTIS.BulkUpdateHistory
            (BulkUpdateMasterId, PropertyId, OldValue, NewValue, UpdatedColumns, UpdatedBy, UpdatedDate, IPAddress)
            VALUES (@BulkUpdateMasterId, @PropertyId, @OldValue, @NewValue, @UpdatedColumns, @UpdatedBy, GETDATE(), @IPAddress)";

        var propertyIds = request.PropertyIds;

        var result = new BulkUpdateResultDto { TotalRequested = propertyIds.Count };

        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen) await connection.OpenAsync(ct);

        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var propertyId in propertyIds)
            {
                try
                {
                    string? oldValue = null;
                    try
                    {
                        using var selectCmd = connection.CreateCommand();
                        selectCmd.Transaction = transaction;
                        selectCmd.CommandText = selectSql;
                        AddParam(selectCmd, "@PropertyId", propertyId);
                        using var reader = await selectCmd.ExecuteReaderAsync(ct);
                        if (await reader.ReadAsync(ct))
                        {
                            var oldDict = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                                oldDict[reader.GetName(i)] = reader[i] is DBNull ? null : reader[i];
                            oldValue = JsonSerializer.Serialize(oldDict);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not read old values for property {PropertyId}", propertyId);
                    }

                    using var updateCmd = connection.CreateCommand();
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = updateSql;
                    foreach (var field in fieldsToUpdate)
                        AddParam(updateCmd, $"@{field}", updateData[field]);
                    AddParam(updateCmd, "@UpdatedBy", updatedBy);
                    AddParam(updateCmd, "@PropertyId", propertyId);
                    await updateCmd.ExecuteNonQueryAsync(ct);

                    var newValue = JsonSerializer.Serialize(
                        fieldsToUpdate.ToDictionary(f => f, f => updateData[f]));

                    using var histCmd = connection.CreateCommand();
                    histCmd.Transaction = transaction;
                    histCmd.CommandText = histSql;
                    AddParam(histCmd, "@BulkUpdateMasterId", master.Id);
                    AddParam(histCmd, "@PropertyId", propertyId);
                    AddParam(histCmd, "@OldValue", (object?)oldValue);
                    AddParam(histCmd, "@NewValue", (object?)newValue);
                    AddParam(histCmd, "@UpdatedColumns", (object?)string.Join(",", fieldsToUpdate));
                    AddParam(histCmd, "@UpdatedBy", updatedBy);
                    AddParam(histCmd, "@IPAddress", (object?)ipAddress);
                    await histCmd.ExecuteNonQueryAsync(ct);

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update property {PropertyId}", propertyId);
                    result.FailedCount++;
                    result.Errors.Add($"Property {propertyId}: {ex.Message}");
                }
            }

            if (result.FailedCount > 0)
            {
                transaction.Rollback();
                // Nothing was persisted — reset the count so callers get accurate numbers.
                result.Errors.Insert(0,
                    $"Transaction rolled back — no properties were updated. " +
                    $"{result.SuccessCount} of {result.TotalRequested} processed before the error(s) occurred, " +
                    "but all changes were reverted.");
                result.SuccessCount = 0;
            }
            else
            {
                transaction.Commit();
            }
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            if (!wasOpen) await connection.CloseAsync();
        }

        return result;
    }

    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = ConvertJsonElementToValue(value) ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private static object? ConvertJsonElementToValue(object? value)
    {
        if (value is not System.Text.Json.JsonElement jsonElement)
            return value;

        return jsonElement.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
            System.Text.Json.JsonValueKind.Number => ResolveNumber(jsonElement),
            System.Text.Json.JsonValueKind.True => (object)true,
            System.Text.Json.JsonValueKind.False => (object)false,
            System.Text.Json.JsonValueKind.Null => null,
            _ => jsonElement.ToString()
        };
    }

    private static object ResolveNumber(System.Text.Json.JsonElement element)
    {
        if (element.TryGetInt32(out int intVal)) return intVal;
        if (element.TryGetInt64(out long longVal)) return longVal;
        if (element.TryGetDecimal(out decimal decimalVal)) return decimalVal;
        return element.GetDouble();
    }
}
