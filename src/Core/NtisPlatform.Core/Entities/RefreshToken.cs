namespace NtisPlatform.Core.Entities;

/// <summary>
/// Refresh token entity for maintaining user sessions
/// </summary>
public class RefreshToken : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public bool IsExpired => DateTime.Now >= ExpiresAt;
    // Hides BaseEntity.IsActive - this is a computed property, not stored in DB
    public new bool IsActive => !IsRevoked && !IsExpired;
}
