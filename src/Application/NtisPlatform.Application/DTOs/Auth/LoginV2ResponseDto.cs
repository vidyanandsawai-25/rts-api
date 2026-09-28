

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Extended login response DTO containing JWT token along with user access details (roles, permissions, admin status).
/// </summary>
public class LoginV2ResponseDto
{

    public bool Success { get; set; }


    public string? Token { get; set; }


    public string? RefreshToken { get; set; }


    public int UserId { get; set; }


    public string? UserCode { get; set; }


    public string? Username { get; set; }


    public string? FirstName { get; set; }


    public string? MiddleName { get; set; }


    public string? LastName { get; set; }


    public string? Message { get; set; }


    public DateTime? ExpiresAt { get; set; }


    public bool RequiresPasswordChange { get; set; }


    public List<string> Roles { get; set; } = new();

  
    public List<LoginPermissionDto> Permissions { get; set; } = new();


    public bool IsAdmin { get; set; }


    public bool CanAllocateWards { get; set; }

    public bool RequiresTwoFactor { get; set; }

    public string? TwoFactorMethod { get; set; }

    public string? ChallengeId { get; set; }

    public DateTime? ChallengeExpiresAt { get; set; }

    public bool RequiresTwoFactorSetup { get; set; }

    public bool Throttled { get; set; }

    public int? RemainingLoginAttempts { get; set; }
}
