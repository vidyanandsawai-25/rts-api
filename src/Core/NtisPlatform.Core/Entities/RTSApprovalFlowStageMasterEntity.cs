using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RTSApprovalFlowStageMasterEntity:BaseEntity
{
    public int ApprovalFlowId { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int SLADays { get; set; }
    public bool CanVerifyDocument { get; set; }
    public bool CanViewNoteSheet { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool CanEdit { get; set; }
    public bool CanIssueCertificate { get; set; }
    public bool CanEditCertificate { get; set; }
    public bool IsManualCertificate { get; set; }
    public bool IsFinalStage { get; set; }
    public virtual RTSApprovalFlowMasterEntity ApprovalFlow { get; set; } = null!;
    public virtual UserEntity User { get; set; } = null!;
    public virtual List<TrackApplicationHistoryEntity> TrackApplicationHistory { get; set; } = new List<TrackApplicationHistoryEntity>();
}
