using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces.Auth;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Authentication controller handling login, token refresh, and logout
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
    /// Authenticate user and generate tokens
    /// Rate limited: 5 attempts per 15 minutes per IP
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Add device info from request context
        if (request.Device == null)
        {
            request.Device = new DeviceInfo();
        }

        request.Device.IpAddress ??= HttpContext.Connection.RemoteIpAddress?.ToString();
        request.Device.UserAgent ??= Request.Headers.UserAgent.ToString();

        var response = await _authService.LoginAsync(request, cancellationToken);

        // For web clients, set httpOnly cookies
        if (request.ClientType == ClientType.Web && !response.RequiresTwoFactor)
        {
            SetAuthCookies(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        // For web clients, read refresh token from cookie
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            request.RefreshToken = Request.Cookies["refresh_token"] ?? string.Empty;
        }

        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return Unauthorized(new { error = "Refresh token required" });
        }

        // Add device info
        if (request.Device == null)
        {
            request.Device = new DeviceInfo
            {
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };
        }

        var response = await _authService.RefreshTokenAsync(request, cancellationToken);

        // Update cookies for web clients
        if (Request.Cookies.ContainsKey("session_token"))
        {
            Response.Cookies.Append("session_token", response.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromSeconds(response.ExpiresIn),
                Path = "/"
            });

            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromDays(30),
                    Path = "/"
                });
            }
        }

        return Ok(response);
    }

    /// <summary>
    /// Logout user and revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refresh_token"] ?? string.Empty;

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.LogoutAsync(refreshToken, ipAddress, cancellationToken);
        }

        // Clear cookies
        Response.Cookies.Delete("session_token");
        Response.Cookies.Delete("refresh_token");
        Response.Cookies.Delete("csrf_token");

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Validate current session token
    /// </summary>
    [HttpPost("validate-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateSession([FromBody] ValidateSessionRequest request, CancellationToken cancellationToken)
    {
        var token = request.AccessToken ?? Request.Cookies["session_token"] ?? string.Empty;

        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { error = "No session token provided" });
        }

        var isValid = await _authService.ValidateSessionAsync(token, cancellationToken);

        if (isValid)
        {
            return Ok(new { valid = true });
        }

        return Unauthorized(new { valid = false, error = "Invalid session" });
    }

    private void SetAuthCookies(LoginResponse response)
    {
        // Set session token cookie
        Response.Cookies.Append("session_token", response.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromSeconds(response.ExpiresIn),
            Path = "/"
        });

        // Set refresh token cookie
        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/"
            });
        }

        // Set CSRF token cookie (readable by JavaScript)
        if (!string.IsNullOrEmpty(response.CsrfToken))
        {
            Response.Cookies.Append("csrf_token", response.CsrfToken, new CookieOptions
            {
                HttpOnly = false, // Readable by JavaScript
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromSeconds(response.ExpiresIn),
                Path = "/"
            });
        }
    }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public class ValidateSessionRequest
{
    public string? AccessToken { get; set; }
}
