using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyMapDetails;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertyMappingController : ControllerBase
{
    private readonly IPropertyMappingService _propertyService;
    private readonly ILogger<PropertyMappingController> _logger;

    public PropertyMappingController(IPropertyMappingService propertyService,ILogger<PropertyMappingController> logger)
    {
        _propertyService = propertyService;
        _logger = logger;
    }

    [HttpPost("map-details")]
    public Task<IActionResult> AddPropertyMapDetails([FromBody] CreatePropertyMapDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyService, createDto, _logger, ct);

    [HttpPut("map-details")]
    public Task<IActionResult> UpdatePropertyMapDetails([FromBody] UpdatePropertyMapDetailsDto dto, CancellationToken ct)
        => this.ExecuteUpdate(_propertyService, dto.PropertyId, dto, _logger, ct);
}
