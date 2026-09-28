using System;
using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Pre-filing citizen application summary providing read-only verification before appeal submission.
/// </summary>
public class RTSAppealApplicationSummaryDto
{
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantMobile { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedDate { get; set; }
    public DateTime? RejectionDate { get; set; }
    public string? RejectionReason { get; set; }

    public List<RTSAppealFieldAnswerDto> DynamicFields { get; set; } = new();
    public List<RTSAppealDocumentDto> Documents { get; set; } = new();
    public List<RTSAppealHistoryItemDto> Timeline { get; set; } = new();
}
