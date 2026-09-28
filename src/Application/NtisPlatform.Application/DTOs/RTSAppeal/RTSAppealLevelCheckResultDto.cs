using System;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Evaluation result checking citizen appeal tier eligibility and statutory windows under Maharashtra Right to Services Act (MRTSA 2015).
/// </summary>
public class RTSAppealLevelCheckResultDto
{
    public string ApplicationNo { get; set; } = string.Empty;
    public string AppealLevel { get; set; } = "1st Appeal";
    public bool IsSecondAppeal { get; set; }
    public int ExistingAppealsCount { get; set; }
    public string SuggestedAppealNo { get; set; } = string.Empty;
    public bool CanFileAppeal { get; set; } = true;
    public string? BlockReason { get; set; }
    public string? FirstAppealStatus { get; set; }
    public string? FirstAppealNo { get; set; }

    /// <summary>
    /// Statutory filing category: "Normal" (<= 30 days) or "Delayed" (31-90 days).
    /// </summary>
    public string FilingCategory { get; set; } = "Normal";

    /// <summary>
    /// True when elapsed days exceed 30 days, requiring statutory Condonation of Delay (विलंब माफीचे कारण).
    /// </summary>
    public bool RequiresDelayJustification { get; set; }

    public DateTime? TriggerDate { get; set; }
    public string? TriggerReason { get; set; }
    public int? ElapsedDays { get; set; }
    public int? ServiceSlaDays { get; set; }
    public DateTime? ServiceSlaExpiryDate { get; set; }
    public DateTime? DeemedDelayDate { get; set; }
}
