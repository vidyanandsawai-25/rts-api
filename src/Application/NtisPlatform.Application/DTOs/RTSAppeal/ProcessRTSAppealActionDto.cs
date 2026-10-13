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

    /// <summary>
    /// Backward-compatible alias for frontend payloads sending "remarks".
    /// </summary>
    public string? Remarks
    {
        get => Remark;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Remark = value;
            }
        }
    }

    public string? Status { get; set; }

    /// <summary>
    /// Statutory date and time of personal hearing (MRTSA 2015 Section 8(2)).
    /// </summary>
    public DateTime? HearingDate { get; set; }

    /// <summary>
    /// Official notice, hearing agenda, or summons remark dispatched to the appellant / designated officer.
    /// </summary>
    [StringLength(1000)]
    public string? HearingNoticeRemarks { get; set; }

    /// <summary>
    /// Statutory appellate order category: "DirectionToDeliverService", "RejectionOrder", "PenaltyImposed", "Dismissed", "Remanded".
    /// </summary>
    [StringLength(100)]
    public string? OrderType { get; set; }

    /// <summary>
    /// Statutory deadline stipulated by the Appellate Authority for the designated officer to provide the public service.
    /// </summary>
    public DateTime? ServiceDeliveryDueDate { get; set; }

    /// <summary>
    /// Statutory penalty amount imposed on the defaulting designated officer (MRTSA 2015 Section 9: ₹250/day up to ₹5,000).
    /// </summary>
    [Range(0, 5000, ErrorMessage = "Penalty amount must be between ₹0 and ₹5,000 as per MRTSA 2015 Section 9.")]
    public decimal? PenaltyAmount { get; set; }

    /// <summary>
    /// Official justification and breakdown for penalty or departmental inquiry recommendation (Section 10).
    /// </summary>
    [StringLength(1000)]
    public string? PenaltyRemarks { get; set; }

    /// <summary>
    /// File path or reference to uploaded signed appellate order document.
    /// </summary>
    [StringLength(500)]
    public string? OrderDocumentPath { get; set; }
}

/// <summary>
/// Alias matching UpdateRTSApplicationProcessDto naming pattern.
/// </summary>
public class UpdateRTSAppealProcessDto : ProcessRTSAppealActionDto
{
}
