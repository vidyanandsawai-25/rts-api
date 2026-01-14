using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces.Auth;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services.Auth;

/// <summary>
/// Basic username/password authentication provider
/// </summary>
public class BasicAuthProvider : IAuthenticationProvider
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<BasicAuthProvider> _logger;

    public AuthProviderType ProviderType => AuthProviderType.Basic;

    public BasicAuthProvider(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<BasicAuthProvider> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find user by username or email
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.Username == request.Username || u.Email == request.Username,
                    cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent user: {Username}", request.Username);
                return AuthResult.Failure(AuthResultStatus.InvalidCredentials, "Invalid username or password");
            }

            // Check if account is locked
            if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt for locked account: {UserId}", user.Id);
                return AuthResult.Failure(AuthResultStatus.AccountLocked, 
                    $"Account is locked until {user.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for disabled account: {UserId}", user.Id);
                return AuthResult.Failure(AuthResultStatus.AccountDisabled, "Account is disabled");
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;
                
                // Lock account after max attempts (get from OrganizationSettings or use default 5)
                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsLocked = true;
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning("Account locked due to failed login attempts: {UserId}", user.Id);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return AuthResult.Failure(AuthResultStatus.InvalidCredentials, "Invalid username or password");
            }

            // Reset failed login attempts on successful login
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Check if two-factor is required
            if (user.RequiresTwoFactor && string.IsNullOrEmpty(request.TwoFactorCode))
            {
                var userInfo = MapToUserInfo(user);
                return AuthResult.TwoFactorRequired(userInfo);
            }

            // Validate two-factor code if provided
            if (user.RequiresTwoFactor && !string.IsNullOrEmpty(request.TwoFactorCode))
            {
                var isValidTwoFactor = await ValidateTwoFactorAsync(user.Id, request.TwoFactorCode, cancellationToken);
                if (!isValidTwoFactor)
                {
                    return AuthResult.Failure(AuthResultStatus.InvalidCredentials, "Invalid two-factor code");
                }
            }

            return AuthResult.Success(MapToUserInfo(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user: {Username}", request.Username);
            throw;
        }
    }

    public async Task<bool> ValidateTwoFactorAsync(int userId, string code, CancellationToken cancellationToken = default)
    {
        // TODO: Implement TOTP validation
        // For now, accept hardcoded OTP for demo
        return await Task.FromResult(code == "123456");
    }

    public async Task<bool> IsEnabledAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        // Check if Basic auth provider is enabled in AuthProviders table
        var provider = await _context.AuthProviders
            .FirstOrDefaultAsync(p => p.ProviderType == "Basic" && p.IsEnabled, cancellationToken);

        return provider != null;
    }

    private UserInfo MapToUserInfo(Core.Entities.User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }
}
