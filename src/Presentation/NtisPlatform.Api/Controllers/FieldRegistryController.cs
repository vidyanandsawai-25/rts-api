using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.FieldRegistry;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FieldRegistryController : ControllerBase
{
    private readonly IFieldRegistryService _fieldRegistryService;
    private readonly ILogger<FieldRegistryController> _logger;

    public FieldRegistryController(IFieldRegistryService fieldRegistryService,ILogger<FieldRegistryController> logger)
    {
        _fieldRegistryService = fieldRegistryService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FieldRegistryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fieldRegistryService.GetAllAsync(cancellationToken);
            return Ok(new ApiResponse<IReadOnlyList<FieldRegistryDto>>
            {
                Success = true,
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field registry schemas");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving field registry schemas"
            });
        }
    }

    [HttpGet("GetDetailsBySchema")]
    [ProducesResponseType(typeof(PagedResult<FieldRegistryDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetailsBySchema([FromQuery] FieldRegistryDetailsQueryParameters queryParameters,CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fieldRegistryService.GetDetailsBySchemaAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field registry details for schema {SchemaName}", queryParameters.SchemaName);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An error occurred while retrieving field registry details"
            });
        }
    }

    [HttpGet("GetDetailsByTable")]
    [ProducesResponseType(typeof(PagedResult<FieldRegistryTableDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetailsByTable([FromQuery] FieldRegistryTableDetailsQueryParameters queryParameters,CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fieldRegistryService.GetDetailsByTableAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting field registry table details for schema {SchemaName} and table {TableName}",
                queryParameters.SchemaName,
                queryParameters.TableName);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An error occurred while retrieving field registry table details"
            });
        }
    }

    [HttpGet("GetFieldRegistries")]
    [ProducesResponseType(typeof(PagedResult<FieldRegistryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFieldRegistries([FromQuery] FieldRegistryQueryParameters queryParameters,CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fieldRegistryService.GetFieldRegistriesAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field registries");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An error occurred while retrieving field registries"
            });
        }
    }

    [HttpPost("AddFieldRegistry")]
    [ProducesResponseType(typeof(FieldRegistryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddFieldRegistry([FromBody] CreateFieldRegistryDto createDto,CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fieldRegistryService.AddFieldRegistryAsync(createDto, cancellationToken);
            return Ok(new ApiResponse<FieldRegistryResponseDto>
            {
                Success = true,
                Message = "Field registry created successfully",
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error adding field registry for UpdateCode {UpdateCode}", createDto.UpdateCode);
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding field registry for UpdateCode {UpdateCode}", createDto.UpdateCode);
             return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
             {
                 Success = false,
                 Message = "An error occurred while creating the field registry"
             });
        }
    }

    [HttpPut("UpdateFieldRegistry/{updateCode}")]
    [ProducesResponseType(typeof(ApiResponse<FieldRegistryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateFieldRegistry(string updateCode, [FromBody] UpdateFieldRegistryDto updateDto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fieldRegistryService.UpdateFieldRegistryAsync(updateCode, updateDto, cancellationToken);
            if (result is null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Field registry not found"
                });
            }

            return Ok(new ApiResponse<FieldRegistryResponseDto>
            {
                Success = true,
                Message = "Field registry updated successfully",
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating field registry for UpdateCode {UpdateCode}", updateCode);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating field registry for UpdateCode {UpdateCode}", updateCode);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while updating the field registry"
            });
        }
    }

    [HttpPatch("SetFieldRegistryStatus/{updateCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetFieldRegistryStatus(string updateCode,[FromQuery] bool isActive,[FromQuery] int? updatedBy,CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _fieldRegistryService.SetActiveStatusAsync(updateCode, isActive, updatedBy, cancellationToken);
            if (!updated)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Field registry not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = isActive ? "Field registry activated successfully" : "Field registry deactivated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for field registry {UpdateCode}", updateCode);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "An error occurred while updating the field registry status"
            });
        }
    }
}
