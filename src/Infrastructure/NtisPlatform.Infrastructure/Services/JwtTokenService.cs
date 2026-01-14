using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NtisPlatform.Application.Interfaces.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NtisPlatform.Infrastructure.Services.Auth;

/// <summary>
/// JWT token generation and validation service
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly int _accessTokenExpirationMinutes;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _accessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:ExpiresInMinutes", 15);
        _issuer = configuration.GetValue<string>("Jwt:Issuer") ?? "NtisPlatform";
        _audience = configuration.GetValue<string>("Jwt:Audience") ?? "NtisPlatformUsers";
    }

    public string GenerateAccessToken(int userId, string organizationId, List<string> roles, Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("organization_id", organizationId),
            new("user_id", userId.ToString())
        };

        // Add roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add additional claims
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var key = GetSigningKey();
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = GetSigningKey();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for expiration
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public int GetAccessTokenExpirationSeconds()
    {
        return _accessTokenExpirationMinutes * 60;
    }

    public string GenerateCsrfToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public bool ValidateCsrfToken(string token, string storedToken)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedToken))
            return false;

        try
        {
            var tokenBytes = Convert.FromBase64String(token);
            var storedBytes = Convert.FromBase64String(storedToken);
            return CryptographicOperations.FixedTimeEquals(tokenBytes, storedBytes);
        }
        catch
        {
            return false;
        }
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        // Load JWT signing key from configuration
        var key = _configuration.GetValue<string>("Jwt:Key") ?? throw new InvalidOperationException("JWT key not configured");
        
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}
