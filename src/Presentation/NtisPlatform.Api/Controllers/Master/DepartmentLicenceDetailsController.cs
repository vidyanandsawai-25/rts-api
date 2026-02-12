using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for Department Licence Details CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DepartmentLicenceDetailsController : ControllerBase
{
    private readonly IDepartmentLicenceDetailsService _service;
    private readonly ILogger<DepartmentLicenceDetailsController> _logger;

    public DepartmentLicenceDetailsController(IDepartmentLicenceDetailsService service, ILogger<DepartmentLicenceDetailsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all Department Licence Details with filtering, sorting, and pagination
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] DepartmentLicenceDetailsQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Get Department Licence Details by ID
    /// </summary>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Create new Department Licence Details
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateDepartmentLicenceDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Update existing Department Licence Details
    /// </summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentLicenceDetailsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Delete Department Licence Details
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
