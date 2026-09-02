using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;

/// <summary>
/// DTO for ApprovalFlowMaster
/// </summary>
public class ApprovalFlowMasterDto : BaseDtos
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string ApprovalFlowName { get; set; } = string.Empty;
}

public class CreateApprovalFlowMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ServiceId_Required")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "ApprovalFlowName_Required")]
    [StringLength(200, ErrorMessage = "ApprovalFlowName_MaxLen_200")]
    public string ApprovalFlowName { get; set; } = string.Empty;
}

public class UpdateApprovalFlowMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ServiceId_Required")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "ApprovalFlowName_Required")]
    [StringLength(200, ErrorMessage = "ApprovalFlowName_MaxLen_200")]
    public string ApprovalFlowName { get; set; } = string.Empty;
}

public class ApprovalFlowMasterQueryParameters : BaseQueryParameters
{
    public int? ServiceId { get; set; }
    public string? ApprovalFlowName { get; set; }
}

/// <summary>
/// DTO for ApprovalFlowStageMaster
/// </summary>
public class ApprovalFlowStageMasterDto
{
    public int Id { get; set; }
    public int ApprovalFlowId { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int EmployeeTypeId { get; set; }
    public int SlaDays { get; set; }
    public bool CanVerifyDocument { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool IsFinalStage { get; set; }

    //officer name
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? OfficerName { get; set; }
}

public class CreateApprovalFlowStageMasterDto
{
    [Required]
    public int ApprovalFlowId { get; set; }

    [Required]
    public int StageOrder { get; set; }

    [Required]
    [StringLength(200)]
    public string StageName { get; set; } = string.Empty;

    [Required]
    public int EmployeeTypeId { get; set; }

    public int SlaDays { get; set; } = 3;

    public bool CanVerifyDocument { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool IsFinalStage { get; set; }
}

public class UpdateApprovalFlowStageMasterDto
{
    [Required]
    public int ApprovalFlowId { get; set; }

    [Required]
    public int StageOrder { get; set; }

    [Required]
    [StringLength(200)]
    public string StageName { get; set; } = string.Empty;

    [Required]
    public int EmployeeTypeId { get; set; }

    public int SlaDays { get; set; }

    public bool CanVerifyDocument { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool IsFinalStage { get; set; }
}

public class ApprovalFlowStageMasterQueryParameters : BaseQueryParameters
{
    public int? ApprovalFlowId { get; set; }
    public int? EmployeeTypeId { get; set; }
}
