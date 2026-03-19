using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.ConfigValueMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for Config Value Master CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigValueMasterController : ControllerBase
{
    private readonly IConfigValueMasterService _service;
    private readonly ILogger<ConfigValueMasterController> _logger;

    public ConfigValueMasterController(IConfigValueMasterService service, ILogger<ConfigValueMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all Config Values with filtering, sorting, and pagination
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ConfigValueMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Get Config Value by ID
    /// </summary>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Create new Config Value
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateConfigValueMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Update existing Config Value
    /// </summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateConfigValueMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Delete Config Value
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
