using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

public class RTSServiceEntity : BaseEntity
{
    public int DepartmentId { get; set; }

    /// <summary>
    /// Government RTS portal service reference code (e.g., 7204 = Birth Certificate, 8273, etc.).
    /// </summary>
    public int? GovtCode { get; set; }

    [NotMapped]
    public int? GovtServiceCode
    {
        get => GovtCode;
        set => GovtCode = value;
    }

    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceNameLocal { get; set; }
    public string? Description { get; set; }
    public string? ServiceUrl { get; set; }
    public string? ServiceIcon { get; set; }
    public int DisplayOrder { get; set; }
    public string? Sla { get; set; }
    public decimal? Fees { get; set; }
    public bool FeesRequired { get; set; }
    public NtisPlatform.Core.Enums.RTSCertificateType CertificateType { get; set; } = NtisPlatform.Core.Enums.RTSCertificateType.None;
    public bool IsCertificateRequired { get; set; } = true;
    public bool IsSmsEnabled { get; set; } = true;

    [ForeignKey(nameof(DepartmentId))]
    public virtual RTSDepartmentEntity? Department { get; set; }
    public virtual List<RTSApprovalFlowMasterEntity> ApprovalFlows { get; set; } = new List<RTSApprovalFlowMasterEntity>();
}
