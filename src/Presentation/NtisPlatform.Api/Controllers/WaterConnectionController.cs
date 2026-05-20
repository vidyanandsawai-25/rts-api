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
public class WaterConnectionController : ControllerBase
{
    private readonly IWaterConnectionService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<WaterConnectionController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;
    public WaterConnectionController(IWaterConnectionService service, IHardDeleteCleanupService cleanupService, IReferenceValidationService referenceValidationService, ILogger<WaterConnectionController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] WaterConnectionQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Returns a water connection with ApplicableRate and ApplicableCharges populated for the
    /// given financeYearId. When financeYearId is omitted the current financial year is used.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int? financeYearId, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetByIdWithFinanceYearAsync(id, financeYearId, ct);
            return result == null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetById failed for WaterConnection {Id}", id);
            return StatusCode(500, new ApiResponse<WaterConnectionDto>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateWaterConnectionDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateWaterConnectionDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<WaterConnectionMasterEntity, int>(_cleanupService, _referenceValidationService,id, _logger, ct);
}
