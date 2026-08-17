namespace NtisPlatform.Application.DTOs.RTSApplicationApproval;

public class RTSApplicationVerificationDto
{
}


public class CurrentApprovalOfficerDto
{
    public int ApplicationId { get; set; }
    public string? ApplicationNo { get; set; }
    public string? ApplicationStatus { get; set; }

    public int? ApprovalFlowId { get; set; }

    public int StageId { get; set; }
    public string? StageName { get; set; }
    public int StageOrder { get; set; }
    public int SLADays { get; set; }
    public bool IsFinalStage { get; set; }

    public int? OfficerId { get; set; }
    public string? OfficerName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? OfficerEmail { get; set; }

    public bool IsAssignedOfficer { get; set; }

    public bool CanVerifyDocument { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool CanEdit { get; set; }
    public bool CanViewNoteSheet { get; set; }

    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public decimal? ServiceFees { get; set; }
    public bool FeesRequired { get; set; }
}
