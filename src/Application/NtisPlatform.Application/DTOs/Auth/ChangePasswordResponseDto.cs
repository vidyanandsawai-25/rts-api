namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Response DTO for authenticated self-service password change.
/// </summary>
public class ChangePasswordResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
