namespace NtisPlatform.Core.Entities;

/// <summary>
/// Audit and tracking timeline entity recording every citizen appeal submission and appellate officer adjudication.
/// </summary>
public class RTSTrackAppealHistoryEntity : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the parent appeal application.
    /// </summary>
    public int AppealId { get; set; }

    /// <summary>
    /// Foreign key referencing the underlying RTS service application.
    /// </summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// Appellate level (1 = First Appeal, 2 = Second Appeal).
    /// </summary>
    public int AppealLevel { get; set; }

    /// <summary>
    /// Foreign key referencing the approval flow configuration, if applicable.
    /// </summary>
    public int? ApprovalFlowId { get; set; }

    /// <summary>
    /// Foreign key referencing the appellate flow stage, if applicable.
    /// </summary>
    public int? AppealFlowStageId { get; set; }

    /// <summary>
    /// ID of the user/officer or citizen who performed this action.
    /// </summary>
    public int ActionByUserId { get; set; }

    /// <summary>
    /// High-level status snapshot after this action (e.g., "First Appeal Submitted", "Approved", "Rejected").
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Specific action performed (e.g., "Appeal Filed", "Appeal Approved", "Appeal Rejected").
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Detailed official remarks or citizen grounds accompanying the action.
    /// </summary>
    public string? Remark { get; set; }

    // Navigation properties
    public virtual RTSAppealApplicationEntity AppealApplication { get; set; } = null!;
    public virtual RTSAppealFlowStageMasterEntity? AppealFlowStage { get; set; }
    public virtual RTSApprovalFlowMasterEntity? ApprovalFlow { get; set; }
}

/// <summary>
/// Backward-compatibility alias for previous casing.
/// </summary>
public class RtsTrackAppealHistoryEntity : RTSTrackAppealHistoryEntity
{
}
