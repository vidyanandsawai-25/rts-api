using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Action payload submitted by an appellate officer to adjudicate an appeal (Approve, Reject, or Return with directions).
/// Inherits from UpdateBaseDtos matching UpdateRTSApplicationProcessDto conventions.
/// </summary>
public class ProcessRTSAppealActionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Appeal ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid Appeal ID.")]
    public int AppealId { get; set; }

    /// <summary>
    /// Appellate action: "Approve", "Reject", or "Return".
    /// </summary>
    [Required(ErrorMessage = "Action is required.")]
    [RegularExpression("^(Approve|Reject|Return)$", ErrorMessage = "Action must be 'Approve', 'Reject', or 'Return'.")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Mandatory official order, reason, or directions issued by the Appellate Authority.
    /// </summary>
    [Required(ErrorMessage = "Official order / remarks are required.")]
    [StringLength(1000, ErrorMessage = "Remark cannot exceed 1000 characters.")]
    public string Remark { get; set; } = string.Empty;

    public string? Status { get; set; }
}

/// <summary>
/// Alias matching UpdateRTSApplicationProcessDto naming pattern.
/// </summary>
public class UpdateRTSAppealProcessDto : ProcessRTSAppealActionDto
{
}
