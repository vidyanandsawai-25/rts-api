using Microsoft.Extensions.Configuration;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Single source of truth for minting an authenticated session, used by both password-only
/// login and post-MFA login completion so refresh-token issuance logic exists in exactly one
/// place.
/// </summary>
public class AuthTokenIssuerService : IAuthTokenIssuerService
{
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;

    public AuthTokenIssuerService(
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> IssueAsync(UserEntity user, string authenticationMethod, CancellationToken cancellationToken = default)
    {
        // Deliberately DateTime.Now (not TimeProvider/UTC): RefreshTokenEntity.ExpiresAt is
        // compared against DateTime.Now everywhere else in this codebase (RefreshTokenRepository,
        // AuthService.RefreshTokenAsync) — writing a UTC value here while those read it as local
        // time would silently corrupt refresh-token expiry by the server's UTC offset.
        var now = DateTime.Now;

        var token = _tokenService.GenerateToken(user.Id, user.UserName, authenticationMethod, user.SecurityStamp);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;
        var refreshTokenHash = _passwordHasher.HashPassword(refreshToken);

        var refreshTokenEntity = new RefreshTokenEntity
        {
            Token = refreshTokenHash,
            UserId = user.Id,
            ExpiresAt = now.AddDays(refreshTokenExpiryDays),
            IsRevoked = false,
            CreatedBy = user.Id,
            CreatedDate = now
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var minutes) ? minutes : 60;

        return new LoginResponseDto
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Username = user.UserName,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            Message = "Login successful",
            ExpiresAt = now.AddMinutes(expiresInMinutes),
            RequiresTwoFactorSetup = user.TwoFactorRequired && !user.TwoFactorEnabled
        };
    }
}
