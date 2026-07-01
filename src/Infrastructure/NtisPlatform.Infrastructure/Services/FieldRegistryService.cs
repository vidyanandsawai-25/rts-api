using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.FieldRegistry;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

public class FieldRegistryService : IFieldRegistryService
{
    private const string GetAllSchemasSql = """
        SELECT DISTINCT s.name AS SchemaName
        FROM sys.schemas s
        INNER JOIN sys.tables t ON s.schema_id = t.schema_id
        WHERE s.schema_id > 4
        ORDER BY s.name;
        """;

    private const string CountDetailsBySchemaBaseSql = """
        SELECT COUNT(*)
        FROM sys.schemas s
        INNER JOIN sys.tables t
            ON s.schema_id = t.schema_id
        WHERE s.name = @SchemaName
        """;

    private const string GetDetailsBySchemaBaseSql = """
        SELECT
            s.name AS SchemaName,
            t.name AS TableName
        FROM sys.schemas s
        INNER JOIN sys.tables t
            ON s.schema_id = t.schema_id
        WHERE s.name = @SchemaName
        """;

    private const string SchemaTableSearchFilter = " AND t.name LIKE @SearchTerm";

    private const string CountDetailsByTableBaseSql = """
        SELECT COUNT(*)
        FROM sys.schemas s
        INNER JOIN sys.tables t ON s.schema_id = t.schema_id
        INNER JOIN sys.columns c ON t.object_id = c.object_id
        WHERE s.name = @SchemaName
          AND t.name = @TableName
        """;

    private const string GetDetailsByTableBaseSql = """
        SELECT
            c.name AS ColumnName
        FROM sys.schemas s
        INNER JOIN sys.tables t ON s.schema_id = t.schema_id
        INNER JOIN sys.columns c ON t.object_id = c.object_id
        WHERE s.name = @SchemaName
          AND t.name = @TableName
        """;

    private const string TableColumnSearchFilter = " AND c.name LIKE @SearchTerm";

    private readonly ApplicationDbContext _context;

