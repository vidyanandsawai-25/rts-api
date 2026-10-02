namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Result response returned upon successfully submitting an RTS appeal.
/// </summary>
public class RTSAppealApplicationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AppealNo { get; set; } = string.Empty;
    public string AppealLevel { get; set; } = string.Empty;
}
