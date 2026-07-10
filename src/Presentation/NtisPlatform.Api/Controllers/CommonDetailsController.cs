using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.CommonDetails;
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

    [HttpPut("update")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update([FromBody] BulkUpdateRequestDto request,CancellationToken ct)
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

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _service.BulkUpdateAsync(request, userId, ipAddress, ct);

            return Ok(new ApiResponse<BulkUpdateResultDto>
            {
                Success = result.FailedCount == 0,
                Message = result.FailedCount == 0
                    ? $"Updated {result.SuccessCount} properties successfully"
                    : $"Updated {result.SuccessCount}, failed {result.FailedCount}",
                Items = result,
                Errors = result.Errors.Count > 0 ? result.Errors : null
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<BulkUpdateResultDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("export-excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportExcel([FromQuery] FilterPropertiesRequestDto request, CancellationToken ct)
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

    private int GetValidatedUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Valid user identification is required.");
        return id;
    }

}