    public FieldRegistryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FieldRegistryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var schemas = new List<FieldRegistryDto>();
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = GetAllSchemasSql;

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            schemas.Add(new FieldRegistryDto
            {
                SchemaName = reader.GetString(0)
            });
        }

        if (connection.State == System.Data.ConnectionState.Open)
        {
            await connection.CloseAsync();
        }
        return schemas;
    }

    public async Task<PagedResult<FieldRegistryDetailsDto>> GetDetailsBySchemaAsync(
        FieldRegistryDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var details = new List<FieldRegistryDetailsDto>();
        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var hasSearch = !string.IsNullOrWhiteSpace(queryParameters.SearchTerm);
        var searchValue = hasSearch ? $"%{queryParameters.SearchTerm!.Trim()}%" : null;

        var totalCount = await GetDetailsCountAsync(queryParameters.SchemaName, queryParameters.SearchTerm, cancellationToken);
        var isUnpaged = queryParameters.PageSize == -1;
        var pageNumber = isUnpaged ? 1 : queryParameters.PageNumber;
        var pageSize = isUnpaged ? (totalCount > 0 ? totalCount : 1) : queryParameters.PageSize;

        await using var command = connection.CreateCommand();
        command.CommandText = GetDetailsBySchemaBaseSql
            + (hasSearch ? SchemaTableSearchFilter : string.Empty)
            + " ORDER BY t.name"
            + (isUnpaged ? ";" : " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

        var schemaNameParameter = command.CreateParameter();
        schemaNameParameter.ParameterName = "@SchemaName";
        schemaNameParameter.Value = queryParameters.SchemaName;
        command.Parameters.Add(schemaNameParameter);

        if (hasSearch)
        {
            AddStringParameter(command, "@SearchTerm", searchValue!);
        }

        if (!isUnpaged)
        {
            var offsetParameter = command.CreateParameter();
            offsetParameter.ParameterName = "@Offset";
            offsetParameter.Value = (pageNumber - 1) * pageSize;
            command.Parameters.Add(offsetParameter);

            var pageSizeParameter = command.CreateParameter();
            pageSizeParameter.ParameterName = "@PageSize";
            pageSizeParameter.Value = pageSize;
            command.Parameters.Add(pageSizeParameter);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            details.Add(new FieldRegistryDetailsDto
            {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1)
            });
        }

        var pagedResult = new PagedResult<FieldRegistryDetailsDto>(details, totalCount, pageNumber, pageSize);
        if (connection.State == System.Data.ConnectionState.Open)
        {
            await connection.CloseAsync();
        }
        return pagedResult;
    }

    public async Task<PagedResult<FieldRegistryTableDetailsDto>> GetDetailsByTableAsync(
        FieldRegistryTableDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var details = new List<FieldRegistryTableDetailsDto>();
        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var hasSearch = !string.IsNullOrWhiteSpace(queryParameters.SearchTerm);

        var totalCount = await GetTableDetailsCountAsync(
            queryParameters.SchemaName,
            queryParameters.TableName,
            queryParameters.SearchTerm,
            cancellationToken);

        var isUnpaged = queryParameters.PageSize == -1;
        var pageNumber = isUnpaged ? 1 : queryParameters.PageNumber;
        var pageSize = isUnpaged ? (totalCount > 0 ? totalCount : 1) : queryParameters.PageSize;

        await using var command = connection.CreateCommand();
        command.CommandText = GetDetailsByTableBaseSql
            + (hasSearch ? TableColumnSearchFilter : string.Empty)
            + " ORDER BY c.column_id"
            + (isUnpaged ? ";" : " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

        AddStringParameter(command, "@SchemaName", queryParameters.SchemaName);
        AddStringParameter(command, "@TableName", queryParameters.TableName);

        if (hasSearch)
        {
            AddStringParameter(command, "@SearchTerm", $"%{queryParameters.SearchTerm!.Trim()}%");
        }

        if (!isUnpaged)
        {
            var offsetParameter = command.CreateParameter();
            offsetParameter.ParameterName = "@Offset";
            offsetParameter.Value = (pageNumber - 1) * pageSize;
            command.Parameters.Add(offsetParameter);

            var pageSizeParameter = command.CreateParameter();
            pageSizeParameter.ParameterName = "@PageSize";
            pageSizeParameter.Value = pageSize;
            command.Parameters.Add(pageSizeParameter);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            details.Add(new FieldRegistryTableDetailsDto
            {
                ColumnName = reader.GetString(0)
            });
        }

        var pagedResult = new PagedResult<FieldRegistryTableDetailsDto>(details, totalCount, pageNumber, pageSize);
        if (connection.State == System.Data.ConnectionState.Open)
        {
            await connection.CloseAsync();
        }
        return pagedResult;
    }

    private async Task<int> GetDetailsCountAsync(string schemaName, string? searchTerm, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);

        await using var command = connection.CreateCommand();
        command.CommandText = CountDetailsBySchemaBaseSql
            + (hasSearch ? SchemaTableSearchFilter : string.Empty)
            + ";";

        var schemaNameParameter = command.CreateParameter();
        schemaNameParameter.ParameterName = "@SchemaName";
        schemaNameParameter.Value = schemaName;
        command.Parameters.Add(schemaNameParameter);

        if (hasSearch)
        {
            AddStringParameter(command, "@SearchTerm", $"%{searchTerm!.Trim()}%");
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private async Task<int> GetTableDetailsCountAsync(
        string schemaName,
        string tableName,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);

        await using var command = connection.CreateCommand();
        command.CommandText = CountDetailsByTableBaseSql
            + (hasSearch ? TableColumnSearchFilter : string.Empty)
            + ";";

        AddStringParameter(command, "@SchemaName", schemaName);
        AddStringParameter(command, "@TableName", tableName);

        if (hasSearch)
        {
            AddStringParameter(command, "@SearchTerm", $"%{searchTerm!.Trim()}%");
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static void AddStringParameter(System.Data.Common.DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public async Task<FieldRegistryResponseDto> AddFieldRegistryAsync(
        CreateFieldRegistryDto createDto,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var masterEntity = new BulkUpdateMasterEntity
            {
                UpdateCode = createDto.UpdateCode,
                UpdateName = createDto.UpdateName,
                UpdateNameMarathi = createDto.UpdateNameMarathi,
                ReferenceTableName = createDto.ReferenceTableName,
                DisplaySequence = createDto.DisplaySequence,
                Description = createDto.Description,
                Category = createDto.Category,
                IsApprovalRequired = createDto.IsApprovalRequired,
                IsActive = createDto.IsActive,
                CreatedBy = createDto.CreatedBy,
                CreatedDate = DateTime.UtcNow
            };

            _context.BulkUpdateMasters.Add(masterEntity);
            await _context.SaveChangesAsync(cancellationToken);

            var fieldConfigEntities = new List<BulkUpdateFieldConfigEntity>();
            var sequenceNo = 1;

            foreach (var fieldConfig in createDto.FieldConfigs)
            {
                var fieldConfigEntity = new BulkUpdateFieldConfigEntity
                {
                    BulkUpdateMasterId = masterEntity.Id,
                    FieldName = fieldConfig.FieldName,
                    DisplayName = fieldConfig.DisplayName,
                    DisplayNameMarathi = fieldConfig.DisplayNameMarathi,
                    ControlType = fieldConfig.ControlType,
                    DataType = fieldConfig.DataType,
                    Placeholder = fieldConfig.Placeholder,
                    IsRequired = fieldConfig.IsRequired,
                    MaxLength = fieldConfig.MaxLength,
                    ValidationRegex = fieldConfig.ValidationRegex,
                    DefaultValue = fieldConfig.DefaultValue,
                    SequenceNo = sequenceNo++,
                    IsReadonly = false,
                    BindApi = fieldConfig.BindApi,
                    IsActive = createDto.IsActive,
                    CreatedBy = createDto.CreatedBy,
                    CreatedDate = DateTime.UtcNow
                };

                fieldConfigEntities.Add(fieldConfigEntity);
            }

            _context.BulkUpdateFieldConfigs.AddRange(fieldConfigEntities);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            masterEntity.FieldConfigs = fieldConfigEntities;
            return MapToResponseDto(masterEntity);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<FieldRegistryResponseDto>> GetFieldRegistriesAsync(
        FieldRegistryQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BulkUpdateMasters
            .Include(m => m.FieldConfigs)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.UpdateCode))
        {
            query = query.Where(m => m.UpdateCode == queryParameters.UpdateCode);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.UpdateName))
        {
            query = query.Where(m => m.UpdateName == queryParameters.UpdateName);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.ReferenceTableName))
        {
            query = query.Where(m => m.ReferenceTableName == queryParameters.ReferenceTableName);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Category))
        {
            query = query.Where(m => m.Category == queryParameters.Category);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.FieldName))
        {
            query = query.Where(m => m.FieldConfigs.Any(fc => fc.FieldName == queryParameters.FieldName));
        }

        query = query.OrderBy(m => m.DisplaySequence).ThenBy(m => m.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var isUnpaged = queryParameters.PageSize == -1;
        var pageNumber = isUnpaged ? 1 : queryParameters.PageNumber;
        var pageSize = isUnpaged ? (totalCount > 0 ? totalCount : 1) : queryParameters.PageSize;

        if (!isUnpaged)
        {
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        var masters = await query.ToListAsync(cancellationToken);
        var items = masters.Select(MapToResponseDto).ToList();

        return new PagedResult<FieldRegistryResponseDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> DeleteFieldRegistryAsync(
        string updateCode,
        int? updatedBy,
        CancellationToken cancellationToken = default)
    {
        var masterEntity = await _context.BulkUpdateMasters
            .Include(m => m.FieldConfigs)
            .FirstOrDefaultAsync(m => m.UpdateCode == updateCode, cancellationToken);

        if (masterEntity is null)
        {
            return false;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;

            masterEntity.IsActive = false;
            masterEntity.UpdatedDate = now;
            if (updatedBy.HasValue)
            {
                masterEntity.UpdatedBy = updatedBy;
            }

            foreach (var fieldConfig in masterEntity.FieldConfigs)
            {
                fieldConfig.IsActive = false;
                fieldConfig.UpdatedDate = now;
                if (updatedBy.HasValue)
                {
                    fieldConfig.UpdatedBy = updatedBy;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static FieldRegistryResponseDto MapToResponseDto(BulkUpdateMasterEntity master)
    {
        return new FieldRegistryResponseDto
        {
            MasterId = master.Id,
            UpdateCode = master.UpdateCode,
            UpdateName = master.UpdateName,
            UpdateNameMarathi = master.UpdateNameMarathi,
            ReferenceTableName = master.ReferenceTableName,
            DisplaySequence = master.DisplaySequence,
            Description = master.Description,
            Category = master.Category,
            IsApprovalRequired = master.IsApprovalRequired,
            IsActive = master.IsActive,
            CreatedDate = master.CreatedDate,
            CreatedBy = master.CreatedBy,
            FieldConfigs = master.FieldConfigs.OrderBy(fc => fc.SequenceNo).Select(fc => new FieldRegistryFieldConfigResponseDto
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
                IsReadonly = fc.IsReadonly,
                BindApi = fc.BindApi,
                IsActive = fc.IsActive,
                CreatedDate = fc.CreatedDate,
                CreatedBy = fc.CreatedBy
            }).ToList()
        };
    }
}
