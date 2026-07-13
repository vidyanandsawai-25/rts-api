using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSCitizenSession;

public class RTSCitizenSessionDto : BaseDtos
{
    public string SessionId { get; set; } = string.Empty;
    public string? CitizenName { get; set; }
    public string? MobileNo { get; set; }

    /// <summary>
    /// Unique Property Identification Code. Renamed from UPIC to Upic (camelCase convention).
    /// </summary>
    public string? Upic { get; set; }

    public string? PropertyNo { get; set; }
    public int? OwnerId { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivityTime { get; set; }
    public DateTime? LogoutTime { get; set; }
}

public class CreateRTSCitizenSessionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "CitizenSession_SessionId_Required")]
    [StringLength(200, ErrorMessage = "CitizenSession_SessionId_MaxLen_200")]
    public string SessionId { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "CitizenSession_CitizenName_MaxLen_200")]
    public string? CitizenName { get; set; }

    [StringLength(20, ErrorMessage = "CitizenSession_MobileNo_MaxLen_20")]
    public string? MobileNo { get; set; }

    [StringLength(50, ErrorMessage = "CitizenSession_Upic_MaxLen_50")]
    public string? Upic { get; set; }

    [StringLength(100, ErrorMessage = "CitizenSession_PropertyNo_MaxLen_100")]
    public string? PropertyNo { get; set; }

    public int? OwnerId { get; set; }
}

public class UpdateRTSCitizenSessionDto : UpdateBaseDtos
{
    [StringLength(200, ErrorMessage = "CitizenSession_CitizenName_MaxLen_200")]
    public string? CitizenName { get; set; }

    [StringLength(20, ErrorMessage = "CitizenSession_MobileNo_MaxLen_20")]
    public string? MobileNo { get; set; }

    [StringLength(50, ErrorMessage = "CitizenSession_Upic_MaxLen_50")]
    public string? Upic { get; set; }

    [StringLength(100, ErrorMessage = "CitizenSession_PropertyNo_MaxLen_100")]
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
