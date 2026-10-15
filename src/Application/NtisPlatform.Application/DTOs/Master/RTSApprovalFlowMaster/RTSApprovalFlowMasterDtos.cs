using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.RTSApprovalFlowMaster;

/// <summary>
/// DTO for ApprovalFlowMaster
/// </summary>
public class RTSApprovalFlowMasterDto : BaseDtos
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string ApprovalFlowName { get; set; } = string.Empty;
}

public class CreateRTSApprovalFlowMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ServiceId_Required")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "ApprovalFlowName_Required")]
    [StringLength(200, ErrorMessage = "ApprovalFlowName_MaxLen_200")]
    public string ApprovalFlowName { get; set; } = string.Empty;
}

public class UpdateRTSApprovalFlowMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ServiceId_Required")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "ApprovalFlowName_Required")]
    [StringLength(200, ErrorMessage = "ApprovalFlowName_MaxLen_200")]
    public string ApprovalFlowName { get; set; } = string.Empty;
}

public class RTSApprovalFlowMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ServiceId { get; set; }

    [Searchable]
    public string? ApprovalFlowName { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for ApprovalFlowStageMaster
/// </summary>
public class RTSApprovalFlowStageMasterDto
{
    public int Id { get; set; }
    public int ApprovalFlowId { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int SlaDays { get; set; }
    public bool CanVerifyDocument { get; set; }
    public bool CanEdit { get; set; }
    public bool CanViewNoteSheet { get; set; }
    public bool CanUploadDocument { get; set; }
    public bool CanIssueCertificate { get; set; }
    public bool CanViewCertificate { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool IsFinalStage { get; set; }
    /// <summary>Number of saved candidate-user allocations configured for this stage.</summary>
    public int UserAllocationCount { get; set; }

}

public class CreateRTSApprovalFlowStageMasterDto
{
    [Required]
    public int ApprovalFlowId { get; set; }

    [Required]
    public int StageOrder { get; set; }

    [Required]
    [StringLength(200)]
    public string StageName { get; set; } = string.Empty;

    public int SlaDays { get; set; } = 3;

    public bool CanVerifyDocument { get; set; }
    public bool CanEdit { get; set; }
    public bool CanViewNoteSheet { get; set; }
    public bool CanUploadDocument { get; set; }
    public bool CanIssueCertificate { get; set; }
    public bool CanViewCertificate { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool IsFinalStage { get; set; }
}

public class UpdateRTSApprovalFlowStageMasterDto
{
    [Required]
    public int ApprovalFlowId { get; set; }

    [Required]
    public int StageOrder { get; set; }

    [Required]
    [StringLength(200)]
    public string StageName { get; set; } = string.Empty;

    public int SlaDays { get; set; }

    public bool CanVerifyDocument { get; set; }
    public bool CanEdit { get; set; }
    public bool CanViewNoteSheet { get; set; }
    public bool CanUploadDocument { get; set; }
    public bool CanIssueCertificate { get; set; }
    public bool CanViewCertificate { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool IsFinalStage { get; set; }
}

public class RTSApprovalFlowStageMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? Id { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ApprovalFlowId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? StageOrder { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? StageName { get; set; }
}
