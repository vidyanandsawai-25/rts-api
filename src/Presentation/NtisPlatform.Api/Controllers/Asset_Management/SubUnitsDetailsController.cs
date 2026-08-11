using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Asset_Management;
[Authorize]
[ApiController]
// Route kept as the pre-existing "AssetFloorDetails" path — this is the AMS.SubUnitsDetails
// CRUD surface, but the frontend already calls /api/AssetFloorDetails/... extensively;
// renaming the C# types shouldn't force a matching public API/URL change.
[Route("api/AssetFloorDetails")]

public class SubUnitsDetailsController : ControllerBase
{
    private readonly ILogger<SubUnitsDetailsController> _logger;
    private readonly ISubUnitsDetailsService _service;

    public SubUnitsDetailsController(
        ILogger<SubUnitsDetailsController> logger,
        ISubUnitsDetailsService service)
    {
        _logger = logger;
        _service = service;
    }

    /// <summary>
    /// Gets sub-unit details by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubUnitsDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Gets all floor details for a specific asset with totals.
    /// </summary>
    [HttpGet("by-asset/{assetId}")]
    [ProducesResponseType(typeof(ApiResponse<SubUnitsDetailsSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAssetId(int assetId, CancellationToken ct)
    {
        var summary = await _service.GetByAssetIdAsync(assetId, ct);
        return Ok(new ApiResponse<SubUnitsDetailsSummaryDto>
        {
            Success = true,
            Message = "Floor details retrieved successfully",
            Items = summary
        });
    }

    /// <summary>
    /// Creates a new sub-unit detail row.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SubUnitsDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SubUnitsDetailsDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateSubUnitsDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Updates an existing sub-unit detail row.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SubUnitsDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SubUnitsDetailsDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<SubUnitsDetailsDto>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateSubUnitsDetailsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);
}
