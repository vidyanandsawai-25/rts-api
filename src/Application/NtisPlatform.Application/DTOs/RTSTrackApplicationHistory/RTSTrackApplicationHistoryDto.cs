using System;

namespace NtisPlatform.Application.DTOs.RTSTrackApplicationHistory;

public class RTSTrackApplicationHistoryDto : BaseDtos
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string? ApplicationNo { get; set; }
    public int ApprovalFlowId { get; set; }
    public int? ApprovalFlowStageId { get; set; }
    public string? StageName { get; set; }
    public int? ActionByUserId { get; set; }
    public string? ActionByUserName { get; set; }
    public string? ActionByOfficerName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public bool IsReverted { get; set; }
    public bool IsDigitallySigned { get; set; }
    public string? DigitalSignatureInfo { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateRTSTrackApplicationHistoryDto : CreateBaseDtos
{
    public int ApplicationId { get; set; }
    public int ApprovalFlowId { get; set; }
    public int? ApprovalFlowStageId { get; set; }
    public int? CurrentEmployeeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? Action { get; set; }
    public bool IsReverted { get; set; }
}
