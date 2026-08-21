using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

// ─────────────────────────────────────────────────────
// Lookup
// ─────────────────────────────────────────────────────

/// <summary>Response DTO for a single signing authority</summary>
public class SignAuthorityDto
{
    public int Id { get; set; }
    public string AuthorityName { get; set; } = string.Empty;
    public string AuthorityCode { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
}

// ─────────────────────────────────────────────────────
// Eligible Properties (before approving)
// ─────────────────────────────────────────────────────

/// <summary>A property eligible to be signed by a given authority</summary>
public class EligiblePropertyDto
{
    public int PropertyId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? WardName { get; set; }
    public string? ZoneName { get; set; }
}

// ─────────────────────────────────────────────────────
// Submit Approval
// ─────────────────────────────────────────────────────

/// <summary>Single property approval item inside a batch request</summary>
public class PropertyApprovalItemDto
{
    /// <summary>Property to approve</summary>
    public int PropertyId { get; set; }

    /// <summary>Optional remarks or comments for this approval</summary>
    public string? Remarks { get; set; }
}

/// <summary>Request body to submit one or more property approvals</summary>
public class SubmitSignatureRequestDto
{
    /// <summary>FK → SignAuthorityMaster — which authority role is signing</summary>
    public int SignAuthorityId { get; set; }

