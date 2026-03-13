using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces.Auth;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services.Auth;

namespace NtisPlatform.Tests.Application;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_SavesRefreshTokenAndLoginAttempt_ReturnsTokens()
    {
        // Arrange
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<System.Collections.Generic.List<string>>(), null))
            .Returns("access-token-xyz");
        jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token-abc");
        jwtMock.Setup(j => j.GenerateCsrfToken()).Returns("csrf-token-123");
        jwtMock.Setup(j => j.GetAccessTokenExpirationSeconds()).Returns(3600);

        var passwordHasherMock = new Mock<IPasswordHasher>();

        var orgServiceMock = new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>();
        orgServiceMock.Setup(o => o.GetOrganizationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 5, Name = "TestOrg" });

        var providerMock = new Mock<IAuthenticationProvider>();
        providerMock.Setup(p => p.ProviderType).Returns(AuthProviderType.Basic);

        var userInfo = new UserInfo
        {
            Id = 1,
            Username = "jdoe",
            Email = "jdoe@example.com",
            FirstName = "John",
            LastName = "Doe",
            Roles = new System.Collections.Generic.List<string> { "Admin" }
        };

        providerMock.Setup(p => p.AuthenticateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult { Status = AuthResultStatus.Success, User = userInfo });
        var logger = NullLogger<AuthService>.Instance;
        // var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<AuthService>();
        // var logger =  NullLogger<OrganizationService>.Instance;

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            passwordHasherMock.Object,
            orgServiceMock.Object,
            new[] { providerMock.Object },
            logger);

        var request = new LoginRequest
        {
            Username = "jdoe",
            Password = "password!",
            ClientType = ClientType.Web,
            AuthProvider = AuthProviderType.Basic,
            Device = new DeviceInfo { IpAddress = "127.0.0.1", DeviceName = "UnitTest" }
        };

        // Act
        var result = await authService.LoginAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token-xyz", result.AccessToken);
        Assert.Equal("refresh-token-abc", result.RefreshToken);
        Assert.Equal("csrf-token-123", result.CsrfToken);
        Assert.Equal(userInfo.Username, result.User.Username);

        // DB side effects
        var rt = _context.RefreshTokens.FirstOrDefault();
        Assert.NotNull(rt);
        Assert.Equal(userInfo.Id, rt!.UserId);
        Assert.False(string.IsNullOrEmpty(rt.TokenHash));

        var attempt = _context.LoginAttempts.FirstOrDefault();
        Assert.NotNull(attempt);
        Assert.Equal(userInfo.Id, attempt!.UserId);
        Assert.True(attempt.Success);
        Assert.Equal(request.Device.IpAddress, attempt.IpAddress);
    }

    [Fact]
    public async Task LoginAsync_ProviderNotFound_ThrowsInvalidOperationException()
    {
        var jwtMock = new Mock<IJwtTokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var orgServiceMock = new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>();
        orgServiceMock.Setup(o => o.GetOrganizationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1, Name = "O" });

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            passwordHasherMock.Object,
            orgServiceMock.Object,
            Array.Empty<IAuthenticationProvider>(),
            NullLogger<AuthService>.Instance);

        var req = new LoginRequest { Username = "x", Password = "p", ClientType = ClientType.Web, AuthProvider = AuthProviderType.Basic };

        await Assert.ThrowsAsync<InvalidOperationException>(() => authService.LoginAsync(req));
    }

    [Fact]
    public async Task LoginAsync_AuthFailure_SavesAttemptAndThrowsUnauthorized()
    {
        var jwtMock = new Mock<IJwtTokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var orgServiceMock = new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>();
        orgServiceMock.Setup(o => o.GetOrganizationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1, Name = "O" });

        var providerMock = new Mock<IAuthenticationProvider>();
        providerMock.Setup(p => p.ProviderType).Returns(AuthProviderType.Basic);
        providerMock.Setup(p => p.AuthenticateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResult.Failure(AuthResultStatus.InvalidCredentials, "bad"));

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            passwordHasherMock.Object,
            orgServiceMock.Object,
            new[] { providerMock.Object },
            NullLogger<AuthService>.Instance);

        var req = new LoginRequest { Username = "x", Password = "p", ClientType = ClientType.Web, AuthProvider = AuthProviderType.Basic };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync(req));

        var attempt = _context.LoginAttempts.FirstOrDefault();
        Assert.NotNull(attempt);
        Assert.False(attempt!.Success);
        Assert.Equal("bad", attempt.FailureReason);
    }

    //[Fact]
    //public async Task LoginAsync_TwoFactorRequired_ReturnsRequiresTwoFactor()
    //{
    //    var jwtMock = new Mock<IJwtTokenService>();
    //    var passwordHasherMock = new Mock<IPasswordHasher>();
    //    var orgServiceMock = new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>();
    //    orgServiceMock.Setup(o => o.GetOrganizationAsync(It.IsAny<CancellationToken>()))
    //        .ReturnsAsync(new Organization { Id = 1, Name = "O" });

    //    var providerMock = new Mock<IAuthenticationProvider>();
    //    providerMock.Setup(p => p.ProviderType).Returns(AuthProviderType.Basic);
    //    var userInfo = new UserInfo { Id = 2, Username = "u" };
    //    providerMock.Setup(p => p.AuthenticateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
    //        .ReturnsAsync(AuthResult.TwoFactorRequired(userInfo));

    //    var authService = new AuthService(
    //        _context,
    //        jwtMock.Object,
    //        passwordHasherMock.Object,
    //        orgServiceMock.Object,
    //        new[] { providerMock.Object },
    //        NullLogger<AuthService>.Instance);

    //    var req = new LoginRequest { Username = "u", Password = "p", ClientType = ClientType.Web, AuthProvider = AuthProviderType.Basic };

    //    var res = await authService.LoginAsync(req, It.IsAny<CancellationToken>());
    //    Assert.True(res.RequiresTwoFactor);
    //    Assert.Equal(userInfo.Id, res.User.Id);
    //}

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ThrowsUnauthorized()
    {
        var jwtMock = new Mock<IJwtTokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var orgServiceMock = new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>();
        orgServiceMock.Setup(o => o.GetOrganizationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 1, Name = "O" });

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            passwordHasherMock.Object,
            orgServiceMock.Object,
            Array.Empty<IAuthenticationProvider>(),
            NullLogger<AuthService>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "nope" }));
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesAndReturnsNewTokens()
    {
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<System.Collections.Generic.List<string>>(), null))
            .Returns("new-access");
        jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("rotated-refresh");
        jwtMock.Setup(j => j.GetAccessTokenExpirationSeconds()).Returns(3600);

        var passwordHasherMock = new Mock<IPasswordHasher>();

        var orgServiceMock = new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>();
        orgServiceMock.Setup(o => o.GetOrganizationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 2, Name = "O" });

        // Prepare user, role and refresh token
        var role = new Role { Id = 10, Name = "User", CreatedDate = DateTime.Now, IsActive = true };
        var user = new User { Id = 3, Username = "u", Email = "e@e", PasswordHash = "h", IsActive = true };
        var userRole = new UserRole { User = user, Role = role, RoleId = role.Id, UserId = user.Id, CreatedDate = DateTime.Now, IsActive = true };
        user.UserRoles = new System.Collections.Generic.List<UserRole> { userRole };

        // Create token string and hash
        var originalToken = "orig-refresh-token";
        static string ComputeHashStatic(string t)
        {
            using var sha = SHA256.Create();
            var hb = sha.ComputeHash(Encoding.UTF8.GetBytes(t));
            return Convert.ToBase64String(hb);
        }

        var originalHash = ComputeHashStatic(originalToken);

        var rt = new RefreshToken
        {
            User = user,
            UserId = user.Id,
            TokenHash = originalHash,
            ClientType = ClientType.Web.ToString(),
            ExpiresAt = DateTime.Now.AddDays(1),
            LastUsedAt = DateTime.Now
        };

        _context.Roles.Add(role);
        _context.Users.Add(user);
        _context.UserRoles.Add(userRole);
        _context.RefreshTokens.Add(rt);
        await _context.SaveChangesAsync();

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            passwordHasherMock.Object,
            orgServiceMock.Object,
            Array.Empty<IAuthenticationProvider>(),
            NullLogger<AuthService>.Instance);

        var resp = await authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = originalToken });

        Assert.Equal("new-access", resp.AccessToken);
        Assert.Equal("rotated-refresh", resp.RefreshToken);

        var storedOld = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == originalHash);
        Assert.NotNull(storedOld);
        Assert.True(storedOld!.IsRevoked);
        Assert.False(string.IsNullOrEmpty(storedOld.ReplacedByToken));

        var rotatedRefreshHash = ComputeHashStatic("rotated-refresh");
        var storedNew = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == rotatedRefreshHash);
        Assert.NotNull(storedNew);
    }

    [Fact]
    public async Task LogoutAsync_RevokesExistingToken()
    {
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var jwtMock = new Mock<IJwtTokenService>();

        string ComputeHash(string t)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(t)));
        }

        var token = "to-logout";
        var hash = ComputeHash(token);
        var rt = new RefreshToken { UserId = 1, TokenHash = hash, ExpiresAt = DateTime.Now.AddDays(1) };
        _context.RefreshTokens.Add(rt);
        await _context.SaveChangesAsync();

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            passwordHasherMock.Object,
            new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>().Object,
            Array.Empty<IAuthenticationProvider>(),
            NullLogger<AuthService>.Instance);

        await authService.LogoutAsync(token, "1.2.3.4", CancellationToken.None);

        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
        Assert.NotNull(stored);
        Assert.True(stored!.IsRevoked);
        Assert.Equal("1.2.3.4", stored.RevokedByIp);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsTrueForValidToken_FalseForInvalid()
    {
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.ValidateToken("valid")).Returns(new System.Security.Claims.ClaimsPrincipal());
        jwtMock.Setup(j => j.ValidateToken("invalid")).Returns((System.Security.Claims.ClaimsPrincipal?)null);

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            new Mock<IPasswordHasher>().Object,
            new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>().Object,
            Array.Empty<IAuthenticationProvider>(),
            NullLogger<AuthService>.Instance);

        var ok = await authService.ValidateSessionAsync("valid");
        var no = await authService.ValidateSessionAsync("invalid");

        Assert.True(ok);
        Assert.False(no);
    }

    [Fact]
    public async Task GetOrganizationConfigAsync_ReturnsConfig()
    {
        var jwtMock = new Mock<IJwtTokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var orgServiceMock = new Mock<NtisPlatform.Application.Interfaces.IOrganizationService>();
        orgServiceMock.Setup(o => o.GetOrganizationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = 9, Name = "MyOrg" });
        orgServiceMock.Setup(o => o.GetOrganizationSettingsAsync(It.IsAny<System.Collections.Generic.IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new System.Collections.Generic.Dictionary<string, string>
            {
                ["Branding.LogoUrl"] = "u.png",
                ["Branding.LogoWidth"] = "100",
                ["Branding.LogoHeight"] = "200",
                ["Security.RequiresTwoFactor"] = "true",
                ["Branding.LocalizedName"] = "My Local"
            });

        _context.AuthProviders.Add(new AuthProvider { ProviderType = "Basic", DisplayName = "Basic", IsEnabled = true, IsDefault = true, Priority = 1 });
        _context.AuthProviders.Add(new AuthProvider { ProviderType = "Google", DisplayName = "Google", IsEnabled = false, IsDefault = false, Priority = 2 });
        await _context.SaveChangesAsync();

        var authService = new AuthService(
            _context,
            jwtMock.Object,
            passwordHasherMock.Object,
            orgServiceMock.Object,
            Array.Empty<IAuthenticationProvider>(),
            NullLogger<AuthService>.Instance);

        var cfg = await authService.GetOrganizationConfigAsync(CancellationToken.None);

        Assert.NotNull(cfg);
        Assert.Equal("9", cfg!.OrganizationId);
        Assert.Equal("MyOrg", cfg.Name);
        Assert.Equal("u.png", cfg.LogoUrl);
        Assert.Equal(100, cfg.LogoWidth);
        Assert.Equal(200, cfg.LogoHeight);
        Assert.True(cfg.RequiresTwoFactor);
        Assert.Single(cfg.EnabledAuthProviders);
        Assert.Equal(AuthProviderType.Basic, cfg.EnabledAuthProviders.First().Type);
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
