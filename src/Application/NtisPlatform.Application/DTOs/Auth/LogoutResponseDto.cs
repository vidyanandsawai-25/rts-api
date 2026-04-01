namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Response DTO for logout
/// </summary>
public class LogoutResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
