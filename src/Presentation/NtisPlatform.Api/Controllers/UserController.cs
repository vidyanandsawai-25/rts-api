using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Application.DTOs.TwoFactor;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public partial class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITwoFactorAuthenticationService _twoFactorService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ITwoFactorAuthenticationService twoFactorService,
        ILogger<UserController> logger,
        IUserScreenAccessService userScreenAccessService)
    {
        _userService = userService;
        _twoFactorService = twoFactorService;
        _logger = logger;
        _userScreenAccessService = userScreenAccessService;
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

    // ── Two-factor admin endpoints ───────────────────────────────────────────
    // "Require" only sets a policy flag — enabling 2FA itself still has to happen on the
    // user's own device, so there is no admin-side "enable" endpoint. "Reset" is the
    // account-recovery path (e.g. lost device): it clears enrollment without needing any code
    // from the user, invalidating their existing sessions in the process.

    [HttpPut("{id:int}/require-2fa")]
    public async Task<IActionResult> RequireTwoFactor(int id, [FromBody] RequireTwoFactorDto dto, CancellationToken ct)
    {
        var result = await _userService.RequireTwoFactorAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/unrequire-2fa")]
    public async Task<IActionResult> UnrequireTwoFactor(int id, [FromBody] UnrequireTwoFactorDto dto, CancellationToken ct)
    {
        var result = await _userService.UnrequireTwoFactorAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}/reset-2fa")]
    public async Task<IActionResult> ResetTwoFactor(int id, [FromBody] AdminResetTwoFactorDto dto, CancellationToken ct)
    {
        var result = await _userService.AdminResetTwoFactorAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Admin-assisted 2FA enrollment ────────────────────────────────────────
    // Lets an admin walk a user through authenticator setup in person (e.g. right after
    // creating their account) by scanning the QR with that user's own phone. This reuses the
    // exact same setup/enable/confirm logic the user's own self-service page uses — the service
    // has no notion of "caller," it always operates on whichever userId it's given. Proving the
    // scanner can operate an authenticator app doesn't prove it's THIS account's, whether the
    // caller is an admin or the account owner themselves — so completing setup always requires
    // confirming a one-time code emailed to the account's registered address too.

    [HttpPost("{id:int}/2fa/setup")]
    public async Task<IActionResult> BeginTwoFactorSetupForUser(int id, CancellationToken ct)
    {
        var result = await _twoFactorService.BeginSetupAsync(id, isReset: false, cancellationToken: ct);
        return result.Success ? Ok(result.Value) : MapTwoFactorFailure(result.Error!.Value);
    }

    [HttpPost("{id:int}/2fa/enable")]
    [EnableRateLimiting("mfa-enable")]
    public async Task<IActionResult> EnableTwoFactorForUser(int id, [FromBody] EnableTwoFactorRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _twoFactorService.EnableAsync(id, request.Code, ct);
        return result.Success ? Ok(result.Value) : MapTwoFactorFailure(result.Error!.Value);
    }

    [HttpPost("{id:int}/2fa/confirm-email")]
    [EnableRateLimiting("mfa-enable")]
    public async Task<IActionResult> ConfirmTwoFactorEmailForUser(int id, [FromBody] EnableTwoFactorRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _twoFactorService.ConfirmEnableAsync(id, request.Code, ct);
        return result.Success ? Ok(result.Value) : MapTwoFactorFailure(result.Error!.Value);
    }

    private IActionResult MapTwoFactorFailure(TwoFactorOperationError error) => error switch
    {
        TwoFactorOperationError.InvalidCode => Unauthorized(new { message = "Invalid verification code." }),
        TwoFactorOperationError.AlreadyEnabled => Conflict(new { message = "Two-factor authentication is already enabled." }),
        TwoFactorOperationError.NotEnabled => Conflict(new { message = "Two-factor authentication is not enabled." }),
        TwoFactorOperationError.SetupNotStarted => Conflict(new { message = "Authenticator setup has not been started." }),
        TwoFactorOperationError.UserNotFound => NotFound(new { message = "User not found." }),
        TwoFactorOperationError.EmailNotOnFile => Conflict(new { message = "This user has no email address on file. Add one before setting up two-factor authentication." }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}