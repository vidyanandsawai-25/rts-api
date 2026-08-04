using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using PropertyMergeDto = NtisPlatform.Application.DTOs.PropertyMergeDetails.PropertyMergeDto;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertyMergeController : ControllerBase
{
    private readonly IPropertyMergeService _propertyMergeService;
    private readonly ILogger<PropertyMergeController> _logger;

    public PropertyMergeController(IPropertyMergeService propertyMergeService, ILogger<PropertyMergeController> logger)
    {
        _propertyMergeService = propertyMergeService;
        _logger = logger;
    }

    [HttpPost("merge")]
    public Task<IActionResult> MergePropertyAsync([FromBody] CreatePropertyMergeDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyMergeService, createDto, _logger, ct);

    [HttpPut("demerge")]
    public Task<IActionResult> DemergeProperty([FromBody] UpdatePropertyMergeDto dto, CancellationToken cancellationToken = default)
        => this.ExecuteUpdate(_propertyMergeService, dto.PropertyIds?.FirstOrDefault() ?? 0, dto, _logger, cancellationToken);

    [HttpGet("{propertyId}/merge-details")]
    public Task<IActionResult> GetPropertyMergeDetailsById(int propertyId, CancellationToken cancellationToken = default)
        => this.ExecuteGetById(_propertyMergeService, propertyId, _logger, cancellationToken);

    [HttpGet("unmerge-details")]
    public Task<IActionResult> GetUnMergePropertyDetailsAsync([FromQuery] PropertyMergeQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_propertyMergeService, queryParameters, _logger, ct);

    [HttpPost("merge-multiple")]
    [ProducesResponseType(typeof(ApiResponse<PropertyMergeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MergeMultiplePropertyAsync([FromBody] PropertyMergeMultipleDto request, CancellationToken ct)
    {
        var result = await _propertyMergeService.MergeMultiplePropertyAsync(request, ct);
        return Ok(new ApiResponse<PropertyMergeDto>
        {
            Success = result.Success,
            Message = result.Message,
            Items = result
        });
    }

    [HttpPost("demerge-multiple")]
    [ProducesResponseType(typeof(ApiResponse<PropertyMergeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DemergeMultiplePropertyAsync([FromBody] PropertyDemergeMultipleDto request, CancellationToken ct)
    {
        var result = await _propertyMergeService.DemergeMultiplePropertyAsync(request, ct);
        return Ok(new ApiResponse<PropertyMergeDto>
        {
            Success = result.Success,
            Message = result.Message,
            Items = result
        });
    }
}

