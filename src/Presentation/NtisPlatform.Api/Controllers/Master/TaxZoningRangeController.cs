using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Ward + property-number-range/whole-ward tax zone assignment. Brand-new feature, independent of
/// the existing <c>TaxZoningController</c>/<c>TaxZoningService</c> (which is left untouched).
/// </summary>
[ApiController]
[Route("api/tax-zoning-ranges")]
[Authorize]
public class TaxZoningRangeController : ControllerBase
{
    private readonly ITaxZoningRangeService _service;
    private readonly ILogger<TaxZoningRangeController> _logger;
    private readonly IUlbConfigService _ulbConfigService;

    public TaxZoningRangeController(ITaxZoningRangeService service, ILogger<TaxZoningRangeController> logger, IUlbConfigService ulbConfigService)
    {
        _service = service;
        _logger = logger;
        _ulbConfigService = ulbConfigService;
    }

    private async Task<string> GetUlbNameAsync(CancellationToken ct)
    {
        try
        {
            var config = await _ulbConfigService.GetUlbConfigAsync(ct);
            // Prefer local (Marathi) name for display; fall back to English name then code
            if (!string.IsNullOrWhiteSpace(config?.UlbNameLocal)) return config.UlbNameLocal;
            if (!string.IsNullOrWhiteSpace(config?.UlbName)) return config.UlbName;
            if (!string.IsNullOrWhiteSpace(config?.UlbCode)) return config.UlbCode;
            return "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch ULB name for Excel export");
            return "";
        }
    }

    private async Task<string> GetUlbCodeForFileNameAsync(CancellationToken ct)
    {
        try
        {
            var config = await _ulbConfigService.GetUlbConfigAsync(ct);
            // Use English name or code for the filename (no special chars)
            var raw = (!string.IsNullOrWhiteSpace(config?.UlbName) ? config.UlbName
                     : !string.IsNullOrWhiteSpace(config?.UlbCode) ? config.UlbCode
                     : "");
            // Strip characters invalid in filenames
            return string.Concat(raw.Split(System.IO.Path.GetInvalidFileNameChars()))
                         .Replace(" ", "")
                         .TrimEnd('_');
        }
        catch
        {
            return "";
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TaxZoningRangeQueryParameters queryParameters, CancellationToken ct)
    {
        var result = await _service.GetAllAsync(queryParameters, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaxZoningRangeDto createDto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(createDto, ct);
        return Ok(new ApiResponse<IReadOnlyList<TaxZoningRangeDto>>
        {
            Success = true,
            Message = "Record inserted successfully",
            Items = result
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaxZoningRangeDto updateDto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, updateDto, ct);
        return result == null
            ? Ok(new ApiResponse<TaxZoningRangeDto> { Success = false, Message = "Record not found for update" })
            : Ok(new ApiResponse<TaxZoningRangeDto> { Success = true, Message = "Record updated successfully", Items = result });
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Bulk([FromBody] BulkTaxZoningRangeRequest request, CancellationToken ct)
    {
        if (request == null || request.Items.Count == 0)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "No items provided for bulk import." });
        }

        var result = await _service.BulkUpsertAsync(request, ct);
        return Ok(new ApiResponse<object>
        {
            Success = result.AllSucceeded,
            Message = result.HasFailures
                ? $"{result.SuccessCount} records created, {result.FailedCount} failed"
                : $"{result.SuccessCount} records created successfully",
            Items = result,
            Errors = result.Errors?.ToList()
        });
    }

    [HttpGet("coverage")]
    public async Task<IActionResult> Coverage([FromQuery] List<int>? wardIds, CancellationToken ct)
    {
        var result = await _service.GetCoverageAsync(wardIds, ct);
        return Ok(new ApiResponse<object> { Success = true, Items = result });
    }

    [HttpGet("properties-by-ward")]
    public async Task<IActionResult> PropertiesByWard([FromQuery] WardPropertyQueryParameters queryParams, CancellationToken ct)
    {
        var result = await _service.GetPropertiesByWardAsync(queryParams, ct);
        return Ok(result);
    }

    [HttpGet("ward-abstract")]
    public async Task<IActionResult> WardAbstract([FromQuery] WardAbstractQueryParameters queryParams, CancellationToken ct)
    {
        var result = await _service.GetWardAbstractAsync(queryParams, ct);
        return Ok(result);
    }

    [HttpGet("ward-abstract/export-excel")]
    public async Task<IActionResult> ExportWardAbstractExcel([FromQuery] WardAbstractQueryParameters queryParams, [FromQuery] string? ulbName = null, CancellationToken ct = default)
    {
        // Display name (Marathi preferred) goes into the Excel header row
        var resolvedUlbName = !string.IsNullOrWhiteSpace(ulbName) ? ulbName : await GetUlbNameAsync(ct);
        // Filename uses a clean English/code identifier
        var ulbCode = await GetUlbCodeForFileNameAsync(ct);
        var fileLabel = string.IsNullOrWhiteSpace(ulbCode) ? "WardWise" : ulbCode;

        var bytes = await _service.ExportWardAbstractToExcelAsync(queryParams, resolvedUlbName, ct);
        var fileName = $"Ward_Abstract_{fileLabel}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportRangesExcel([FromQuery] TaxZoningRangeQueryParameters queryParams, [FromQuery] string? ulbName = null, CancellationToken ct = default)
    {
        // Display name (Marathi preferred) goes into the Excel header row
        var resolvedUlbName = !string.IsNullOrWhiteSpace(ulbName) ? ulbName : await GetUlbNameAsync(ct);
        // Filename uses a clean English/code identifier
        var ulbCode = await GetUlbCodeForFileNameAsync(ct);
        var fileLabel = string.IsNullOrWhiteSpace(ulbCode) ? "WardWise" : ulbCode;

        var bytes = await _service.ExportRangesToExcelAsync(queryParams, resolvedUlbName, ct);
        var fileName = $"TaxZoningRanges_{fileLabel}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("pending/export-excel")]
    public async Task<IActionResult> ExportPendingPropertiesExcel([FromQuery] int? wardId, [FromQuery] string? ulbName = null, CancellationToken ct = default)
    {
        // Display name (Marathi preferred) goes into the Excel header row
        var resolvedUlbName = !string.IsNullOrWhiteSpace(ulbName) ? ulbName : await GetUlbNameAsync(ct);
        // Filename uses a clean English/code identifier
        var ulbCode = await GetUlbCodeForFileNameAsync(ct);
        var fileLabel = string.IsNullOrWhiteSpace(ulbCode) ? "WardWise" : ulbCode;

        var bytes = await _service.ExportPendingPropertiesToExcelAsync(wardId, resolvedUlbName, ct);
        var fileName = $"PendingTaxZoning_{fileLabel}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("bulk-template")]
    [AllowAnonymous]
    public IActionResult DownloadBulkTemplate()
    {
        var bytes = _service.GenerateBulkTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Tax_Zoning_Bulk_Update_Template.xlsx");
    }
}
