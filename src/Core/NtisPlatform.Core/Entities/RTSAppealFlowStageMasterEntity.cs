using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Master entity representing appellate stage configurations mapped to RTS approval flows.
/// StageOrder 1 corresponds to the First Appellate Authority, and StageOrder 2 corresponds to the Second Appellate Authority.
/// </summary>
public class RTSAppealFlowStageMasterEntity : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the parent approval flow.
    /// </summary>
    public int AppealFlowId { get; set; }

    /// <summary>
    /// Descriptive name of the appellate stage (e.g., "First Appellate Authority", "Second Appellate Authority").
    /// </summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// Appellate order (1 = First Appeal, 2 = Second Appeal).
    /// </summary>
    public int StageOrder { get; set; }

    /// <summary>
    /// Designated officer/user ID assigned to this appellate stage.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Whether this stage officer is permitted to approve appeals.
    /// </summary>
    public bool CanApprove { get; set; } = true;

    /// <summary>
    /// Whether this stage officer is permitted to reject appeals.
    /// </summary>
    public bool CanReject { get; set; } = true;

    /// <summary>
    /// Whether this stage officer is permitted to return appeals with directions.
    /// </summary>
    public bool CanReturn { get; set; } = true;

    // Navigation properties
    public virtual RTSApprovalFlowMasterEntity ApprovalFlow { get; set; } = null!;
    public virtual UserEntity? User { get; set; }
}

/// <summary>
/// Backward-compatibility alias for previous casing.
/// </summary>
public class RtsAppealFlowStageMasterEntity : RTSAppealFlowStageMasterEntity
{
}
