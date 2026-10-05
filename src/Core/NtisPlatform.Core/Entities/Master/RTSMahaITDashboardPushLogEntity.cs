using System;

namespace NtisPlatform.Core.Entities.Master;

public class RTSMahaITDashboardPushLogEntity
{
    public int Id { get; set; }
    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public int TotalServices { get; set; }
    public int SuccessServices { get; set; }
    public int FailedServices { get; set; }
    public bool IsSuccess { get; set; }
    public bool TokenObtained { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime PushedAt { get; set; } = DateTime.UtcNow;
}
