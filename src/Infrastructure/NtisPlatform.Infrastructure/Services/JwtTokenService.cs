using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NtisPlatform.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// JWT token service implementation
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int userId, string username, string authenticationMethod = "pwd", string? securityStamp = null)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];
        var expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var minutes) ? minutes : 60;

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT Key is not configured");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("amr", authenticationMethod)
        };

        if (!string.IsNullOrEmpty(securityStamp))
        {
            claims.Add(new Claim("sst", securityStamp));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string GenerateShortLivedToken(Guid reportRequestId, int userId, int expiresInMinutes)
    {
        var (key, issuer, audience) = GetKeyMaterial();
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("scope", "report-slt"),
            new Claim("reportRequestId", reportRequestId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        return BuildToken(key, issuer, audience, claims, expiresInMinutes);
    }

    public (Guid reportRequestId, int userId)? ValidateShortLivedToken(string token)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];
        if (string.IsNullOrEmpty(jwtKey)) return null;

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var scope = principal.FindFirst("scope")?.Value;
            if (scope != "report-slt") return null;

            var rawId = principal.FindFirst("reportRequestId")?.Value;
            if (!Guid.TryParse(rawId, out var reportRequestId)) return null;

            var rawUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(rawUserId, out var userId)) return null;

            return (reportRequestId, userId);
        }
        catch
        {
            return null;
        }
    }

    public string GenerateReportWorkerToken(Guid reportRequestId, int subjectUserId, int expiresInMinutes)
    {
        var (key, issuer, audience) = GetKeyMaterial();
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectUserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, subjectUserId.ToString()),
            new Claim("scope", "report-worker"),
            new Claim("reportRequestId", reportRequestId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        return BuildToken(key, issuer, audience, claims, expiresInMinutes);
    }

    public string GenerateHubToken(int userId, int expiresInMinutes = 5)
    {
        var (key, issuer, audience) = GetKeyMaterial();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("scope", "report-hub"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        return BuildToken(key, issuer, audience, claims, expiresInMinutes);
    }

    private (string key, string issuer, string audience) GetKeyMaterial()
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? string.Empty;
        var audience = _configuration["Jwt:Audience"] ?? string.Empty;
        return (key, issuer, audience);
    }

    private static string BuildToken(string key, string issuer, string audience,
        IEnumerable<Claim> claims, int expiresInMinutes)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public JwtValidationResult ValidateToken(string token)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtKey))
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "JWT Key is not configured"
            };
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Extract claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var usernameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;

            var jwtToken = validatedToken as JwtSecurityToken;

            return new JwtValidationResult
            {
                IsValid = true,
                UserId = int.TryParse(userIdClaim, out var userId) ? userId : null,
                Username = usernameClaim,
                ExpiresAt = jwtToken?.ValidTo
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Token validation failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Unexpected error during token validation: {ex.Message}"
            };
        }
    }
}
