using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WaterConnectionDetailsController : ControllerBase
{
    private readonly IWaterConnectionDetailsService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<WaterConnectionDetailsController> _logger;

    public WaterConnectionDetailsController(
        IWaterConnectionDetailsService service,
        IHardDeleteCleanupService cleanupService,
        ILogger<WaterConnectionDetailsController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] WaterConnectionDetailsQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateWaterConnectionDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateWaterConnectionDetailsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<WaterConnectionDetailsEntity, int>(_cleanupService, id, _logger, ct);

    /// <summary>
    /// Generates a pro-rata water bill for the specified connection and financial year.
    /// Uses the billing rules: ChargeMonths from MAX(ConnectionStartDate, FYStart)
    /// to MIN(ConnectionStopDate, FYEnd), inclusive of both months.
    /// Returns 204 when no bill applies (connection inactive for the selected year).
    /// </summary>
    [HttpPost("generate-bill")]
    public async Task<IActionResult> GenerateBill(
        [FromBody] GenerateBillRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.GenerateBillAsync(request.WaterConnectionId, request.FinanceYearId, ct);

            if (result == null)
                return NoContent();

            return Ok(new ApiResponse<WaterConnectionDetailsDto>
            {
                Success = true,
                Message = "Bill generated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bill generation failed: {Message}", ex.Message);
            return BadRequest(new ApiResponse<WaterConnectionDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateBill failed for connection {ConnectionId}, year {YearId}",
                request.WaterConnectionId, request.FinanceYearId);
            return StatusCode(500, new ApiResponse<WaterConnectionDetailsDto>
            {
                Success = false,
                Message = "An error occurred while generating the bill."
            });
        }
    }
}

public class GenerateBillRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "WaterConnectionId must be a positive integer.")]
    public int WaterConnectionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "FinanceYearId must be a positive integer.")]
    public int FinanceYearId { get; set; }
}
