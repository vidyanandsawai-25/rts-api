using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyBulkMerge;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertyBulkMergeController : ControllerBase
{
    private readonly IPropertyBulkMergeService _propertyBulkMergeService;
    private readonly ILogger<PropertyBulkMergeController> _logger;

    public PropertyBulkMergeController(IPropertyBulkMergeService propertyBulkMergeService, ILogger<PropertyBulkMergeController> logger)
    {
        _propertyBulkMergeService = propertyBulkMergeService;
        _logger = logger;
    }

    [HttpPost]
    public Task<IActionResult> PropertyBulkMergeCreateAsync([FromBody] CreatePropertyBulkMergeDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyBulkMergeService, createDto, _logger, ct);

    [HttpPut]
    public Task<IActionResult> PropertyBulkMergeUpdateAsync([FromBody] UpdatePropertyBulkMergeDto dto, CancellationToken cancellationToken = default)
        => this.ExecuteUpdate(_propertyBulkMergeService, dto.PropertyIdList.First().PropertyId, dto, _logger, cancellationToken);
}
