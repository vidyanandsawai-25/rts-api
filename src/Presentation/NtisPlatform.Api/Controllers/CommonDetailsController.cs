using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for dynamic bulk property common details update operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommonDetailsController : ControllerBase
{
    private readonly ICommonDetailsService _service;
    private readonly FileValidationHelper _fileValidationHelper;

    public CommonDetailsController(ICommonDetailsService service, FileValidationHelper fileValidationHelper)
    {
        _service = service;
        _fileValidationHelper = fileValidationHelper;
    }

    [HttpGet("master")]
    public async Task<IActionResult> GetMaster(CancellationToken ct)
    {
        var result = await _service.GetMenuAsync(ct);
        return Ok(new ApiResponse<List<BulkUpdateMasterDto>>
        {
            Success = true,
            Items = result
        });
    }

    [HttpGet("form-fields/{updateCode}")]
    public async Task<IActionResult> GetFormFields([FromRoute, Required(AllowEmptyStrings = false)] string updateCode,CancellationToken ct)
    {

        var result = await _service.GetFormFieldsAsync(updateCode, ct);
        return Ok(new ApiResponse<List<BulkUpdateFieldConfigDto>>
        {
            Success = true,
            Items = result
        });
    }

    [HttpGet("grid-columns/{updateCode}")]
    public async Task<IActionResult> GetGridColumns([FromRoute, Required(AllowEmptyStrings = false)] string updateCode,CancellationToken ct)
    {
        var result = await _service.GetGridColumnsAsync(updateCode, ct);
        return Ok(new ApiResponse<List<PreviewGridColumnDto>>
        {
            Success = true,
            Items = result
        });
    }

    [HttpGet("source-tables")]
    public async Task<IActionResult> GetSourceTables(CancellationToken ct)
    {
        var result = await _service.GetSourceTablesAsync(ct);
        return Ok(new ApiResponse<List<SourceTableLookupDto>>
        {
            Success = true,
            Items = result
        });
    }

    [HttpGet("source-table-fields/{sourceTableId}")]
    public async Task<IActionResult> GetSourceTableFields([FromRoute] int sourceTableId, CancellationToken ct)
    {
        var result = await _service.GetSourceTableFieldsAsync(sourceTableId, ct);
        return Ok(new ApiResponse<List<SourceTableFieldLookupDto>>
        {
            Success = true,
            Items = result
        });
    }

    [HttpPost("bulk-update-definitions")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBulkUpdateDefinition(
        [FromBody] CreateBulkUpdateDefinitionFromSourceDto request, CancellationToken ct)
    {
        int userId;
        try
        {
            userId = GetValidatedUserId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<BulkUpdateDefinitionResultDto>
            {
                Success = false,
                Message = ex.Message
            });
        }

        try
        {
            var result = await _service.CreateFromSourceTableAsync(request, userId, ct);
            return Ok(new ApiResponse<BulkUpdateDefinitionResultDto>
            {
                Success = true,
                Items = result,
                Message = $"Bulk update definition '{result.Master.UpdateCode}' created with {result.FieldConfigs.Count} field(s)."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<BulkUpdateDefinitionResultDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("filter-properties")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FilterProperties([FromQuery] FilterPropertiesRequestDto request,CancellationToken ct)
    {
        try
        {
            var result = await _service.FilterPropertiesAsync(request, ct);
            return Ok(new ApiResponse<PagedResult<PropertyPreviewDto>>
            {
                Success = true,
                Items = result,
                Message = $"{result.TotalCount} properties found"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<PagedResult<PropertyPreviewDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("filter-properties-by-category")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FilterPropertiesByCategory([FromQuery] FilterPropertiesByCategoryRequestDto request, CancellationToken ct)
    {
        try
        {
            var result = await _service.FilterPropertiesByCategoryAsync(request, ct);
            return Ok(new ApiResponse<PagedResult<PropertyPreviewDto>>
            {
                Success = true,
                Items = result,
                Message = $"{result.TotalCount} properties found"
            });
        }
        catch (PropertyValidationException ex)
        {
            return BadRequest(new ApiResponse<PagedResult<PropertyPreviewDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<PagedResult<PropertyPreviewDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("update")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update([FromBody] List<BulkUpdateRequestDto> requests, CancellationToken ct)
    {
        int userId;
        try
        {
            userId = GetValidatedUserId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<List<BulkUpdateResultDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }

        if (requests is null || requests.Count == 0)
        {
            return BadRequest(new ApiResponse<List<BulkUpdateResultDto>>
            {
                Success = false,
                Message = "At least one update item is required."
            });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var results = await _service.BulkUpdateBatchAsync(requests, userId, ipAddress, ct);

            var totalSuccess = results.Sum(r => r.SuccessCount);
            var totalFailed = results.Sum(r => r.FailedCount);
            var allErrors = results.SelectMany(r => r.Errors.Select(e => $"[{r.UpdateCode}] {e}")).ToList();

            return Ok(new ApiResponse<List<BulkUpdateResultDto>>
            {
                Success = totalFailed == 0,
                Message = totalFailed == 0
                    ? $"Processed {results.Count} update item(s): {totalSuccess} properties updated successfully"
                    : $"Processed {results.Count} update item(s): {totalSuccess} succeeded, {totalFailed} failed",
                Items = results,
                Errors = allErrors.Count > 0 ? allErrors : null
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<List<BulkUpdateResultDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("export-excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportExcel([FromQuery] ExportPropertiesRequestDto request, CancellationToken ct)
    {
        try
        {
            var bytes = await _service.ExportPropertiesToExcelAsync(request, ct);
            var fileName = $"{request.UpdateCode}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("import-excel")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ImportExcel([FromForm] ExcelImportFormDto form, CancellationToken ct)
    {
        int userId;
        try
        {
            userId = GetValidatedUserId();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<BulkUpdateResultDto>
            {
                Success = false,
                Message = ex.Message
            });
        }

        if (form.File is null || form.File.Length == 0)
            return BadRequest(new ApiResponse<BulkUpdateResultDto> { Success = false, Message = "File is required" });

        if (!_fileValidationHelper.IsValidFileType(form.File.ContentType, form.File.FileName))
            return BadRequest(new ApiResponse<BulkUpdateResultDto>
            {
                Success = false,
                Message = _fileValidationHelper.GetInvalidFileTypeMessage()
            });

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await using var stream = form.File.OpenReadStream();
            var result = await _service.ImportPropertiesFromExcelAsync(form.UpdateCode, stream, userId, ipAddress, ct);

            return Ok(new ApiResponse<BulkUpdateResultDto>
            {
                Success = result.FailedCount == 0,
                Message = result.FailedCount == 0
                    ? $"Updated {result.SuccessCount} properties successfully"
                    : $"Update failed: {result.FailedCount} row error(s)",
                Items = result,
                Errors = result.Errors.Count > 0 ? result.Errors : null
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<BulkUpdateResultDto> { Success = false, Message = ex.Message });
        }
    }

    [HttpGet("update-history")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUpdateHistory([FromQuery] UpdateHistoryQueryParameters request, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetUpdateHistoryAsync(request, ct);
            return Ok(new ApiResponse<PagedResult<UpdateHistoryDto>>
            {
                Success = true,
                Items = result,
                Message = $"{result.TotalCount} update history record(s) found"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }    
    }

    [HttpGet("update-history/export-excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportUpdateHistoryExcel([FromQuery] UpdateHistoryQueryParameters request, CancellationToken ct)
    {
        try
        {
            var bytes = await _service.ExportUpdateHistoryToExcelAsync(request, ct);
            var fileName = $"UpdateHistory_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
    }

    private int GetValidatedUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Valid user identification is required.");
        return id;
    }

}
