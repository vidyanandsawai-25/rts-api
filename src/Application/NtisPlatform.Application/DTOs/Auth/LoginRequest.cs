using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Request model for user authentication
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username or email address
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Client type requesting authentication
    /// </summary>
    [Required]
    public ClientType ClientType { get; set; } = ClientType.Web;

    /// <summary>
    /// Two-factor authentication code (if applicable)
    /// </summary>
    [MaxLength(10)]
    public string? TwoFactorCode { get; set; }

    /// <summary>
    /// Authentication provider to use
    /// </summary>
    public AuthProviderType? AuthProvider { get; set; }

    /// <summary>
    /// Device information for session tracking
    /// </summary>
    public DeviceInfo? Device { get; set; }
}

/// <summary>
/// Device information for security tracking
/// </summary>
public class DeviceInfo
{
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    public string? DeviceName { get; set; }

    [MaxLength(50)]
    public string? Platform { get; set; }
}
