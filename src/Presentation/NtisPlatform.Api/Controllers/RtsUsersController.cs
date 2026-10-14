using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RTSUser;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Read-only, compact user lookup for RTS workflows. It never exposes user
/// department, module, or role allocation collections.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class RtsUsersController : ControllerBase
{
    private readonly IRtsUserService _service;

    public RtsUsersController(IRtsUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] RtsUserQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(queryParameters, cancellationToken);
        return Ok(result);
    }
}
