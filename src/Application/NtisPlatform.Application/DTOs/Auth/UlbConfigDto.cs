namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// ULB Configuration DTO for login response
/// Contains organization/ULB details
/// </summary>
public class UlbConfigDto
{
    public int UlbId { get; set; }
    public string UlbCode { get; set; } = string.Empty;
    public string UlbName { get; set; } = string.Empty;
    public string? UlbNameLocal { get; set; }
    public string? UlbLogo { get; set; }
    public string? EmailId { get; set; }
    public string? MobileNo { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? UlbAddress { get; set; }
    public string? State { get; set; }
    public string? District { get; set; }
}
