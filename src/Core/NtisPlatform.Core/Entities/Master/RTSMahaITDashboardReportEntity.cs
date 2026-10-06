using System;

namespace NtisPlatform.Core.Entities.Master;

public class RTSMahaITDashboardReportEntity
{
    public int Id { get; set; }
    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public int RtsServiceId { get; set; }
    public int MahaITServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string DepartmentCode { get; set; } = "AKMC";
    public int Division { get; set; } = 6;
    public int District { get; set; } = 520;
    public int Taluka { get; set; } = 4173;
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int PendingatUser { get; set; }
    public int PendingatDepartment { get; set; }
    public int OnTimeDelivery { get; set; }
    public int NotOnTimeDelivery { get; set; }
    public string ApplicationSource { get; set; } = "U";
    public string PaymentMode { get; set; } = "PG";
    public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;
    public DateTime? LastPushedOn { get; set; }
    public string? PushStatus { get; set; }
    public string? PushResponse { get; set; }
}
