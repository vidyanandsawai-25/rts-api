namespace NtisPlatform.Application.DTOs.RTSCitizenSession;

public class RTSCitizenSessionDto:BaseDtos
{
    public string SessionId { get; set; } = string.Empty;
    public string? CitizenName { get; set; }
    public string? MobileNo { get; set; }
    public string? UPIC { get; set; }
    public string? PropertyNo { get; set; }
    public int? OwnerId { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivityTime { get; set; }
    public DateTime? LogoutTime { get; set; }
}

public class CreateRTSCitizenSessionDto : CreateBaseDtos
{
    public string SessionId { get; set; } = string.Empty;
    public string? CitizenName { get; set; }
    public string? MobileNo { get; set; }
    public string? UPIC { get; set; }
    public string? PropertyNo { get; set; }
    public int? OwnerId { get; set; }
}

public class UpdateRTSCitizenSessionDto : UpdateBaseDtos
{
    public string? CitizenName { get; set; }
    public string? MobileNo { get; set; }
    public string? UPIC { get; set; }
    public string? PropertyNo { get; set; }
    public int? OwnerId { get; set; }
    public DateTime? LastActivityTime { get; set; }
    public DateTime? LogoutTime { get; set; }
}

public class RTSCitizenSessionValidationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RTSCitizenSessionDto? Session { get; set; }
}

