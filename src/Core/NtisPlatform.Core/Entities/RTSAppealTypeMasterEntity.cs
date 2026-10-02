namespace NtisPlatform.Core.Entities;

/// <summary>
/// Master entity defining statutory appeal grounds / categories (e.g., Delay in Service Delivery, Rejection of Application, Defective Service).
/// </summary>
public class RTSAppealTypeMasterEntity : BaseEntity
{
    /// <summary>
    /// Unique code identifying the appeal type.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive name of the appeal type.
    /// </summary>
    public string AppealTypeName { get; set; } = string.Empty;
}

/// <summary>
/// Backward-compatibility alias for previous casing.
/// </summary>
public class RtsAppealTypeMasterEntity : RTSAppealTypeMasterEntity
{
}
