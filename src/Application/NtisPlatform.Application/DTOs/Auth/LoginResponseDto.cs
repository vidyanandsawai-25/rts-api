namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Login response DTO
/// </summary>
public class LoginResponseDto
{
    public bool Success { get; set; }

    /// <summary>
    /// JWT access token (short-lived)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Refresh token (long-lived, used to obtain new access tokens)
    /// </summary>
    public string? RefreshToken { get; set; }

    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool RequiresPasswordChange { get; set; }
}
