using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NtisPlatform.Application.DTOs.FieldRegistry;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

public class FieldRegistryService : IFieldRegistryService
{
    private readonly ApplicationDbContext _context;

    public FieldRegistryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<FieldRegistryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var schemas = GetMappedTables()
            .Select(t => t.Schema)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Select(s => new FieldRegistryDto { SchemaName = s })
            .ToList();

        return Task.FromResult<IReadOnlyList<FieldRegistryDto>>(schemas);
    }

    public Task<PagedResult<FieldRegistryDetailsDto>> GetDetailsBySchemaAsync(
        FieldRegistryDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var tables = GetMappedTables()
            .Where(t => string.Equals(t.Schema, queryParameters.SchemaName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
        {
            var term = queryParameters.SearchTerm.Trim();
            tables = tables.Where(t => t.Table.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = tables
            .OrderBy(t => t.Table, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalCount = ordered.Count;
        var isUnpaged = queryParameters.PageSize == -1;
        var pageNumber = isUnpaged ? 1 : queryParameters.PageNumber;
        var pageSize = isUnpaged ? (totalCount > 0 ? totalCount : 1) : queryParameters.PageSize;

        var pageItems = (isUnpaged ? ordered : ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize))
            .Select(t => new FieldRegistryDetailsDto
            {
                SchemaName = t.Schema,
                TableName = t.Table
            })
            .ToList();

        return Task.FromResult(new PagedResult<FieldRegistryDetailsDto>(pageItems, totalCount, pageNumber, pageSize));
    }

    public Task<PagedResult<FieldRegistryTableDetailsDto>> GetDetailsByTableAsync(
        FieldRegistryTableDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<string> columns = GetMappedColumns(queryParameters.SchemaName, queryParameters.TableName);

        if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
        {
            var term = queryParameters.SearchTerm.Trim();
            columns = columns.Where(c => c.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = columns
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalCount = ordered.Count;
        var isUnpaged = queryParameters.PageSize == -1;
        var pageNumber = isUnpaged ? 1 : queryParameters.PageNumber;
        var pageSize = isUnpaged ? (totalCount > 0 ? totalCount : 1) : queryParameters.PageSize;

        var pageItems = (isUnpaged ? ordered : ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize))
            .Select(c => new FieldRegistryTableDetailsDto { ColumnName = c })
            .ToList();

        return Task.FromResult(new PagedResult<FieldRegistryTableDetailsDto>(pageItems, totalCount, pageNumber, pageSize));
    }

    /// <summary>
    /// Distinct (schema, table) pairs for every table-mapped entity in the EF model. Entities mapped to a
    /// view / keyless (no table) are excluded; multiple entities sharing a table (TPH, owned) collapse via Distinct.
    /// </summary>
    private IEnumerable<(string Schema, string Table)> GetMappedTables()
    {
        var defaultSchema = _context.Model.GetDefaultSchema() ?? "dbo";
        return _context.Model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null)
            .Select(e => (Schema: e.GetSchema() ?? defaultSchema, Table: e.GetTableName()!))
            .Distinct();
    }

    /// <summary>
    /// Distinct column names mapped for the given schema/table across all entity types that map to it.
    /// </summary>
    private IEnumerable<string> GetMappedColumns(string schema, string table)
    {
        var defaultSchema = _context.Model.GetDefaultSchema() ?? "dbo";
        var columns = new List<string>();

        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            if (entityType.GetTableName() is not { } entityTable)
            {
                continue;
            }

            var entitySchema = entityType.GetSchema() ?? defaultSchema;
            if (!string.Equals(entitySchema, schema, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(entityTable, table, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);
            if (storeObject is null)
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject.Value);
                if (!string.IsNullOrEmpty(columnName))
                {
                    columns.Add(columnName);
                }
            }
        }

        return columns.Distinct(StringComparer.OrdinalIgnoreCase);
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
                CreatedDate = DateTime.Now
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
                    CreatedDate = DateTime.Now
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

    public async Task<bool> SetActiveStatusAsync(
        string updateCode,
        bool isActive,
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
            var now = DateTime.Now;

            masterEntity.IsActive = isActive;
            masterEntity.UpdatedDate = now;
            if (updatedBy.HasValue)
            {
                masterEntity.UpdatedBy = updatedBy;
            }

            foreach (var fieldConfig in masterEntity.FieldConfigs)
            {
                fieldConfig.IsActive = isActive;
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
