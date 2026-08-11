using NtisPlatform.Application.DTOs.RTSFieldValue;

namespace NtisPlatform.Application.DTOs.RTSApplicationApproval;

public  class UpdateRTSApplicationVerificationDto : UpdateBaseDtos
{
    public string? Remark { get; set; }
    public string? Status { get; set; }
    public List<UpdateRTSFieldValueDto> FieldValue { get; set; } = new List<UpdateRTSFieldValueDto>();
}


public class UpdateRTSApplicationProcessDto : UpdateBaseDtos
{
    public string? Remark { get; set; }
    public string? Status { get; set; }
}

    public class RTSApplicationApprovalResponseDto
{
    public string? Status { get; set; }
    public string? Remark { get; set; }
    public int ApplicationId { get; set; }
    public string? ApplicationNo { get; set; }

}
