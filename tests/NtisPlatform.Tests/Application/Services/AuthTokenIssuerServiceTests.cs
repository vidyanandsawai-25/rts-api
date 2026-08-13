using Microsoft.Extensions.Configuration;
using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Unit tests for AuthTokenIssuerService — the single place that mints an access token and
/// persists a refresh token, shared by password-only login and post-MFA login completion.
/// Expiration math uses DateTime.Now directly (see the comment in AuthTokenIssuerService), so
/// these tests assert against a small tolerance window around "now" rather than a fixed instant.
/// </summary>
public class AuthTokenIssuerServiceTests
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly AuthTokenIssuerService _service;

    public AuthTokenIssuerServiceTests()
    {
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("60");
        _configurationMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns((string s) => $"hash:{s}");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("raw-refresh-token");

        _service = new AuthTokenIssuerService(
            _tokenServiceMock.Object,
            _passwordHasherMock.Object,
            _refreshTokenRepositoryMock.Object,
            _configurationMock.Object);
    }

    private static UserEntity NewUser() => new()
    {
        Id = 1,
        UserName = "jdoe",
        FirstName = "Jane",
        LastName = "Doe",
        SecurityStamp = "stamp-123"
    };

    [Fact]
    public async Task IssueAsync_GeneratesTokenWithAuthenticationMethodAndSecurityStamp()
    {
        var user = NewUser();
        _tokenServiceMock.Setup(x => x.GenerateToken(1, "jdoe", "mfa", "stamp-123")).Returns("jwt-token");

        var result = await _service.IssueAsync(user, "mfa");

        Assert.True(result.Success);
        Assert.Equal("jwt-token", result.Token);
        _tokenServiceMock.Verify(x => x.GenerateToken(1, "jdoe", "mfa", "stamp-123"), Times.Once);
    }

    [Fact]
    public async Task IssueAsync_PersistsHashedRefreshTokenNotPlaintext()
    {
        var user = NewUser();
        _tokenServiceMock.Setup(x => x.GenerateToken(1, "jdoe", "pwd", "stamp-123")).Returns("jwt-token");

        RefreshTokenEntity? saved = null;
        _refreshTokenRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((e, _) => saved = e)
            .ReturnsAsync((RefreshTokenEntity e, CancellationToken _) => e);

        var result = await _service.IssueAsync(user, "pwd");

        Assert.Equal("raw-refresh-token", result.RefreshToken);
        Assert.NotNull(saved);
        Assert.Equal("hash:raw-refresh-token", saved!.Token);
        Assert.NotEqual("raw-refresh-token", saved.Token);
        _refreshTokenRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IssueAsync_UsesConfiguredTokenExpiration()
    {
        var user = NewUser();
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("60");

        var beforeIssue = DateTime.Now;
        var result = await _service.IssueAsync(user, "pwd");
        var afterIssue = DateTime.Now;

        Assert.NotNull(result.ExpiresAt);
        Assert.True(result.ExpiresAt >= beforeIssue.AddMinutes(60).AddSeconds(-1));
        Assert.True(result.ExpiresAt <= afterIssue.AddMinutes(60).AddSeconds(1));
    }

    [Fact]
    public async Task IssueAsync_WithInvalidExpirationConfig_UsesDefaultSixtyMinutes()
    {
        var user = NewUser();
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("not-a-number");

        var beforeIssue = DateTime.Now;
        var result = await _service.IssueAsync(user, "pwd");
        var afterIssue = DateTime.Now;

        Assert.NotNull(result.ExpiresAt);
        Assert.True(result.ExpiresAt >= beforeIssue.AddMinutes(60).AddSeconds(-1));
        Assert.True(result.ExpiresAt <= afterIssue.AddMinutes(60).AddSeconds(1));
    }

    [Fact]
    public async Task IssueAsync_UsesConfiguredRefreshTokenExpiryDays()
    {
        var user = NewUser();
        _configurationMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("14");

        RefreshTokenEntity? saved = null;
        _refreshTokenRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((e, _) => saved = e)
            .ReturnsAsync((RefreshTokenEntity e, CancellationToken _) => e);

        var beforeIssue = DateTime.Now;
        await _service.IssueAsync(user, "pwd");
        var afterIssue = DateTime.Now;

        Assert.NotNull(saved);
        Assert.True(saved!.ExpiresAt >= beforeIssue.AddDays(14).AddSeconds(-1));
        Assert.True(saved.ExpiresAt <= afterIssue.AddDays(14).AddSeconds(1));
    }

    [Fact]
    public async Task IssueAsync_ReturnsUserProfileFields()
    {
        var user = NewUser();

        var result = await _service.IssueAsync(user, "pwd");

        Assert.True(result.Success);
        Assert.Equal(1, result.UserId);
        Assert.Equal("jdoe", result.Username);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("Login successful", result.Message);
    }
}
