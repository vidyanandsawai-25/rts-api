using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.TwoFactor;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Authenticator-app (TOTP) two-factor authentication management for the calling user's own
/// account. All actions operate exclusively on the authenticated caller — there is no way to
/// target another user's 2FA state through this controller.
/// </summary>
[ApiController]
[Route("api/security/two-factor")]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorAuthenticationService _twoFactorService;
    private readonly ILogger<TwoFactorController> _logger;

    public TwoFactorController(ITwoFactorAuthenticationService twoFactorService, ILogger<TwoFactorController> logger)
    {
        _twoFactorService = twoFactorService;
        _logger = logger;
    }

    /// <summary>
    /// Current 2FA status for the caller. Never returns the authenticator secret.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TwoFactorStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _twoFactorService.GetStatusAsync(GetUserId(), cancellationToken);
            return Ok(status);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving two-factor status");
            return StatusCode(500, new { message = "An error occurred while retrieving two-factor status" });
        }
    }

    /// <summary>
    /// Starts authenticator setup: generates a new secret and returns the otpauth:// URI and
    /// manual key for the frontend to render as a QR code. Rejected with 409 if 2FA is already
    /// enabled — use /reset for that case.
    /// </summary>
    [HttpPost("setup")]
    [ProducesResponseType(typeof(TwoFactorSetupResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Setup(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _twoFactorService.BeginSetupAsync(GetUserId(), isReset: false, cancellationToken: cancellationToken);
            return result.Success ? Ok(result.Value) : MapFailure(result.Error!.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting two-factor setup");
            return StatusCode(500, new { message = "An error occurred while starting two-factor setup" });
        }
    }

    /// <summary>
    /// Confirms the first code from the authenticator app. Proves the caller can operate an
    /// authenticator app, but not that it's bound to this account, so this does NOT enable 2FA
    /// yet — it emails a one-time code to the caller's registered address and returns the masked
    /// address. Call <see cref="ConfirmEmail"/> with that code to finish.
    /// </summary>
    [HttpPost("enable")]
    [EnableRateLimiting("mfa-enable")]
    [ProducesResponseType(typeof(TwoFactorEmailVerificationPendingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enable([FromBody] EnableTwoFactorRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _twoFactorService.EnableAsync(GetUserId(), request.Code, cancellationToken);
            return result.Success
                ? Ok(result.Value)
                : MapFailure(result.Error!.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling two-factor authentication");
            return StatusCode(500, new { message = "An error occurred while enabling two-factor authentication" });
        }
    }

    /// <summary>
    /// Confirms the one-time code emailed by <see cref="Enable"/>. Only on success does this
    /// actually enable 2FA and return recovery codes.
    /// </summary>
    [HttpPost("confirm-email")]
    [EnableRateLimiting("mfa-enable")]
    [ProducesResponseType(typeof(EnableTwoFactorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmEmail([FromBody] EnableTwoFactorRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _twoFactorService.ConfirmEnableAsync(GetUserId(), request.Code, cancellationToken);
            return result.Success
                ? Ok(result.Value)
                : MapFailure(result.Error!.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming two-factor email verification");
            return StatusCode(500, new { message = "An error occurred while confirming two-factor email verification" });
        }
    }

    /// <summary>
    /// Regenerates recovery codes after re-verifying a TOTP code. Invalidates all previously
    /// issued, unused recovery codes.
    /// </summary>
    [HttpPost("recovery-codes/regenerate")]
    [EnableRateLimiting("mfa-enable")]
    [ProducesResponseType(typeof(RecoveryCodesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegenerateRecoveryCodes([FromBody] TwoFactorCodeRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _twoFactorService.RegenerateRecoveryCodesAsync(GetUserId(), request.Code, cancellationToken);
            return result.Success
                ? Ok(result.Value)
                : MapFailure(result.Error!.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating recovery codes");
            return StatusCode(500, new { message = "An error occurred while regenerating recovery codes" });
        }
    }

    /// <summary>
    /// Disables 2FA after re-verifying a TOTP or recovery code. Clears the authenticator secret,
    /// invalidates recovery codes, rotates the security stamp, and revokes existing sessions.
    /// </summary>
    [HttpPost("disable")]
    [EnableRateLimiting("mfa-enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Disable([FromBody] TwoFactorCodeRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _twoFactorService.DisableAsync(GetUserId(), request.Code, cancellationToken);
            return result.Success
                ? NoContent()
                : MapFailure(result.Error!.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication");
            return StatusCode(500, new { message = "An error occurred while disabling two-factor authentication" });
        }
    }

    /// <summary>
    /// Resets the authenticator: disables the current one, invalidates recovery codes, rotates
    /// the security stamp, revokes existing sessions, and immediately starts a new setup so the
    /// caller can re-enroll. This is a sensitive operation and requires the same reauthentication
    /// as disable.
    /// </summary>
    [HttpPost("reset")]
    [EnableRateLimiting("mfa-enable")]
    [ProducesResponseType(typeof(TwoFactorSetupResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reset([FromBody] TwoFactorCodeRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _twoFactorService.ResetAsync(GetUserId(), request.Code, cancellationToken);
            return result.Success
                ? Ok(result.Value)
                : MapFailure(result.Error!.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting two-factor authenticator");
            return StatusCode(500, new { message = "An error occurred while resetting the authenticator" });
        }
    }

    private IActionResult MapFailure(TwoFactorOperationError error) => error switch
    {
        TwoFactorOperationError.InvalidCode => Unauthorized(new { message = "Invalid verification code." }),
        TwoFactorOperationError.AlreadyEnabled => Conflict(new { message = "Two-factor authentication is already enabled." }),
        TwoFactorOperationError.NotEnabled => Conflict(new { message = "Two-factor authentication is not enabled." }),
        TwoFactorOperationError.SetupNotStarted => Conflict(new { message = "Authenticator setup has not been started." }),
        TwoFactorOperationError.UserNotFound => Unauthorized(new { message = "User not found." }),
        TwoFactorOperationError.EmailNotOnFile => Conflict(new { message = "Your account has no email address on file. Add one before setting up two-factor authentication." }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }

        return id;
    }
}
