namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Response DTO for refresh token operation
/// </summary>
public class RefreshTokenResponseDto
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
}
