using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for managing role-wise screen access permissions
/// </summary>
[ApiController]
[Route("api/[controller]")]
 
public class RoleWiseScreenAccessMasterController : ControllerBase
{
    private readonly IRoleWiseScreenAccessMasterService _service;
    private readonly ILogger<RoleWiseScreenAccessMasterController> _logger;

    public RoleWiseScreenAccessMasterController(IRoleWiseScreenAccessMasterService service, ILogger<RoleWiseScreenAccessMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all role-wise screen access records with filtering and pagination
    /// </summary>
    [HttpGet]    
    public Task<IActionResult> GetAll([FromQuery] RoleWiseScreenAccessQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Get a specific role-wise screen access by ID
    /// </summary>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Create a new role-wise screen access
    /// </summary>
    [HttpPost]  
    public Task<IActionResult> Create([FromBody] CreateRoleWiseScreenAccessMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Update an existing role-wise screen access
    /// </summary>
    [HttpPut("{id}")]   
    public Task<IActionResult> Update(int id, [FromBody] UpdateRoleWiseScreenAccessMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Delete a role-wise screen access
    /// </summary>
    [HttpDelete("{id}")]   
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
