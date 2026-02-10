using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.DepartmentMaster;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for Department Master CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DepartmentMasterController : ControllerBase
{
    private readonly IDepartmentMasterService _service;
    private readonly ILogger<DepartmentMasterController> _logger;

    public DepartmentMasterController(IDepartmentMasterService service, ILogger<DepartmentMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all Department Masters with filtering, sorting, and pagination
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] DepartmentMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Get Department Master by ID
    /// </summary>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Create new Department Master
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateDepartmentMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Update existing Department Master
    /// </summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Delete Department Master
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
