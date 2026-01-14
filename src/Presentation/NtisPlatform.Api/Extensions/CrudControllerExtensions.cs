using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Diagnostics.Metrics;

namespace NtisPlatform.Api.Extensions;

public static class CrudControllerExtensions
{
    public static async Task<IActionResult> ExecuteGetAllPaged<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TQueryParams queryParameters,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
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
        catch (Exception ex)
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
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while processing your request."
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
        catch (Exception ex)
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
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });

        }
    }

    public static async Task<IActionResult> ExecuteDelete<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>(
        this ControllerBase controller,
        ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey> service,
        TKey id,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TQueryParams : BaseQueryParameters
    {
        try
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result ? controller.Ok(new ApiResponse<TDto>
            {
                Success = true,
                Message = "Record deleted"
            }) :
            controller.Ok(new ApiResponse<TDto>
            {
                Success = false,
                Message = "Record not found to delete"
            });


        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete operation failed for id: {Id}", id);
            return controller.StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }
}
