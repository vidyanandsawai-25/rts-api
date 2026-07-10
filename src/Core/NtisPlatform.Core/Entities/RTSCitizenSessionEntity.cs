namespace NtisPlatform.Core.Entities;

public class RTSCitizenSessionEntity:BaseEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string? CitizenName { get; set; }
    public string? MobileNo { get; set; }
    public string? UPIC { get; set; }
    public string? PropertyNo { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivityTime { get; set; }
    public DateTime? LogoutTime { get; set; }
}
