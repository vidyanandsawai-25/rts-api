namespace NtisPlatform.Core.Entities.Master;

public class RTSServiceEntity : BaseEntity
{
    public int DepartmentId { get; set; }

    /// <summary>
    /// Government RTS portal service reference code (e.g., 7204 = Birth Certificate).
    /// </summary>
    public int? GovtServiceCode { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceNameLocal { get; set; }
    public string? Description { get; set; }
    public string? ServiceUrl { get; set; }
    public string? ServiceIcon { get; set; }
    public int DisplayOrder { get; set; }
    public string? Sla { get; set; }
    public decimal? Fees { get; set; }
    public bool FeesRequired { get; set; }
    public virtual List<RTSApprovalFlowMasterEntity> ApprovalFlows { get; set; } = new List<RTSApprovalFlowMasterEntity>();
}
