namespace NtisPlatform.Core.Entities;

public class TrackApplicationHistoryEntity : BaseEntity
{
    public int ApplicationId { get; set; }
    public int ApprovalFlowId { get; set; }
    public int? ApprovalFlowStageId { get; set; }
    public int? ActionByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? Action { get; set; }
    public bool IsReverted { get; set; }

    // Navigation properties 
    public virtual RTSApplicationDetailsEntity Application { get; set; } = null!;
    public virtual RTSApprovalFlowMasterEntity ApprovalFlow { get; set; } = null!;
    public virtual RTSApprovalFlowStageMasterEntity? ApprovalFlowStage { get; set; }


}
