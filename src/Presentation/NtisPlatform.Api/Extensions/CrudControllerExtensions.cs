using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Diagnostics.Metrics;

namespace NtisPlatform.Api.Extensions;

public static class CrudControllerExtensions
{
    #region Single CRUD Operations

    public static async Task<IActionResult> ExecuteGetAllPaged<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TQueryParams queryParameters,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        try
        {
            var result = await service.GetAllAsync(queryParameters, cancellationToken);
            return controller.Ok(result);
        }
        catch (FilterValidationException ex)
        {
            logger.LogWarning(ex, "Filter validation failed: {Message}", ex.Message);
            return controller.BadRequest(new
            {
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetAll operation failed");
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    public static async Task<IActionResult> ExecuteGetById<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TKey id,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        try
        {
            var result = await service.GetByIdAsync(id, cancellationToken);
            return result == null ? controller.NotFound() : controller.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById operation failed for id: {Id}", id);
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    public static async Task<IActionResult> ExecuteCreate<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TCreateDto createDto,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        try
        {
            var result = await service.CreateAsync(createDto, cancellationToken);
            return controller.Ok(new ApiResponse<TDto>
            {
                Success = true,
                Message = "Record inserted successfully",
                Items = result
            });
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            logger.LogError(ex, "Create operation failed");
            var errorMessage = ex.InnerException?.Message ?? ex.Message;
            // Duplicate / unique constraint (DB-agnostic-ish)
            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return controller.Conflict(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            // Return 500 for other exceptions
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while creating the record",
                Items = default
            });
        }
    }

    public static async Task<IActionResult> ExecuteUpdate<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TKey id,
        TUpdateDto updateDto,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        try
        {
            var result = await service.UpdateAsync(id, updateDto, cancellationToken);
            if (result == null)
            {
                return controller.Ok(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = "Record not found for Update ",
                    Items = result
                });
            }
            return controller.Ok(new ApiResponse<TDto>
            {
                Success = true,
                Message = "Record updated successfully",
                Items = result
            });
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            logger.LogError(ex, "Update operation failed for id: {Id}", id);
            var errorMessage = ex.InnerException?.Message ?? ex.Message;
            // Duplicate / unique constraint (DB-agnostic-ish)
            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return controller.Conflict(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            // Return 500 for other exceptions
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while updating the record",
                Items = default
            });
        }
    }

    // This method performs a soft-delete operation through the service layer.
    // For entities supporting hard deletion, it may also set MarkedForDeletion.
    public static async Task<IActionResult> ExecuteDelete<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TKey id,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        try
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result ? controller.Ok(new ApiResponse<TDto>
            {
                Success = true,
                Message = "Record marked for deletion"
            }) :
            controller.Ok(new ApiResponse<TDto>
            {
                Success = false,
                Message = "Record not found"
            });
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            logger.LogError(ex, "Delete operation failed for id: {Id}", id);
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while deleting the record",
                Items = default
            });
        }
    }

    /// <summary>
    /// Execute permanent delete operation through the centralized cleanup service.
    /// This is an irreversible operation and should be used with extreme caution.
    /// Routes through HardDeleteCleanupService to ensure consistent policy enforcement.
    /// Logs actor identity for audit trail.
    /// </summary>
    public static async Task<IActionResult> ExecuteForceDelete<TEntity, TKey>(
        this ControllerBase controller,
        IHardDeleteCleanupService cleanupService,
        TKey id,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var userName = controller.User?.Identity?.Name ?? "Anonymous";
        var userId = controller.User?.FindFirst("sub")?.Value ??
                     controller.User?.FindFirst("userId")?.Value ??
                     "Unknown";

        // Audit log: Track who is attempting permanent deletion
        logger.LogWarning("PURGE attempt by User: {UserName} (ID: {UserId}) on {EntityType} with ID: {EntityId} at {Timestamp}",
            userName, userId, typeof(TEntity).Name, id, DateTime.UtcNow);

        try
        {
            var result = await cleanupService.ForceHardDeleteAsync<TEntity, TKey>(id, cancellationToken);

            if (result)
            {
                // Audit log: Successful permanent deletion
                logger.LogWarning("PURGE completed successfully by User: {UserName} (ID: {UserId}) on {EntityType} with ID: {EntityId} at {Timestamp}",
                    userName, userId, typeof(TEntity).Name, id, DateTime.UtcNow);

                return controller.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Record permanently deleted"
                });
            }
            else
            {
                // Audit log: Entity not found
                logger.LogWarning("PURGE failed - entity not found. User: {UserName} (ID: {UserId}) attempted to delete {EntityType} with ID: {EntityId}",
                    userName, userId, typeof(TEntity).Name, id);

                return controller.Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Record not found"
                });
            }
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            logger.LogWarning(ex, "PURGE blocked - FK constraint violation. User: {UserName} (ID: {UserId}) attempted to delete {EntityType} with ID: {EntityId}",
                userName, userId, typeof(TEntity).Name, id);

            return controller.Conflict(new ApiResponse<object>
            {
                Success = false,
                Message = "Cannot delete this record because it is still referenced by other entities. Please remove dependent records first."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PURGE failed with error. User: {UserName} (ID: {UserId}) on {EntityType} with ID: {EntityId}",
                userName, userId, typeof(TEntity).Name, id);

            return controller.StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    /// <summary>
    /// Determines if a DbUpdateException is caused by a foreign key constraint violation.
    /// </summary>
    private static bool IsForeignKeyViolation(DbUpdateException ex)
    {
        // Check for SQL Server foreign key violation
        if (ex.InnerException is SqlException sqlException)
        {
            // SQL Server error codes:
            // 547 = Foreign key constraint violation (DELETE/UPDATE)
            return sqlException.Number == 547;
        }

        // Fallback: check error message for common foreign key constraint patterns
        var errorMessage = ex.InnerException?.Message ?? ex.Message;
        return errorMessage.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("conflicted with the FOREIGN KEY", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Bulk Operations

    public static async Task<IActionResult> ExecuteBulkCreate<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TCreateDto[] items,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        if (items == null || items.Length == 0)
        {
            return controller.BadRequest(new ApiResponse<BulkResult<TDto>>
            {
                Success = false,
                Message = "No items provided for Bulk create."
            });
        }

        try
        {
            var result = await service.BulkCreateAsync(items, cancellationToken);
            return controller.Ok(new ApiResponse<BulkResult<TDto>>
            {
                Success = result.AllSucceeded,
                Message = result.HasFailures
                    ? $"{result.SuccessCount} records created, {result.FailedCount} failed"
                    : $"{result.SuccessCount} records created successfully",
                Items = result,
                Errors = result.Errors?.ToList()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bulk create operation failed for {Count} items", items.Length);
            var errorMessage = ex.InnerException?.Message ?? ex.Message;

            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return controller.Conflict(new ApiResponse<BulkResult<TDto>>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            return controller.StatusCode(500, new ApiResponse<BulkResult<TDto>>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    public static async Task<IActionResult> ExecuteBulkUpdate<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        BulkUpdateItem<TKey, TUpdateDto>[] items,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        if (items == null || items.Length == 0)
        {
            return controller.BadRequest(new ApiResponse<BulkResult<TDto>>
            {
                Success = false,
                Message = "No items provided for Bulk update."
            });
        }

        try
        {
            var result = await service.BulkUpdateAsync(items, cancellationToken);
            return controller.Ok(new ApiResponse<BulkResult<TDto>>
            {
                Success = result.AllSucceeded,
                Message = $"{result.SuccessCount} records updated, {result.FailedCount} failed",
                Items = result,
                Errors = result.Errors?.ToList()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bulk update operation failed for {Count} items", items.Length);
            var errorMessage = ex.InnerException?.Message ?? ex.Message;

            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return controller.Conflict(new ApiResponse<BulkResult<TDto>>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            return controller.StatusCode(500, new ApiResponse<BulkResult<TDto>>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    public static async Task<IActionResult> ExecuteCreateFromRange<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        RangeCreateRequest<TCreateDto> request,
        Func<TCreateDto, string, int, TCreateDto> transformer,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        if (request == null)
        {
            return controller.BadRequest(new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = "No request provided for Range create."
            });
        }

        if (string.IsNullOrWhiteSpace(request.RangeFrom) || string.IsNullOrWhiteSpace(request.RangeTo))
        {
            return controller.BadRequest(new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = "RangeFrom and RangeTo are required."
            });
        }

        try
        {
            var result = await service.CreateFromRangeAsync(request, transformer, cancellationToken);
            return controller.Ok(new ApiResponse<RangeResult<TDto>>
            {
                Success = result.AllSucceeded,
                Message = result.HasFailures ? $"{result.SuccessCount} records created, {result.FailedCount} failed" : $"{result.SuccessCount} records created successfully from range",
                Items = result,
                Errors = result.Errors?.ToList()
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid range parameters: {Message}", ex.Message);
            return controller.BadRequest(new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Range create operation failed");
            var errorMessage = ex.InnerException?.Message ?? ex.Message;

            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return controller.Conflict(new ApiResponse<RangeResult<TDto>>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            return controller.StatusCode(500, new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }
    //new added method to support range create without transformer, for simpler use cases where no transformation logic is needed. The service will handle default transformation if transformer is not provided.  
    public static async Task<IActionResult> ExecuteCreateFromRange<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        RangeCreateRequest<TCreateDto> request,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        if (request == null)
        {
            return controller.BadRequest(new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = "No request provided for Range create."
            });
        }

        if (string.IsNullOrWhiteSpace(request.RangeFrom) || string.IsNullOrWhiteSpace(request.RangeTo))
        {
            return controller.BadRequest(new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = "RangeFrom and RangeTo are required."
            });
        }

        try
        {
            var result = await ((dynamic)service).CreateFromRangeAsync(request, cancellationToken);
            return controller.Ok(new ApiResponse<RangeResult<TDto>>
            {
                Success = result.AllSucceeded,
                Message = result.HasFailures ? $"{result.SuccessCount} records created, {result.FailedCount} failed" : $"{result.SuccessCount} records created successfully from range",
                Items = result,
                Errors = result.Errors?.ToList()
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid range parameters: {Message}", ex.Message);
            return controller.BadRequest(new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Range create operation failed");
            var errorMessage = ex.InnerException?.Message ?? ex.Message;

            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return controller.Conflict(new ApiResponse<RangeResult<TDto>>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            return controller.StatusCode(500, new ApiResponse<RangeResult<TDto>>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }


    public static async Task<IActionResult> ExecuteBulkDelete<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TKey[] ids,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
        where TCreateDto : class
        where TUpdateDto : class
    {
        if (ids == null || ids.Length == 0)
        {
            return controller.BadRequest(new ApiResponse<BulkResult<TKey>>
            {
                Success = false,
                Message = "No IDs provided for Bulk delete."
            });
        }

        try
        {
            var result = await service.BulkDeleteAsync(ids, cancellationToken);

            return controller.Ok(new ApiResponse<BulkResult<TKey>>
            {
                Success = result.AllSucceeded,
                Message = $"{result.SuccessCount} records deleted, {result.FailedCount} not found",
                Items = result,
                Errors = result.Errors?.ToList()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bulk delete operation failed for {Count} ids", ids.Length);
            return controller.StatusCode(500, new ApiResponse<BulkResult<TKey>>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    /// <summary>
    /// Execute permanent bulk delete operation through the centralized cleanup service.
    /// This is an irreversible operation and should be used with extreme caution.
    /// Routes through HardDeleteCleanupService to ensure consistent policy enforcement.
    /// Logs actor identity for audit trail.
    /// </summary>
    public static async Task<IActionResult> ExecuteBulkForceDelete<TEntity, TKey>(
        this ControllerBase controller,
        IHardDeleteCleanupService cleanupService,
        TKey[] ids,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        if (ids == null || ids.Length == 0)
        {
            return controller.BadRequest(new ApiResponse<BulkResult<TKey>>
            {
                Success = false,
                Message = "No IDs provided for Bulk purge."
            });
        }

        var userName = controller.User?.Identity?.Name ?? "Anonymous";
        var userId = controller.User?.FindFirst("sub")?.Value ??
                     controller.User?.FindFirst("userId")?.Value ??
                     "Unknown";

        // Audit log: Track who is attempting permanent bulk deletion
        logger.LogWarning("BULK PURGE attempt by User: {UserName} (ID: {UserId}) on {EntityType} with {Count} IDs at {Timestamp}",
            userName, userId, typeof(TEntity).Name, ids.Length, DateTime.UtcNow);

        try
        {
            var result = await cleanupService.BulkForceHardDeleteAsync<TEntity, TKey>(ids, cancellationToken);

            // Audit log: Bulk purge completed
            logger.LogWarning("BULK PURGE completed by User: {UserName} (ID: {UserId}) on {EntityType}. Success: {SuccessCount}, Failed: {FailedCount} at {Timestamp}",
                userName, userId, typeof(TEntity).Name, result.SuccessCount, result.FailedCount, DateTime.UtcNow);

            return controller.Ok(new ApiResponse<BulkResult<TKey>>
            {
                Success = result.AllSucceeded,
                Message = result.HasFailures
                    ? $"{result.SuccessCount} records permanently deleted, {result.FailedCount} failed"
                    : $"{result.SuccessCount} records permanently deleted successfully",
                Items = result,
                Errors = result.Errors?.ToList()
            });
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            logger.LogWarning(ex, "BULK PURGE blocked - FK constraint violation. User: {UserName} (ID: {UserId}) attempted to delete {EntityType}",
                userName, userId, typeof(TEntity).Name);

            return controller.Conflict(new ApiResponse<BulkResult<TKey>>
            {
                Success = false,
                Message = "Cannot delete one or more records because they are still referenced by other entities. Please remove dependent records first."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BULK PURGE failed with error. User: {UserName} (ID: {UserId}) on {EntityType}",
                userName, userId, typeof(TEntity).Name);

            return controller.StatusCode(500, new ApiResponse<BulkResult<TKey>>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    #endregion
}
