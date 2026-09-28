using System;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Row item representation for the appellate officer work desk grid.
/// Adheres to RTSApplicationDashboardDetailsDto naming conventions with appeal-specific extensions.
/// </summary>
public class RTSAppealDashboardDetailsDto
{
    // Identity & Key
    public int Id { get; set; }
    public int AppealId { get; set; }
    public string AppealNo { get; set; } = string.Empty;
    public int AppealLevel { get; set; }
    public string AppealLevelLabel { get; set; } = string.Empty;

    // Original RTS application
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;

    // Department & Service
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? DepartmentNameLocal { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceNameLocal { get; set; }

    // Applicant Details
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantMobileNo { get; set; } = string.Empty;
    public string ApplicantMobile { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;

    // Grievance / Appeal type
    public int AppealTypeId { get; set; }
    public string AppealTypeName { get; set; } = string.Empty;
    public string ReasonForComplaint { get; set; } = string.Empty;

    // Appeal Lifecycle Status & Dates
    public string AppealStatus { get; set; } = string.Empty;
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? FilingDate { get; set; }
    public string FilingCategory { get; set; } = "Normal";

    // SLA & Delay metrics
    public string? Sla { get; set; }
    public int? RemainingDays { get; set; }
    public string? DueDays { get; set; }
    public string? OverdueDays { get; set; }

    // Appellate Stage & Authority
    public int? CurrentStageOrder { get; set; }
    public string? CurrentStageName { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }

    // Action Permissions for Current Officer
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
}

/// <summary>
/// Type alias ensuring backward compatibility with RTSAppealGridItemDto.
/// </summary>
public class RTSAppealGridItemDto : RTSAppealDashboardDetailsDto
{
}
