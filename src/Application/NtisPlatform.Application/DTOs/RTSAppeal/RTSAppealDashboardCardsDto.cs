namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Metric counts and distribution percentages for appellate officer work desk summary cards.
/// Aligns with RTSApplicationDashboardCardsCountDto patterns used across NTIS RTS modules.
/// </summary>
public class RTSAppealDashboardCardsCountDto
{
    // Total counters
    public int TotalApplications { get; set; }
    public int TotalAppeals { get; set; }

    // Status counters
    public int Pending { get; set; }
    public int PendingAppeals { get; set; }

    public int Approved { get; set; }
    public int ApprovedAppeals { get; set; }

    public int Rejected { get; set; }
    public int RejectedAppeals { get; set; }

    public int Returned { get; set; }
    public int ReturnedAppeals { get; set; }

    // Tier counters
    public int FirstAppeals { get; set; }
    public int SecondAppeals { get; set; }

    // Statutory timeline counters
    public int DelayedAppeals { get; set; }
    public int OverdueAppeals { get; set; }
    public int? OverdueApplications { get; set; }
    public int TodayApplications { get; set; }
    public int TodayAppeals { get; set; }
    public int DueToday { get; set; }
    public int ResolvedAppeals { get; set; }

    // Percentage distributions (rounded to 2 decimal places, matching RTSApplicationApproval)
    public decimal PendingPercentage { get; set; }
    public decimal ApprovedPercentage { get; set; }
    public decimal RejectedPercentage { get; set; }
    public decimal ReturnedPercentage { get; set; }
    public decimal TodayPercentage { get; set; }
    public decimal DelayedPercentage { get; set; }
    public decimal OverduePercentage { get; set; }
    public decimal DueTodayPercentage { get; set; }
}

/// <summary>
/// Type alias ensuring backward compatibility with RTSAppealDashboardCardsDto.
/// </summary>
public class RTSAppealDashboardCardsDto : RTSAppealDashboardCardsCountDto
{
}
