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

    /// <summary>
    /// Devanagari Marathi localized name of the appeal ground.
    /// </summary>
    public string? AppealTypeNameLocal { get; set; }

    /// <summary>
    /// Statutory description citing MRTSA 2015 provisions.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Backward-compatibility alias for previous casing.
/// </summary>
public class RtsAppealTypeMasterEntity : RTSAppealTypeMasterEntity
{
}