    /// <summary>One or more properties to approve in this batch</summary>
    public List<PropertyApprovalItemDto> PropertyApprovals { get; set; } = new();
}

/// <summary>Response after submitting approvals</summary>
public class SubmitSignatureResponseDto
{
    public int ApprovedCount { get; set; }
    public List<RejectedPropertyDto> RejectedProperties { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>A property that was rejected during approval submission</summary>
public class RejectedPropertyDto
{
    public int PropertyId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────
// My Approvals / List
// ─────────────────────────────────────────────────────

/// <summary>One approval record returned in list queries</summary>
public class SignatureApprovalDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? WardName { get; set; }
    public int SignAuthorityId { get; set; }
    public string? AuthorityName { get; set; }
    public string? ApprovedByUserName { get; set; }
    public string? Remarks { get; set; }
    public DateTime? ApprovedOn { get; set; }  // CreatedDate
}

// ─────────────────────────────────────────────────────
// Property Approval Status
// ─────────────────────────────────────────────────────

/// <summary>Full approval chain status of a single property</summary>
public class PropertySignatureStatusDto
{
    public int PropertyId { get; set; }
    public string? PropertyNo { get; set; }

    /// <summary>All authority approvals recorded for this property</summary>
    public List<AuthorityApprovalStatusDto> Approvals { get; set; } = new();

    /// <summary>Name of the next authority that needs to sign, or null if fully approved</summary>
    public string? PendingAuthority { get; set; }

    /// <summary>True if all 4 authorities have signed</summary>
    public bool IsFullyApproved { get; set; }
}

/// <summary>One authority's approval status for a property</summary>
public class AuthorityApprovalStatusDto
{
    public int SignAuthorityId { get; set; }
    public string AuthorityName { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? Remarks { get; set; }
}

// ─────────────────────────────────────────────────────
// Sign Authority Dashboard Grid
// ─────────────────────────────────────────────────────

/// <summary>
/// Response DTO for Sign Authority dashboard grid data
/// </summary>
public class SignAuthorityGridResponseDto
{
    public List<SignAuthorityZoneDataDto> ZoneData { get; set; } = new();
    public SignAuthorityZoneDataDto TotalRow { get; set; } = new();
    public SignAuthorityZoneDataDto GrandTotalRow { get; set; } = new();
}

/// <summary>
/// Zone-wise sign-off status breakdown
/// </summary>
public class SignAuthorityZoneDataDto
{
    public int? ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;
    public int? WardId { get; set; }
    public string? WardName { get; set; }
    public int TotalStructure { get; set; }
    public int TotalUnit { get; set; }
    public decimal TotalDemand { get; set; }
    public List<SignAuthorityClassificationDto> Classifications { get; set; } = new();
}

/// <summary>
/// Sign-off metrics classification per authority role (e.g. "Clerk", "Tax Inspector", etc.)
/// </summary>
public class SignAuthorityClassificationDto
{
    public int TypeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Structure { get; set; } // Signed Structures
    public int Unit { get; set; }      // Signed Units
    public int PendingStructure { get; set; }
    public int PendingUnit { get; set; }
    public decimal OldDemand { get; set; }
    public decimal CurrentDemand { get; set; }
    public decimal RetroDemand { get; set; }
    public decimal TotalDemand { get; set; }
    public decimal AdditionalRevenueGenerated { get; set; }
}

/// <summary>
/// Building-level row for the property signature approval sub-grid.
/// </summary>
public class PropertySignatureSubGridDto
{
    public string BuildingNo { get; set; } = string.Empty;
    public string? NoticeNo { get; set; }
    public int Units { get; set; }
    public decimal TotalDemand { get; set; }
    public List<PropertySignatureAuthoritySignDto> AuthoritySignatures { get; set; } = new();
}

public class PropertySignatureBuildingWiseQueryParameters
{
    public int WardId { get; set; }
    public int WorkflowStageId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? NoticeNo { get; set; }
}

public class PropertySignaturePropertyWiseQueryParameters
{
    public string PropertyNo { get; set; } = string.Empty;
    public string? SearchType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Dynamic authority status from SignAuthorityMaster for a sub-grid row.
/// </summary>
public class PropertySignatureAuthoritySignDto
{
    public int SignAuthorityId { get; set; }
    public string AuthorityName { get; set; } = string.Empty;
    public string AuthorityCode { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public int IsSigned { get; set; }
}

/// <summary>
/// Property-level row for a building/property approval detail grid.
/// </summary>
public class PropertySignaturePropertyWiseDto
{
    public int PropertyId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string NewPropertyNo { get; set; } = string.Empty;
    public string OldPropertyNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OccupierName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string SocietyName { get; set; } = string.Empty;
    public string BuilderName { get; set; } = string.Empty;
    public string WingNo { get; set; } = string.Empty;
    public string FlatNo { get; set; } = string.Empty;
    public PropertySignaturePropertyRecordDto OldRecord { get; set; } = new();
    public PropertySignaturePropertyRecordDto NewRecord { get; set; } = new();
    public string PropertyType { get; set; } = string.Empty;
    public decimal TotalDemand { get; set; }
    public int ClerkSign { get; set; }
    public int TaxInspectorSign { get; set; }
    public int AssistantCommissionerSign { get; set; }
    public int DeputyCommissionerSign { get; set; }
    public int AdditionalCommissionerSign { get; set; }
    public List<PropertySignatureAuthoritySignDto> AuthoritySignatures { get; set; } = new();
}

public class PropertySignaturePropertyRecordDto
{
    public string Area { get; set; } = "N/A";
    public string Use { get; set; } = "N/A";
    public string Year { get; set; } = "N/A";
    public string RV { get; set; } = "N/A";
    public string Tax { get; set; } = "N/A";
}

public class PropertySignaturePagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

public class PropertySignaturePendingExportDto
{
    public string Zone { get; set; } = string.Empty;
    public string BuildingNo { get; set; } = string.Empty;
    public string SrNoticeNo { get; set; } = string.Empty;
    public string PendingSignAt { get; set; } = string.Empty;
    public string PendingOfficerName { get; set; } = string.Empty;
}

public class PropertySignaturePendingExportAuthorityDto
{
    public int SignAuthorityId { get; set; }
    public string AuthorityName { get; set; } = string.Empty;
    public string OfficerName { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
}

public class PropertySignaturePendingExportSourceDto
{
    public int PropertyId { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string BuildingNo { get; set; } = string.Empty;
    public string SrNoticeNo { get; set; } = string.Empty;
    public List<int> SignedAuthorityIds { get; set; } = new();
}

public class PropertySignaturePendingSignDto
{
    public int PropertyId { get; set; }
    public int SignAuthorityId { get; set; }
    public string StructureName { get; set; } = string.Empty;
    public string SrNoticeNo { get; set; } = string.Empty;
    public int NoOfUnits { get; set; }
    public decimal Demand { get; set; }
    public string SignStatus { get; set; } = string.Empty;
    public string AuthorityCode { get; set; } = string.Empty;
}

public class PropertySignaturePendingSignSourceDto
{
    public int SignatureId { get; set; }
    public int PropertyId { get; set; }
    public int SignAuthorityId { get; set; }
    public int UnitPropertyId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public string? PartitionNo { get; set; }
    public string SrNoticeNo { get; set; } = string.Empty;
    public string SignStatus { get; set; } = string.Empty;
    public string AuthorityCode { get; set; } = string.Empty;
    public decimal UnitDemand { get; set; }
}

public abstract class PropertySignatureUpdateSignIdentifiersBaseDto
{
    [Range(1, int.MaxValue, ErrorMessage = "The UserId is Required")]
    public int UserId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The SignAuthorityId is Required")]
    public int SignAuthorityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyId is Required")]
    public int PropertyId { get; set; }
}

public abstract class PropertySignatureUpdateSignBaseDto : PropertySignatureUpdateSignIdentifiersBaseDto
{
    [Required(ErrorMessage = "The AuthorityCode is Required")]
    public string AuthorityCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "The SignStatus is Required")]
    public string SignStatus { get; set; } = string.Empty;
}

public abstract class PropertySignatureUpdateSignResultBaseDto
{
    public string UpdatedSignStatus { get; set; } = string.Empty;
    public int? NextSignAuthorityId { get; set; }
    public string? NextSignStatus { get; set; }
}

public class PropertySignatureUpdateSignRequestDto : PropertySignatureUpdateSignBaseDto
{
}

public class PropertySignatureUpdateSignResponseDto : PropertySignatureUpdateSignResultBaseDto
{
    public int PropertyId { get; set; }
    public int UpdatedSignAuthorityId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PropertySignatureUpdateSignSourceDto : PropertySignatureUpdateSignBaseDto
{
    public int SignatureId { get; set; }
    public string? NoticeNo { get; set; }
    public bool IsActive { get; set; }
}

public class PropertySignatureUpdateSignCommandDto : PropertySignatureUpdateSignResultBaseDto
{
    public int SignatureId { get; set; }
    public int UserId { get; set; }
    public int PropertyId { get; set; }
    public int SignAuthorityId { get; set; }
    public string? NoticeNo { get; set; }
    public bool IsActive { get; set; }
    public int UpdatedBy { get; set; }
}
