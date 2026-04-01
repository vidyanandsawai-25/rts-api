using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Authentication controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return Unauthorized(new { message = response.Message });
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
}
