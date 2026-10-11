namespace NtisPlatform.Core.Entities;

/// <summary>
/// A candidate user allocation for an RTS approval-flow stage.
/// Database relationships are intentionally loose until the allocation model is finalized.
/// </summary>
public class RTSApprovalFlowStageUserAllocationMasterEntity : BaseEntity
{
    public int? ApprovalFlowStageId { get; set; }
    public int? UserId { get; set; }
    public int? Priority { get; set; }
}
