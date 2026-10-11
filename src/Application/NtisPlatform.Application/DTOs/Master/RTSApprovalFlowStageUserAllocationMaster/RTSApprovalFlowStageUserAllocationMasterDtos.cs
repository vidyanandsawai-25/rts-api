using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.RTSApprovalFlowStageUserAllocationMaster;

public class RTSApprovalFlowStageUserAllocationMasterDto : BaseDtos
{
    public int? ApprovalFlowStageId { get; set; }
    public int? UserId { get; set; }
    public int? Priority { get; set; }

    // Read-only officer profile fields, populated from CORE.UserMaster for display.
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? UserName { get; set; }
}

public class CreateRTSApprovalFlowStageUserAllocationMasterDto : CreateBaseDtos
{
    public int? ApprovalFlowStageId { get; set; }
    public int? UserId { get; set; }
    public int? Priority { get; set; }
}

public class UpdateRTSApprovalFlowStageUserAllocationMasterDto : UpdateBaseDtos
{
    public int? ApprovalFlowStageId { get; set; }
    public int? UserId { get; set; }
    public int? Priority { get; set; }
}

public class RTSApprovalFlowStageUserAllocationMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ApprovalFlowStageId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? UserId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? Priority { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }
}
