namespace NtisPlatform.Application.DTOs.RTSApplicationApproval;

/// <summary>
/// Application Dashboard cards Counts
/// </summary>
public class RTSApplicationDashboardCardsCountDto
{
    public int TotalApplications { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Reverted { get; set; }
    public int TodayApplications { get; set; }
    public int? OverdueApplications { get; set; }
    public int DueToday { get; set; }
    /// <summary>
    /// Application Dashboard Cards appplication status wise percentage Percentage 
    /// </summary>
    public decimal PendingPercentage { get; set; }
    public decimal ApprovedPercentage { get; set; }
    public decimal RejectedPercentage { get; set; }
    public decimal RevertedPercentage { get; set; }
    public decimal TodayPercentage { get; set; }
    public decimal DueTodayPercentage { get; set; }
    public decimal OverduePercentage { get; set; }
}
