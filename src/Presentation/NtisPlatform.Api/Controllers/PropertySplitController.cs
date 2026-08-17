using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertySplit;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertySplitController : ControllerBase
{
    private readonly IPropertySplitService _propertySplitService;
    private readonly ILogger<PropertySplitController> _logger;
    public PropertySplitController(IPropertySplitService propertySplitService, ILogger<PropertySplitController> logger)
    {
        _propertySplitService = propertySplitService;
        _logger = logger;
    }

    [HttpPost]
    public Task<IActionResult> PropertySplitCreateAsync([FromBody] CreatePropertySplitDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertySplitService, createDto, _logger, ct);

    [HttpPut]
    public Task<IActionResult> PropertySplitUpdateAsync([FromBody] UpdatePropertySplitDto dto, CancellationToken cancellationToken = default)
        => this.ExecuteUpdate(_propertySplitService, dto.PropertyOldId, dto, _logger, cancellationToken);

    [HttpGet]
    public Task<IActionResult> GetUnMergePropertyDetailsAsync([FromQuery] PropertySplitQueryParameters queryParameters, CancellationToken ct)
=> this.ExecuteGetAllPaged(_propertySplitService, queryParameters, _logger, ct);
}
