using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RTSApprovalFlowMasterEntity : BaseEntity
{
    public int ServiceId { get; set; }
    public string ApprovalFlowName { get; set; } = string.Empty;

    // Navigation properties
    public virtual RTSServiceEntity Service { get; set; } = null!;
    public virtual List<RTSApprovalFlowStageMasterEntity> ApprovalFlowStages { get; set; } = new List<RTSApprovalFlowStageMasterEntity>();
    public virtual List<TrackApplicationHistoryEntity> TrackApplicationHistories { get; set; } = new List<TrackApplicationHistoryEntity>();
}
