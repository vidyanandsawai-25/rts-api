using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyMergeSingle;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertyMergeSingleController : ControllerBase
{
    private readonly IPropertyMergeSingleService _propertyMergeSingleService;
    private readonly ILogger<PropertyMergeSingleController> _logger;

    public PropertyMergeSingleController(IPropertyMergeSingleService propertyMergeSingleService, ILogger<PropertyMergeSingleController> logger)
    {
        _propertyMergeSingleService = propertyMergeSingleService;
        _logger = logger;
    }

    [HttpPost]
    public Task<IActionResult> PropertyMergeSingleCreateAsync([FromBody] CreatePropertyMergeSingleDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyMergeSingleService, createDto, _logger, ct);

    [HttpPut]
    public Task<IActionResult> PropertyMergeSingleUpdateAsync([FromBody] UpdatePropertyMergeSingleDto dto, CancellationToken cancellationToken = default)
        => this.ExecuteUpdate(_propertyMergeSingleService, dto.PropertyId, dto, _logger, cancellationToken);
}
