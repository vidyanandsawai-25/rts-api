namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Response for <c>GET api/ApartmentQCTopSectionBelowFlex</c>: every configured workflow stage
/// and certificate type for one property, each with its completion/issuance status and audit
/// trail (who created/updated it and when). Data-driven — whichever stages/certificate types are
/// configured in PropertyWorkflowStageMaster/PropertyCertificateTypeMaster show up here; there is
/// no hardcoded/fabricated badge list.
/// </summary>
public sealed class ApartmentQcBelowFlexDto
{
    public int PropertyId { get; init; }
    public List<WorkflowStageStatusDto> WorkflowStages { get; init; } = new();
    public List<CertificateTypeStatusDto> CertificateTypes { get; init; } = new();
}

/// <summary>
/// One PropertyWorkflowStageMaster stage: whether this property has completed it (the latest
/// PropertyWorkflowDetails row for the stage has CurrentStatus = true), and who created/last
/// updated that row and when. All audit fields are null when the property has no
/// PropertyWorkflowDetails row for this stage yet (i.e. not started).
/// </summary>
public sealed class WorkflowStageStatusDto
{
    public int StageId { get; init; }
    public string StageName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
    public int? CreatedBy { get; init; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedDate { get; init; }
    public int? UpdatedBy { get; init; }
    public string? UpdatedByName { get; set; }
    public DateTime? UpdatedDate { get; init; }
}

/// <summary>
/// One PropertyCertificateTypeMaster certificate type: whether this property has an active,
/// property-level PropertyCertificates row of that type on file, and who created/last updated it
/// and when. All audit fields are null when no such certificate has been issued.
/// </summary>
public sealed class CertificateTypeStatusDto
{
    public int CertificateTypeId { get; init; }
    public string CertificateTypeCode { get; init; } = string.Empty;
    public string CertificateTypeName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsIssued { get; init; }
    public string? CertificateNo { get; init; }
    public DateTime? IssueDate { get; init; }
    public int? CreatedBy { get; init; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedDate { get; init; }
    public int? UpdatedBy { get; init; }
    public string? UpdatedByName { get; set; }
    public DateTime? UpdatedDate { get; init; }
}
