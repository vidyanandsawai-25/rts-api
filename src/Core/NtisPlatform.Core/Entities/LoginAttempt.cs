namespace NtisPlatform.Core.Entities;

/// <summary>
/// Login attempt audit log for security monitoring
/// </summary>
public class LoginAttempt : BaseEntity
{
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? AuthProvider { get; set; }
    public string? ClientType { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
}
