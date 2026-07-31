using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;

/// <summary>
/// Display names resolved by joining the lease's FK ids against their master tables
/// (AssetMaster/AssetCategoryMaster/ApplicationTypeMaster). Not backed by columns on
/// AMS.AssetLeaseRentDetails itself.
/// </summary>
public class AssetLeaseRentDetailsNamesDto
{
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public string? AssetCategoryName { get; set; }
    public string? ApplicationTypeName { get; set; }
    public string? FloorDescription { get; set; }
}

public class AssetLeaseRentDetailsDto : BaseDtos
{
    public int? ParentAssetId { get; set; }
    public int AssetId { get; set; }
    public int? FloorDetailsId { get; set; }
    public string? ShopNo { get; set; }
    public string? ShopName { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantMobile { get; set; } = string.Empty;
    public string? TenantEmail { get; set; }
    public string TenantType { get; set; } = "Individual";
    public string? TenantAadhaarNo { get; set; }
    public string? TenantPanCardNo { get; set; }
    public string? TenantAddress { get; set; }
    public string? GSTNo { get; set; }
    public decimal? TotalAreaSqFt { get; set; }
    public int? ApplicationTypeId { get; set; }
    public string LeaseType { get; set; } = "Rent";
    public DateTime LeaseStartDate { get; set; }
    public DateTime? LeaseEndDate { get; set; }
    public int? Duration { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal? RentAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string? DepositType { get; set; }
    public string PaymentFrequency { get; set; } = "Monthly";
    public string? AgreementId { get; set; }
    public string? IncrementFrequency { get; set; }
    public string? IncrementType { get; set; }
    public double? IncrementValue { get; set; }
    public string? IncrementMethod { get; set; }
    public string? Reason { get; set; }
    public string WorkflowStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public bool IsRejection { get; set; }
    public int? RejectionBy { get; set; }
    public DateTime? RejectionDate { get; set; }
    public bool IsVerified { get; set; }
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedDate { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? LeaseDurationDisplay { get; set; }
    public string? RentAmountDisplay { get; set; }
    public AssetLeaseRentDetailsNamesDto Names { get; set; } = new();

    // Flat mirror of Names.AssetNo/AssetName/AssetCategoryName — list/grid consumers (e.g. the
    // Verification/Approval tables on manage-renters) read the flat shape, not the nested Names.
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public string? AssetCategoryName { get; set; }
}

public class CreateAssetLeaseRentDetailsDto : CreateBaseDtos
{
    public int? ParentAssetId { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_AssetId_Required")]
    public int AssetId { get; set; }

    public int? FloorDetailsId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_ShopNo_MaxLengthExceeded_50")]
    public string? ShopNo { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetLeaseRentDetails_ShopName_MaxLengthExceeded_200")]
    public string? ShopName { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantName_Required")]
    [StringLength(500, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantName_MaxLengthExceeded_500")]
    public string TenantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantMobile_Required")]
    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantMobile_MaxLengthExceeded_20")]
    public string TenantMobile { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantEmail_MaxLengthExceeded_200")]
    [EmailAddress(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantEmail_Invalid")]
    public string? TenantEmail { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantType_Required")]
    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantType_MaxLengthExceeded_50")]
    public string TenantType { get; set; } = "Individual";

    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantAadhaarNo_MaxLengthExceeded_20")]
    public string? TenantAadhaarNo { get; set; }

    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantPanCardNo_MaxLengthExceeded_20")]
    public string? TenantPanCardNo { get; set; }

    [StringLength(500, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantAddress_MaxLengthExceeded_500")]
    public string? TenantAddress { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_GSTNo_MaxLengthExceeded_50")]
    public string? GSTNo { get; set; }

    public decimal? TotalAreaSqFt { get; set; }
    public int? ApplicationTypeId { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_LeaseType_Required")]
    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_LeaseType_MaxLengthExceeded_20")]
    public string LeaseType { get; set; } = "Rent";

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_LeaseStartDate_Required")]
    public DateTime LeaseStartDate { get; set; }

    public DateTime? LeaseEndDate { get; set; }
    public int? Duration { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_AssetLeaseRentDetails_MonthlyRent_InvalidRange")]
    public decimal MonthlyRent { get; set; }

    public decimal? RentAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetLeaseRentDetails_SecurityDeposit_InvalidRange")]
    public decimal SecurityDeposit { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_DepositType_MaxLengthExceeded_50")]
    public string? DepositType { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_PaymentFrequency_Required")]
    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_PaymentFrequency_MaxLengthExceeded_20")]
    public string PaymentFrequency { get; set; } = "Monthly";

    [StringLength(25, ErrorMessage = "AMS_AssetLeaseRentDetails_AgreementId_MaxLengthExceeded_25")]
    public string? AgreementId { get; set; }

    [StringLength(35, ErrorMessage = "AMS_AssetLeaseRentDetails_IncrementFrequency_MaxLengthExceeded_35")]
    public string? IncrementFrequency { get; set; }

    [StringLength(35, ErrorMessage = "AMS_AssetLeaseRentDetails_IncrementType_MaxLengthExceeded_35")]
    public string? IncrementType { get; set; }

    public double? IncrementValue { get; set; }

    [StringLength(35, ErrorMessage = "AMS_AssetLeaseRentDetails_IncrementMethod_MaxLengthExceeded_35")]
    public string? IncrementMethod { get; set; }

    [StringLength(1000, ErrorMessage = "AMS_AssetLeaseRentDetails_Reason_MaxLengthExceeded_1000")]
    public string? Reason { get; set; }

    [StringLength(30, ErrorMessage = "AMS_AssetLeaseRentDetails_WorkflowStatus_MaxLengthExceeded_30")]
    public string WorkflowStatus { get; set; } = "Pending";
}

public class UpdateAssetLeaseRentDetailsDto : UpdateBaseDtos
{
    public int? ParentAssetId { get; set; }
    public int AssetId { get; set; }
    public int? FloorDetailsId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_ShopNo_MaxLengthExceeded_50")]
    public string? ShopNo { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetLeaseRentDetails_ShopName_MaxLengthExceeded_200")]
    public string? ShopName { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantName_Required")]
    [StringLength(500, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantName_MaxLengthExceeded_500")]
    public string TenantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantMobile_Required")]
    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantMobile_MaxLengthExceeded_20")]
    public string TenantMobile { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantEmail_MaxLengthExceeded_200")]
    [EmailAddress(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantEmail_Invalid")]
    public string? TenantEmail { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_TenantType_Required")]
    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantType_MaxLengthExceeded_50")]
    public string TenantType { get; set; } = "Individual";

    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantAadhaarNo_MaxLengthExceeded_20")]
    public string? TenantAadhaarNo { get; set; }

    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantPanCardNo_MaxLengthExceeded_20")]
    public string? TenantPanCardNo { get; set; }

    [StringLength(500, ErrorMessage = "AMS_AssetLeaseRentDetails_TenantAddress_MaxLengthExceeded_500")]
    public string? TenantAddress { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_GSTNo_MaxLengthExceeded_50")]
    public string? GSTNo { get; set; }

    public decimal? TotalAreaSqFt { get; set; }
    public int? ApplicationTypeId { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_LeaseType_Required")]
    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_LeaseType_MaxLengthExceeded_20")]
    public string LeaseType { get; set; } = "Rent";

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_LeaseStartDate_Required")]
    public DateTime LeaseStartDate { get; set; }

    public DateTime? LeaseEndDate { get; set; }
    public int? Duration { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_AssetLeaseRentDetails_MonthlyRent_InvalidRange")]
    public decimal MonthlyRent { get; set; }

    public decimal? RentAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetLeaseRentDetails_SecurityDeposit_InvalidRange")]
    public decimal SecurityDeposit { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetLeaseRentDetails_DepositType_MaxLengthExceeded_50")]
    public string? DepositType { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetails_PaymentFrequency_Required")]
    [StringLength(20, ErrorMessage = "AMS_AssetLeaseRentDetails_PaymentFrequency_MaxLengthExceeded_20")]
    public string PaymentFrequency { get; set; } = "Monthly";

    [StringLength(25, ErrorMessage = "AMS_AssetLeaseRentDetails_AgreementId_MaxLengthExceeded_25")]
    public string? AgreementId { get; set; }

    [StringLength(35, ErrorMessage = "AMS_AssetLeaseRentDetails_IncrementFrequency_MaxLengthExceeded_35")]
    public string? IncrementFrequency { get; set; }

    [StringLength(35, ErrorMessage = "AMS_AssetLeaseRentDetails_IncrementType_MaxLengthExceeded_35")]
    public string? IncrementType { get; set; }

    public double? IncrementValue { get; set; }

    [StringLength(35, ErrorMessage = "AMS_AssetLeaseRentDetails_IncrementMethod_MaxLengthExceeded_35")]
    public string? IncrementMethod { get; set; }

    [StringLength(1000, ErrorMessage = "AMS_AssetLeaseRentDetails_Reason_MaxLengthExceeded_1000")]
    public string? Reason { get; set; }

    [StringLength(30, ErrorMessage = "AMS_AssetLeaseRentDetails_WorkflowStatus_MaxLengthExceeded_30")]
    public string WorkflowStatus { get; set; } = "Pending";
}

/// <summary>
/// Payload for a workflow transition (verify / approve / reject / revert).
/// </summary>
public class LeaseWorkflowActionDto
{
    [StringLength(500, ErrorMessage = "AMS_LeaseWorkflowAction_Remarks_MaxLengthExceeded_500")]
    public string? Remarks { get; set; }
}

/// <summary>
/// Reject payload — reason is mandatory.
/// </summary>
public class LeaseRejectDto
{
    [Required(ErrorMessage = "AMS_LeaseReject_Reason_Required")]
    [StringLength(500, ErrorMessage = "AMS_LeaseReject_Reason_MaxLengthExceeded_500")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Stats bar counts shown above the lease registration tables.
/// </summary>
public class LeaseStatsDto
{
    public int TotalApproved { get; set; }
    public int TotalVerified { get; set; }
    public int VerificationPending { get; set; }
    public int ApprovalPending { get; set; }
    public int TotalRejected { get; set; }
}

public class AssetLeaseRentDetailsPagedResult : NtisPlatform.Application.Models.PagedResult<AssetLeaseRentDetailsDto>
{
    public LeaseStatsDto Stats { get; set; } = new();

    public AssetLeaseRentDetailsPagedResult() { }

    public AssetLeaseRentDetailsPagedResult(
        System.Collections.Generic.IEnumerable<AssetLeaseRentDetailsDto> items, int totalCount, int pageNumber, int pageSize, LeaseStatsDto stats)
        : base(items, totalCount, pageNumber, pageSize)
    {
        Stats = stats;
    }
}

/// <summary>
/// Snapshot of a previous tenant entry from LeaseRentRegistrationHistory,
/// used to populate the "Previous Tenant Information" tab in the registration drawer.
/// </summary>
public class AssetLeaseRentPreviousTenantHistoryDto
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public DateTime PerformedDate { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Remarks { get; set; }

    // Tenant snapshot
    public string TenantName { get; set; } = string.Empty;
    public string TenantMobile { get; set; } = string.Empty;
    public string? TenantEmail { get; set; }
    public string? TenantType { get; set; }
    public string? TenantAadhaarNo { get; set; }
    public string? TenantPanCardNo { get; set; }
    public string? TenantAddress { get; set; }

    // Previous tenant snapshot
    public string? PreviousTenantName { get; set; }
    public string? PreviousTenantMobile { get; set; }

    // Lease snapshot
    public string LeaseType { get; set; } = string.Empty;
    public string? ShopNo { get; set; }
    public string? Floor { get; set; }
    public string? ShopName { get; set; }
    public DateTime? OldLeaseStartDate { get; set; }
    public DateTime? OldLeaseEndDate { get; set; }
    public DateTime? LeaseStartDate { get; set; }
    public DateTime? LeaseEndDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    // Financial snapshot
    public decimal? PreviousMonthlyRent { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string PaymentFrequency { get; set; } = string.Empty;

    // Status snapshot
    public string WorkflowStatus { get; set; } = string.Empty;
    public string RentStatus { get; set; } = string.Empty;
}

/// <summary>
/// DTO for uploading documents for AssetLeaseRentDetails.
/// Documents are stored in Core.Document and linked via Core.DocumentBinding.
/// </summary>
public class AssetLeaseRentDetailsDocumentUploadFormDto
{
    [Required(ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_File_Required")]
    public Microsoft.AspNetCore.Http.IFormFile File { get; set; } = null!;

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_AssetLeaseRentDetailsId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_AssetLeaseRentDetailsId_InvalidRange")]
    public int AssetLeaseRentDetailsId { get; set; }

    [Required(ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_ModuleId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_ModuleId_InvalidRange")]
    public int ModuleId { get; set; }

    public int? FloorDetailId { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_DocumentTitle_MaxLengthExceeded_200")]
    public string? DocumentTitle { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_DocumentType_MaxLengthExceeded_100")]
    public string? DocumentType { get; set; }

    public DateTime? DocumentDate { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_DocumentNumber_MaxLengthExceeded_100")]
    public string? DocumentNumber { get; set; }

    [StringLength(500, ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_Remarks_MaxLengthExceeded_500")]
    public string? Remarks { get; set; }

    public bool IsPrimaryDocument { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetLeaseRentDetailsDocumentUploadForm_BindingPurpose_MaxLengthExceeded_200")]
    public string? BindingPurpose { get; set; }

    public int? UploadedByUserId { get; set; }
}

/// <summary>
/// Response DTO for document upload operations.
/// </summary>
public class AssetLeaseRentDetailsDocumentUploadResponseDto
{
    public Guid DocumentGuid { get; set; }
    public int DocumentId { get; set; }
    public int DocumentBindingId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

/// <summary>
/// DTO for document information linked to AssetLeaseRentDetails.
/// </summary>
public class AssetLeaseRentDetailsDocumentDto
{
    public int AssetDocumentId { get; set; }
    public int DocumentId { get; set; }
    public Guid DocumentGuid { get; set; }
    public int DocumentBindingId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentTitle { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Remarks { get; set; }
    public bool IsPrimaryDocument { get; set; }
    public string? BindingPurpose { get; set; }
    public int UploadedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int DownloadCount { get; set; }
}
