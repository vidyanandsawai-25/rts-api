using System;
using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Comprehensive appellate dossier presented to appellate officers for adjudication.
/// Contains the complete snapshot of the original RTS application, dynamic answers, documents, timeline, and decision actions.
/// Adheres to RTSApplicationViewDetailsDto conventions with appellate enhancements.
/// </summary>
public class RTSAppealProcessDetailsDto
{
    // Appeal identity & status
    public int AppealId { get; set; }
    public string AppealNo { get; set; } = string.Empty;
    public int AppealLevel { get; set; }
    public string AppealLevelLabel { get; set; } = string.Empty;
    public string AppealStatus { get; set; } = string.Empty;
    public DateTime? FilingDate { get; set; }
    public int AppealTypeId { get; set; }
    public string AppealTypeName { get; set; } = string.Empty;
    public string ReasonForComplaint { get; set; } = string.Empty;

    // Appellate Decision & remarks
    public string? ActionRemarks { get; set; }
    public DateTime? ActionDate { get; set; }
    public int? ActionByUserId { get; set; }
    public string? ActionByUserName { get; set; }

    // Original application snapshot
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantMobile { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string OriginalApplicationStatus { get; set; } = string.Empty;
    public DateTime? OriginalApplicationDate { get; set; }
    public DateTime? OriginalRejectionDate { get; set; }
    public string? OriginalRejectionReason { get; set; }

    /// <summary>
    /// Nested application summary (optional structured navigation).
    /// </summary>
    public RTSAppealApplicationSummaryDto AppealDetails { get; set; } = new();

    /// <summary>
    /// Dynamic form answers submitted with the original application.
    /// </summary>
    public List<RTSAppealFieldAnswerDto> DynamicFields { get; set; } = new();

    /// <summary>
    /// Uploaded citizen attachments and verification certificates.
    /// </summary>
    public List<RTSAppealDocumentDto> Documents { get; set; } = new();

    /// <summary>
    /// Chronological history timeline of the appeal and original application workflow stages.
    /// </summary>
    public List<RTSAppealHistoryItemDto> Timeline { get; set; } = new();

    // Stage & Permissions
    public int? CurrentStageOrder { get; set; }
    public string? CurrentStageName { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
}

/// <summary>
/// Alias matching RTSApplicationViewDetailsDto naming convention.
/// </summary>
public class RTSAppealViewDetailsDto : RTSAppealProcessDetailsDto
{
}
