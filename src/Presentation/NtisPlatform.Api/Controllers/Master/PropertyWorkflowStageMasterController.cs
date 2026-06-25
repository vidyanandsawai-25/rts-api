using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for Property Workflow Stage Master CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]

public class PropertyWorkflowStageMasterController : ControllerBase
{
    private readonly IPropertyWorkflowStageMasterService _service;
    private readonly ILogger<PropertyWorkflowStageMasterController> _logger;

    public PropertyWorkflowStageMasterController(IPropertyWorkflowStageMasterService service, ILogger<PropertyWorkflowStageMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all Property Workflow Stage Masters with filtering, sorting, and pagination
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyWorkflowStageMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Get Property Workflow Stage Master by ID
    /// </summary>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Create new Property Workflow Stage Master
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyWorkflowStageMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Update existing Property Workflow Stage Master
    /// </summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyWorkflowStageMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Delete Property Workflow Stage Master
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
