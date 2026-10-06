using System;
using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.AapleSarkar;

/// <summary>
/// Data row structure required by MahaIT RTS Central Dashboard:
/// POST /api/Dashboard/PushDepartmentDetails
/// Document Reference: DashboardAPI_clientDoc_New.pdf
/// </summary>
public class MahaITDashboardRowDto
{
    public string Department { get; set; } = "AKMC";
    public int Service { get; set; } // MahaIT Service ID (e.g. 5873, 7165, etc.)
    public int Division { get; set; } = 6;
    public int District { get; set; } = 520;
    public int Taluka { get; set; } = 4173;
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int OnTimeDelivery { get; set; }
    public int PendingatUser { get; set; }
    public int PendingatDepartment { get; set; }
    public int NotOnTimeDelivery { get; set; }
    public string PaymentMode { get; set; } = "PG"; // "PG" or "NA"
    public string ApplicationSource { get; set; } = "U"; // 'U' for Aaple Sarkar, 'D' for Department Direct
    public int Year { get; set; }
    public int Month { get; set; }
}

public class MahaITDashboardPushResultDto
{
    public bool IsSuccess { get; set; }
    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public int TotalServices { get; set; }
    public int SuccessServices { get; set; }
    public int FailedServices { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RawResponse { get; set; }
    public List<MahaITDashboardRowDto> PushedData { get; set; } = new();
}

public class MahaITTokenResponse
{
    public string? data { get; set; }
}
