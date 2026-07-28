using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class PropertyMapMasterController : ControllerBase
{
    private readonly IPropertyMapMasterService _service;
    private readonly ILogger<PropertyMapMasterController> _logger;

    public PropertyMapMasterController(IPropertyMapMasterService service, ILogger<PropertyMapMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyMapQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyMapMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyMapMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpGet("mapped-properties")]
    public async Task<IActionResult> GetMappedProperties([FromQuery] PropertyMapDetailQueryParameters queryParameters, CancellationToken ct)
    {
        var result = await _service.GetMappedPropertiesAsync(queryParameters, ct);
        return Ok(result);
    }

    /// <summary>
    /// Searches across up to 10 fields (6 old-property + 4 new-property).
    /// Returns:
    ///   - mappedProperties  — paged pairs already linked in PropertyMapDetail (old + new blocks)
    ///   - oldPropertySuggestions — up to 20 unlinked old-property candidates sorted by match %
    ///   - newPropertySuggestions — up to 20 unlinked new-property candidates sorted by match %
    ///
    /// Each result carries matchPercentage (0–100) and mappingDecision (AUTO_MAP / MANUAL_REVIEW / LOW_MATCH).
    /// Match % = (fields that matched / fields caller provided) × 100.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchPropertyMappings(
        [FromQuery] PropertyMapDetailQueryParameters queryParameters,
        CancellationToken ct)
    {
        var result = await _service.SearchPropertyMappingsAsync(queryParameters, ct);
        return Ok(result);
    }
}
