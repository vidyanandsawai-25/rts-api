using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Interfaces;

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

    [HttpPost]
    public Task<IActionResult> PropertyMergeCreateAsync([FromBody] CreatePropertyMergeDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyMergeService, createDto, _logger, ct);

    [HttpPut]
    public Task<IActionResult> PropertyMergeUpdateAsync([FromBody] UpdatePropertyMergeDto dto, CancellationToken cancellationToken = default)
        => this.ExecuteUpdate(_propertyMergeService, dto.PropertyId, dto, _logger, cancellationToken);

    [HttpGet("{propertyId}")]
    public Task<IActionResult> GetPropertyMergeDetailsById(int propertyId, CancellationToken cancellationToken = default)
    => this.ExecuteGetById(_propertyMergeService, propertyId, _logger, cancellationToken);
}

