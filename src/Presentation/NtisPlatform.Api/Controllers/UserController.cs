using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    // ── Standard CRUD ────────────────────────────────────────────────────────

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] UserQueryParameter queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_userService, queryParameters, _logger, ct);

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_userService, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateUserDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_userService, createDto, _logger, ct);

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateUserDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_userService, id, updateDto, _logger, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromBody] DeleteUserDto dto, CancellationToken ct)
    {
        var result = await _userService.DeleteAsync(id, dto, ct);
        return result ? NoContent() : NotFound();
    }

    // ── Security admin endpoints ─────────────────────────────────────────────
    // Separated from profile update so a normal user-edit role cannot
    // lock accounts or reset passwords.

    [HttpPut("{id:int}/lock")]
    public async Task<IActionResult> Lock(int id, [FromBody] DeactivateUserDto dto, CancellationToken ct)
    {
        var result = await _userService.DeactivateUserAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/unlock")]
    public async Task<IActionResult> Unlock(int id, [FromBody] ActivateUserDto dto, CancellationToken ct)
    {
        var result = await _userService.ActivateUserAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        var result = await _userService.ResetPasswordAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }
}