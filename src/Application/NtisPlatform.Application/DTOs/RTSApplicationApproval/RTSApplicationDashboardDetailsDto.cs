namespace NtisPlatform.Application.DTOs.RTSApplicationApproval;


/// <summary>
/// RTS Application approval Dashboard Application details
/// </summary>
public class RTSApplicationDashboardDetailsDto
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public int ServiceId { get; set; }
    public string? ApplicationNo { get; set; }
    public string? ApplicationStatus { get; set; }
    public string? ApplicantName { get; set; }
    public string? ApplicantMobileNo { get; set; }
    public string? Remark { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? SessionId { get; set; }
    public int? OwnerId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentNameLocal { get; set; }
    public string? ServiceName { get; set; }
    public string? ServiceNameLocal { get; set; }
    public string? Sla { get; set; }
    public int? RemainingDays { get; set; }
    public string? DueDays { get; set; }
    public string? OverdueDays { get; set; }
}
