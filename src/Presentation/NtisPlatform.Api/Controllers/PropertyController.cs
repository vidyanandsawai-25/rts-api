using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Core Property Aggregate API - Provides property search and lookup functionality.
/// Used across multiple features (ApplyTaxes, BillGeneration, Reports, etc.).
/// </summary>
/// <remarks>
/// Unlike other simple master data controllers (e.g. BankMaster, Ward, Zone) that live under
/// Controllers/Master, the Property aggregate is a core, cross-cutting domain concept used
/// by multiple bounded contexts and workflows. For this reason, it is intentionally exposed
/// as a root-level API at route <c>/api/Property</c> rather than being grouped under the
/// Master controllers folder.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class PropertyController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly ILogger<PropertyController> _logger;

    /// <summary>
    /// Constructor follows codebase convention: Service dependencies first, then infrastructure.
    /// </summary>
    public PropertyController(
        IPropertyService propertyService,
        ILogger<PropertyController> logger)
    {
        _propertyService = propertyService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyQueryParameters query, CancellationToken ct)
        => this.ExecuteGetAllPaged(_propertyService, query, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_propertyService, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyService, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_propertyService, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_propertyService, id, _logger, ct);
}
