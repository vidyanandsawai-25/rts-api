namespace NtisPlatform.Application.DTOs.RTSTrackApplicationHistory;

public class RTSTrackApplicationHistoryDto:BaseDtos
{
}


public class CreateRTSTrackApplicationHistoryDto : CreateBaseDtos
{
    public int ApplicationId { get; set; }
    public int ApprovalFlowId { get; set; }
    public int? ApprovalFlowStageId { get; set; }
    public int? CurrentEmployeeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? Action { get; set; }
    public bool IsReverted { get; set; }

}
