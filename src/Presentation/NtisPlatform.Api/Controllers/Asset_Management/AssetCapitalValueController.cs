using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.AssetCapitalValue;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Asset_Management;

/// <summary>
/// Exposes the existing <see cref="IAssetCapitalValueService"/> calculation methods used by the
/// asset Valuation step (building / open-plot / movable). This is HTTP wiring only — the CV
/// calculation logic lives entirely in the service.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssetCapitalValueController : ControllerBase
{
    private readonly IAssetCapitalValueService _service;
    private readonly ILogger<AssetCapitalValueController> _logger;

    public AssetCapitalValueController(
        IAssetCapitalValueService service,
        ILogger<AssetCapitalValueController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Calculate capital value for a single asset (shop/unit/office) — the AMS.SubUnitsDetails row(s)
    /// belonging to this specific AssetId. Pass SubUnitsDetailsId to target one exact row (e.g. the
    /// unit just saved via ManageSubUnits/create); leave it 0 to calculate every floor detail the
    /// asset owns. Used by the Add-Asset floor-details "Save Unit" step to calculate and persist CV
    /// for that one unit, separately from the building-wide rollup below.
    /// </summary>
    [HttpPost("unit/calculate-cv")]
    [ProducesResponseType(typeof(AssetCVSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateUnitCV([FromBody] CalculateAssetCVRequestDto request, CancellationToken ct)
    {
        var result = await _service.CalculateAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Calculate capital value for a building (parent asset) including its floor details and child assets.
    /// </summary>
    [HttpPost("building/calculate-cv")]
    [ProducesResponseType(typeof(BuildingCVSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateBuildingCV([FromBody] CalculateBuildingCVRequestDto request, CancellationToken ct)
    {
        var result = await _service.CalculateBuildingCVAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Calculate capital value for an open-plot asset using LandAreaSqMeter from AMS.AssetDetails.
    /// </summary>
    [HttpPost("plot/calculate-cv")]
    [ProducesResponseType(typeof(PlotCVSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculatePlotCV([FromBody] CalculatePlotCVRequestDto request, CancellationToken ct)
    {
        var result = await _service.CalculatePlotCVAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Calculate capital value for a movable asset (vehicle / equipment / furniture) via depreciation.
    /// </summary>
    [HttpPost("movable/calculate-cv")]
    [ProducesResponseType(typeof(MovableAssetCVResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateMovableAssetCV([FromBody] CalculateMovableAssetCVRequestDto request, CancellationToken ct)
    {
        var result = await _service.CalculateMovableAssetCVAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Read-only valuation rollup of a parent asset: its own base value plus the already-calculated
    /// CV of all its sub-units (child assets) and inventory batches. Purely additive over previously
    /// calculated values — does not calculate or persist anything.
    /// </summary>
    [HttpGet("parent/{parentAssetId}/valuation")]
    [ProducesResponseType(typeof(ParentAssetValuationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParentAssetValuation(long parentAssetId, CancellationToken ct)
    {
        var result = await _service.GetParentAssetValuationAsync(parentAssetId, ct);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
