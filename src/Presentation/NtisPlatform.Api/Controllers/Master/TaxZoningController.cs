using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaxZoningController : ControllerBase
{
    private readonly ITaxZoningService _service;
    private readonly ILogger<TaxZoningController> _logger;

    public TaxZoningController(ITaxZoningService service, ILogger<TaxZoningController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action,string operationName,Func<T, bool>? shouldReturnNotFound = null)
    {
        try
        {
            var result = await action();

            if (shouldReturnNotFound?.Invoke(result) == true)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "No matching records found.",
                    Items = null
                });
            }

            return Ok(new ApiResponse<T>
            {
                Success = true,
                Message = $"{operationName} completed successfully.",
                Items = result
            });
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "{Operation} validation error: {Message}", operationName, ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Operation} failed", operationName);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TaxZoningDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] TaxZoningQueryParameters queryParams, CancellationToken ct)
    {
        return await ExecuteAsync(
            async () =>
            {
                return string.Equals(queryParams.GroupBy, "ward", StringComparison.OrdinalIgnoreCase)
                    ? await _service.GetFromToPropertyNo(queryParams, ct)
                    : await _service.GetAllPropertyNo(queryParams, ct);
            },
            "Get tax zoning data");
    }    

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<TaxZoningDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update([FromBody] UpdateTaxZoningDto dto, CancellationToken ct)
    {
        return await ExecuteAsync(
            async () => await _service.UpdateAsync(dto, ct),
            "Update tax zoning",
            shouldReturnNotFound: result => result == null
        );
    }
}