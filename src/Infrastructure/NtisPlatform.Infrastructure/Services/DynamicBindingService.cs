using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// EF Core implementation of <see cref="IDynamicBindingService"/>. Uses metadata mapping
/// to dynamically set or clear the <c>DocumentBindingId</c> column on any mapped business table.
/// </summary>
public class DynamicBindingService : IDynamicBindingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DynamicBindingService> _logger;

    public DynamicBindingService(
        ApplicationDbContext context,
        ILogger<DynamicBindingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task LinkBindingToEntityAsync(
        string tableName,
        int entityId,
        int bindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entityType = _context.Model.GetEntityTypes()
            .FirstOrDefault(t => string.Equals(t.GetTableName(), tableName, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(t.ClrType.Name, tableName, StringComparison.OrdinalIgnoreCase));

        if (entityType == null)
        {
            _logger.LogWarning("DynamicBindingService: Entity type not found for reference table: {TableName}", tableName);
            return;
        }

        var bindingIdProperty = entityType.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "DocumentBindingId", StringComparison.OrdinalIgnoreCase));

        if (bindingIdProperty == null)
        {
            _logger.LogDebug("DynamicBindingService: Property 'DocumentBindingId' not found on entity {EntityName}. Skipping dynamic link.", entityType.Name);
            return;
        }

        var entity = await _context.FindAsync(entityType.ClrType, new object[] { entityId }, cancellationToken);

        if (entity == null)
        {
            _logger.LogWarning("DynamicBindingService: Entity of type {EntityName} with ID {EntityId} not found.", entityType.Name, entityId);
            return;
        }

        var clrProperty = bindingIdProperty.PropertyInfo;
        if (clrProperty != null && clrProperty.CanWrite)
        {
            object? valToSet = bindingId;
            if (clrProperty.PropertyType == typeof(int?))
            {
                valToSet = (int?)bindingId;
            }
            clrProperty.SetValue(entity, valToSet);

            // Dynamically set DocumentGuid if it exists on the entity
            var documentGuidProp = entityType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, "DocumentGuid", StringComparison.OrdinalIgnoreCase));
            if (documentGuidProp?.PropertyInfo != null && documentGuidProp.PropertyInfo.CanWrite)
            {
                var binding = await _context.DocumentBindings
                    .Include(db => db.Document)
                    .FirstOrDefaultAsync(db => db.Id == bindingId, cancellationToken);
                if (binding?.Document != null)
                {
                    object? guidToSet = binding.Document.DocumentGuid;
                    if (documentGuidProp.PropertyInfo.PropertyType == typeof(string))
                    {
                        guidToSet = binding.Document.DocumentGuid.ToString();
                    }
                    documentGuidProp.PropertyInfo.SetValue(entity, guidToSet);
                }
            }

            // Dynamically update standard audit properties if they exist
            var updatedByProp = entityType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, "UpdatedBy", StringComparison.OrdinalIgnoreCase));
            if (updatedByProp?.PropertyInfo != null && updatedByProp.PropertyInfo.CanWrite)
            {
                updatedByProp.PropertyInfo.SetValue(entity, updatedBy);
            }

            var updatedDateProp = entityType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, "UpdatedDate", StringComparison.OrdinalIgnoreCase));
            if (updatedDateProp?.PropertyInfo != null && updatedDateProp.PropertyInfo.CanWrite)
            {
                updatedDateProp.PropertyInfo.SetValue(entity, DateTime.Now);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("DynamicBindingService: Successfully linked BindingId={BindingId} to {EntityName} with ID={EntityId} dynamically.", bindingId, entityType.Name, entityId);
        }
    }

    /// <inheritdoc/>
    public async Task UnlinkBindingFromEntityAsync(
        string tableName,
        int entityId,
        int bindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entityType = _context.Model.GetEntityTypes()
            .FirstOrDefault(t => string.Equals(t.GetTableName(), tableName, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(t.ClrType.Name, tableName, StringComparison.OrdinalIgnoreCase));

        if (entityType == null) return;

        var bindingIdProperty = entityType.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "DocumentBindingId", StringComparison.OrdinalIgnoreCase));

        if (bindingIdProperty == null) return;

        var entity = await _context.FindAsync(entityType.ClrType, new object[] { entityId }, cancellationToken);

        if (entity == null) return;

        var clrProperty = bindingIdProperty.PropertyInfo;
        if (clrProperty != null && clrProperty.CanWrite)
        {
            var currentVal = clrProperty.GetValue(entity);
            if (currentVal != null && (int)Convert.ChangeType(currentVal, typeof(int)) == bindingId)
            {
                clrProperty.SetValue(entity, null);

                // Dynamically clear DocumentGuid if it exists on the entity
                var documentGuidProp = entityType.GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, "DocumentGuid", StringComparison.OrdinalIgnoreCase));
                if (documentGuidProp?.PropertyInfo != null && documentGuidProp.PropertyInfo.CanWrite)
                {
                    documentGuidProp.PropertyInfo.SetValue(entity, null);
                }

                var updatedByProp = entityType.GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, "UpdatedBy", StringComparison.OrdinalIgnoreCase));
                if (updatedByProp?.PropertyInfo != null && updatedByProp.PropertyInfo.CanWrite)
                {
                    updatedByProp.PropertyInfo.SetValue(entity, updatedBy);
                }

                var updatedDateProp = entityType.GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, "UpdatedDate", StringComparison.OrdinalIgnoreCase));
                if (updatedDateProp?.PropertyInfo != null && updatedDateProp.PropertyInfo.CanWrite)
                {
                    updatedDateProp.PropertyInfo.SetValue(entity, DateTime.Now);
                }

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("DynamicBindingService: Successfully unlinked BindingId={BindingId} from {EntityName} with ID={EntityId} dynamically.", bindingId, entityType.Name, entityId);
            }
        }
    }

    /// <inheritdoc/>
    public bool CanLinkEntity(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return false;

        var entityType = _context.Model.GetEntityTypes()
            .FirstOrDefault(t => string.Equals(t.GetTableName(), tableName, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(t.ClrType.Name, tableName, StringComparison.OrdinalIgnoreCase));

        if (entityType == null)
            return false;

        var bindingIdProperty = entityType.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "DocumentBindingId", StringComparison.OrdinalIgnoreCase));

        return bindingIdProperty != null;
    }
}
