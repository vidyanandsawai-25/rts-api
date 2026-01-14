using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Auth;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using System.Security.Cryptography;
using static NtisPlatform.Core.Entities.SettingKeys;

namespace NtisPlatform.Infrastructure.Services.Auth;

/// <summary>
/// Main authentication service orchestrating login, token refresh, and logout
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<AuthService> _logger;
    private readonly Dictionary<AuthProviderType, IAuthenticationProvider> _authProviders;

    public AuthService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IOrganizationService organizationService,
        IEnumerable<IAuthenticationProvider> authProviders,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _organizationService = organizationService;
        _logger = logger;
        _authProviders = authProviders.ToDictionary(p => p.ProviderType);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Get organization from database (single deployment)
        var organization = await _organizationService.GetOrganizationAsync(cancellationToken);
        var organizationId = organization?.Id.ToString() ?? "1";

        // Determine auth provider
        var providerType = request.AuthProvider ?? AuthProviderType.Basic;
        if (!_authProviders.TryGetValue(providerType, out var authProvider))
        {
            _logger.LogWarning("Unsupported auth provider: {ProviderType}", providerType);
            throw new InvalidOperationException($"Auth provider {providerType} not supported");
        }

        // Log login attempt
        var loginAttempt = new LoginAttempt
        {
            Username = request.Username,
            IpAddress = request.Device?.IpAddress ?? "unknown",
            UserAgent = request.Device?.UserAgent,
            AuthProvider = providerType.ToString(),
            ClientType = request.ClientType.ToString(),
            Success = false
        };

        var loginAttemptSaved = false;

        try
        {
            // Authenticate user
            var authResult = await authProvider.AuthenticateAsync(request, cancellationToken);

            if (!authResult.IsSuccess)
            {
                loginAttempt.FailureReason = authResult.ErrorMessage;
                _context.LoginAttempts.Add(loginAttempt);
                await _context.SaveChangesAsync(cancellationToken);
                loginAttemptSaved = true;

                throw new UnauthorizedAccessException(authResult.ErrorMessage ?? "Authentication failed");
            }

            // Check if two-factor is required
            if (authResult.RequiresTwoFactor)
            {
                return new LoginResponse
                {
                    RequiresTwoFactor = true,
                    User = authResult.User!
                };
            }

            var user = authResult.User!;
            loginAttempt.UserId = user.Id;
            loginAttempt.Success = true;

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(
                user.Id, 
                organizationId, 
                user.Roles
            );

                var response = new LoginResponse
                {
                    AccessToken = accessToken,
                    TokenType = request.ClientType == ClientType.Web ? "Cookie" : "Bearer",
                    ExpiresIn = _jwtTokenService.GetAccessTokenExpirationSeconds(),
                    User = user
                };

                // Generate refresh token for non-web clients or web if requested
                if (request.ClientType != ClientType.Web || request.ClientType == ClientType.Web)
                {
                    var refreshToken = _jwtTokenService.GenerateRefreshToken();
                    var refreshTokenHash = HashToken(refreshToken);

                    var refreshTokenEntity = new RefreshToken
                    {
                        UserId = user.Id,
                        TokenHash = refreshTokenHash,
                        ClientType = request.ClientType.ToString(),
                        DeviceInfo = request.Device?.DeviceName,
                        IpAddress = request.Device?.IpAddress,
                        UserAgent = request.Device?.UserAgent,
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        LastUsedAt = DateTime.UtcNow
                    };

                    _context.RefreshTokens.Add(refreshTokenEntity);
                    response.RefreshToken = refreshToken;
                }

                // Generate CSRF token for web clients
                if (request.ClientType == ClientType.Web)
                {
                    response.CsrfToken = _jwtTokenService.GenerateCsrfToken();
                }

            // Save login attempt
            _context.LoginAttempts.Add(loginAttempt);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully from {IpAddress}", user.Id, request.Device?.IpAddress);

            return response;
        }
        catch (Exception ex)
        {
            // Only save failed login attempt if it wasn't already saved
            if (!loginAttemptSaved)
            {
                loginAttempt.FailureReason = ex.Message;
                _context.LoginAttempts.Add(loginAttempt);
                await _context.SaveChangesAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);

            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && !rt.IsRevoked, cancellationToken);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                _logger.LogWarning("Invalid or expired refresh token");
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // Check if device info matches for additional security
            if (!string.IsNullOrEmpty(request.Device?.IpAddress) && 
                refreshToken.IpAddress != request.Device.IpAddress)
            {
                _logger.LogWarning("Refresh token used from different IP. Original: {Original}, Current: {Current}", 
                    refreshToken.IpAddress, request.Device.IpAddress);
            }

            // Update last used timestamp
            refreshToken.LastUsedAt = DateTime.UtcNow;

            // Generate new access token
            var organization = await _organizationService.GetOrganizationAsync(cancellationToken);
            var organizationId = organization?.Id.ToString() ?? "1";
            var roles = refreshToken.User.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _jwtTokenService.GenerateAccessToken(
                refreshToken.UserId, 
                organizationId, 
                roles
            );

            // Implement refresh token rotation for enhanced security
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var newTokenHash = HashToken(newRefreshToken);

            // Revoke old token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReplacedByToken = newTokenHash;

            // Create new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = refreshToken.UserId,
                TokenHash = newTokenHash,
                ClientType = refreshToken.ClientType,
                DeviceInfo = request.Device?.DeviceName ?? refreshToken.DeviceInfo,
                IpAddress = request.Device?.IpAddress ?? refreshToken.IpAddress,
                UserAgent = request.Device?.UserAgent ?? refreshToken.UserAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                LastUsedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenService.GetAccessTokenExpirationSeconds()
        };
    }

    public async Task LogoutAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token != null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged out successfully", token.UserId);
        }
    }

    public async Task<bool> ValidateSessionAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = _jwtTokenService.ValidateToken(accessToken);
            return principal != null;
        }
        catch
        {
            // Expected exception for invalid tokens - return false instead of throwing
            return false;
        }
    }

    public async Task<OrganizationConfigResponse?> GetOrganizationConfigAsync(CancellationToken cancellationToken = default)
    {
        // Get organization from database
        var organization = await _organizationService.GetOrganizationAsync(cancellationToken);
            
            if (organization == null)
            {
                _logger.LogWarning("Organization not found in database");
                return null;
            }

            // Get branding and security settings from OrganizationSettings
            var settingKeys = new[] { 
                RequiresTwoFactor, 
                LogoUrl, 
                LogoWidth, 
                LogoHeight,
                LocalizedName,
                BackgroundImageUrl,
                PortalTitle
            };
            var settings = await _organizationService.GetOrganizationSettingsAsync(settingKeys, cancellationToken);
            
            var authProviders = await _context.AuthProviders
                .Where(ap => ap.IsEnabled)
                .OrderBy(ap => ap.Priority)
                .ToListAsync(cancellationToken);

        return new OrganizationConfigResponse
        {
            OrganizationId = organization.Id.ToString(),
            Name = organization.Name,
            LogoUrl = settings.GetValueOrDefault(LogoUrl),
            LogoWidth = int.TryParse(settings.GetValueOrDefault(LogoWidth), out var lw) ? lw : null,
            LogoHeight = int.TryParse(settings.GetValueOrDefault(LogoHeight), out var lh) ? lh : null,
            LocalizedName = settings.GetValueOrDefault(LocalizedName),
            BackgroundImageUrl = settings.GetValueOrDefault(BackgroundImageUrl),
            PortalTitle = settings.GetValueOrDefault(PortalTitle),
            EnabledAuthProviders = authProviders.Select(ap => new AuthProviderConfig
            {
                Type = Enum.Parse<AuthProviderType>(ap.ProviderType),
                DisplayName = ap.DisplayName,
                IsDefault = ap.IsDefault
            }).ToList(),
            RequiresTwoFactor = bool.TryParse(settings.GetValueOrDefault(RequiresTwoFactor), out var twoFactorValue) && twoFactorValue
        };
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}
