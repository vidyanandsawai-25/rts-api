using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.DTOs.PasswordReset;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Api.Controllers;



/// <summary>
/// Authentication controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMfaChallengeService _mfaChallengeService;
    private readonly IOtpChallengeService _otpChallengeService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IAuthTokenIssuerService _authTokenIssuer;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IMfaChallengeService mfaChallengeService,
        IOtpChallengeService otpChallengeService,
        IPasswordResetService passwordResetService,
        IAuthTokenIssuerService authTokenIssuer,
        IUserRepository userRepository,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _mfaChallengeService = mfaChallengeService;
        _otpChallengeService = otpChallengeService;
        _passwordResetService = passwordResetService;
        _authTokenIssuer = authTokenIssuer;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Login endpoint - authenticate user and return JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with JWT token if successful</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);

            if (!response.Success)
            {
                if (response.Throttled)
                {
                    _logger.LogWarning("Login throttled for username: {Username}", request.Username);
                    return StatusCode(StatusCodes.Status423Locked, new { message = response.Message });
                }

                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return Unauthorized(new { message = response.Message, remainingLoginAttempts = response.RemainingLoginAttempts });
            }

            _logger.LogInformation("Successful login for user: {Username}", request.Username);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or request timed out - let it propagate
            // ASP.NET Core will handle this appropriately (no 500 error logged)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Completes a login that returned RequiresTwoFactor by verifying a TOTP or recovery code
    /// against the pending MFA challenge.
    /// </summary>
    /// <param name="request">Challenge id plus code (or recovery code)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The normal login response (token + refresh token) once verified</returns>
    [HttpPost("two-factor/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("mfa-verify")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _mfaChallengeService.VerifyLoginChallengeAsync(
                request.ChallengeId,
                request.Code,
                request.UseRecoveryCode,
                cancellationToken);

            if (!result.Success)
            {
                if (result.FailureReason == MfaVerificationFailureReason.ChallengeLocked)
                {
                    return StatusCode(StatusCodes.Status423Locked, new { message = "Too many failed attempts. Please sign in again." });
                }

                _logger.LogWarning("MFA verification failed: {Reason}", result.FailureReason);
                return Unauthorized(new { message = "Invalid or expired verification code." });
            }

            _logger.LogInformation("MFA verification succeeded");
            return Ok(result.LoginResponse);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during two-factor verification");
            return StatusCode(500, new { message = "An error occurred during verification" });
        }
    }

    /// <summary>
    /// Completes a login that returned RequiresTwoFactor with TwoFactorMethod "otp" by verifying
    /// the emailed/texted one-time code against the pending challenge.
    /// </summary>
    /// <param name="request">Challenge id plus the OTP code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The normal login response (token + refresh token) once verified</returns>
    [HttpPost("login-otp/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("mfa-verify")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _otpChallengeService.VerifyAsync(
                request.ChallengeId, OtpChallengePurpose.LoginOtp, request.Code, cancellationToken);

            if (!result.Success)
            {
                if (result.FailureReason == OtpVerificationFailureReason.ChallengeLocked)
                {
                    return StatusCode(StatusCodes.Status423Locked, new { message = "Too many failed attempts. Please sign in again." });
                }

                _logger.LogWarning("Login OTP verification failed: {Reason}", result.FailureReason);
                return Unauthorized(new { message = "Invalid or expired verification code." });
            }

            var user = await _userRepository.GetByIdAsync(result.UserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login OTP verified for user {UserId} but user is no longer valid", result.UserId);
                return Unauthorized(new { message = "Invalid or expired verification code." });
            }

            var loginResponse = await _authTokenIssuer.IssueAsync(user, "otp", cancellationToken);

            _logger.LogInformation("Login OTP verification succeeded for user {UserId}", result.UserId);
            return Ok(loginResponse);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login OTP verification");
            return StatusCode(500, new { message = "An error occurred during verification" });
        }
    }

    /// <summary>
    /// Refresh access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.RefreshTokenAsync(request, cancellationToken);

            if (!response.Success)
            {
                return Unauthorized(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Validate if an access token is still valid
    /// </summary>
    /// <param name="request">Session validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with user information</returns>
    [HttpPost("validate-session")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ValidateSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateSession([FromBody] ValidateSessionRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.ValidateSessionAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session validation");
            return StatusCode(500, new { message = "An error occurred during session validation" });
        }
    }

    /// <summary>
    /// Looks up which OTP delivery methods (email/SMS/authenticator app) are actually usable for
    /// the given account, so the client can offer only real choices before calling
    /// <see cref="ForgotPassword"/>. Gated by "2FALOGINFORFPASS" like the rest of the flow.
    /// </summary>
    /// <param name="request">Username or email identifying the account</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("forgot-password/methods")]
    [AllowAnonymous]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordAvailableMethodsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPasswordMethods([FromBody] ForgotPasswordAvailableMethodsRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _passwordResetService.GetAvailableMethodsAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot-password methods lookup");
            return StatusCode(500, new { message = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Starts the self-service forgot-password flow by sending a one-time code via email/SMS
    /// (per SECURITY_AUTH config). Gated by the "2FALOGINFORFPASS" flag — when off, self-service
    /// reset is unavailable and the response says so without revealing whether the account exists.
    /// </summary>
    /// <param name="request">Username or email identifying the account</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _passwordResetService.ForgotPasswordAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot-password request");
            return StatusCode(500, new { message = "An error occurred processing the request" });
        }
    }

    /// <summary>
    /// Verifies the OTP sent by <see cref="ForgotPassword"/> and, on success, issues a short-lived
    /// reset token used to authorize the actual password change via <see cref="ResetPassword"/>.
    /// </summary>
    /// <param name="request">Challenge id plus the OTP code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("forgot-password/verify-otp")]
    [AllowAnonymous]
    [EnableRateLimiting("mfa-verify")]
    [ProducesResponseType(typeof(VerifyForgotPasswordOtpResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyForgotPasswordOtpRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _passwordResetService.VerifyForgotPasswordOtpAsync(request, cancellationToken);

            if (!response.Success)
            {
                return Unauthorized(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot-password OTP verification");
            return StatusCode(500, new { message = "An error occurred during verification" });
        }
    }

    /// <summary>
    /// Completes the forgot-password flow, setting a new password using the reset token obtained
    /// from <see cref="VerifyForgotPasswordOtp"/>. Revokes all of the user's existing sessions.
    /// </summary>
    /// <param name="request">Reset token plus new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType(typeof(ResetPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _passwordResetService.ResetPasswordAsync(request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { message = "An error occurred during password reset" });
        }
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Important limitation:</b> This endpoint only revokes the refresh token.
    /// The access token (JWT) remains valid until its natural expiration.
    /// </para>
    /// <para>
    /// This is an intentional design decision based on stateless JWT architecture.
    /// For short-lived access tokens (recommended: 15-60 minutes), this provides
    /// an acceptable security posture while maintaining scalability.
    /// </para>
    /// <para>
    /// If immediate session invalidation is required, consider implementing
    /// access token revocation via a token blocklist or distributed cache.
    /// </para>
    /// </remarks>
    /// <param name="request">Logout request with refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout result</returns>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LogoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _authService.LogoutAsync(request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Changes password for the authenticated user or an unauthenticated user with must-change-password requirement.
    /// Requires current password verification and validates new password policy compliance.
    /// </summary>
    /// <param name="request">Current and new password payload (plus optional UserName for unauthenticated reset)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the password change attempt</returns>
    [HttpPost("change-password")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ChangePasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        int? userId = null;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId) && parsedId > 0)
        {
            userId = parsedId;
        }

        if (!userId.HasValue && string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(new { message = "Username is required." });
        }

        try
        {
            var response = await _authService.ChangePasswordAsync(userId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(new { message = response.Message });
            }

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during change password");
            return StatusCode(500, new { message = "An error occurred during password change" });
        }
    }
}
