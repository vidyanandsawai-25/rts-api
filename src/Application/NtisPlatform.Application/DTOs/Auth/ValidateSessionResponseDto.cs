namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Response DTO for session validation
/// </summary>
public class ValidateSessionResponseDto
{
    public bool IsValid { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public int? UserRoleId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
}
