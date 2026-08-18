using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculation;

/// <summary>
/// Custom DTOs (not <see cref="BaseDtos"/>): RetrospectiveTaxCalculationEntity uses a BIGINT key
/// and has no IsActive/UpdatedBy/UpdatedDate columns — calculation runs are immutable
/// transaction records produced by the retro engine, not editable master data.
/// </summary>
public class RetrospectiveTaxCalculationDto
{
    public long Id { get; set; }
    public int PropertyId { get; set; }
    public string CalculationMode { get; set; } = string.Empty;
    public int? FloorId { get; set; }
    public int? AppliedRuleId { get; set; }
    public int? AppliedTaxPolicyId { get; set; }
    public DateTime AssessmentDate { get; set; }
    public DateTime? PolicyStartDate { get; set; }
    public DateTime? LegalBoundaryDate { get; set; }
    public DateTime? RuleBoundaryDate { get; set; }
    public DateTime? ChargeableStartDate { get; set; }
    public DateTime? ChargeableEndDate { get; set; }
    public decimal BaseTaxAmount { get; set; }
    public decimal RetrospectiveTaxAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? AuthorizationStatus { get; set; }
    public string CalculationStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateRetrospectiveTaxCalculationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculation_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    /// <summary>PROPERTY / FLOOR</summary>
    [Required(ErrorMessage = "RetrospectiveTaxCalculation_CalculationMode_Required")]
    [StringLength(20, ErrorMessage = "RetrospectiveTaxCalculation_CalculationMode_MaxLen_20")]
    public string CalculationMode { get; set; } = string.Empty;

    public int? FloorId { get; set; }
    public int? AppliedRuleId { get; set; }
    public int? AppliedTaxPolicyId { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculation_AssessmentDate_Required")]
    public DateTime AssessmentDate { get; set; }

    public DateTime? PolicyStartDate { get; set; }
    public DateTime? LegalBoundaryDate { get; set; }
    public DateTime? RuleBoundaryDate { get; set; }
    public DateTime? ChargeableStartDate { get; set; }
    public DateTime? ChargeableEndDate { get; set; }

    public decimal BaseTaxAmount { get; set; }
    public decimal RetrospectiveTaxAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }

    [StringLength(30, ErrorMessage = "RetrospectiveTaxCalculation_AuthorizationStatus_MaxLen_30")]
    public string? AuthorizationStatus { get; set; }

    /// <summary>Calculated / ManualReview / Failed</summary>
    [Required(ErrorMessage = "RetrospectiveTaxCalculation_CalculationStatus_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveTaxCalculation_CalculationStatus_MaxLen_30")]
    public string CalculationStatus { get; set; } = "Calculated";

    [StringLength(1000, ErrorMessage = "RetrospectiveTaxCalculation_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }

    public int? CreatedBy { get; set; }
}

public class UpdateRetrospectiveTaxCalculationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculation_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculation_CalculationMode_Required")]
    [StringLength(20, ErrorMessage = "RetrospectiveTaxCalculation_CalculationMode_MaxLen_20")]
    public string CalculationMode { get; set; } = string.Empty;

    public int? FloorId { get; set; }
    public int? AppliedRuleId { get; set; }
    public int? AppliedTaxPolicyId { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculation_AssessmentDate_Required")]
    public DateTime AssessmentDate { get; set; }

    public DateTime? PolicyStartDate { get; set; }
    public DateTime? LegalBoundaryDate { get; set; }
    public DateTime? RuleBoundaryDate { get; set; }
    public DateTime? ChargeableStartDate { get; set; }
    public DateTime? ChargeableEndDate { get; set; }

    public decimal BaseTaxAmount { get; set; }
    public decimal RetrospectiveTaxAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }

    [StringLength(30, ErrorMessage = "RetrospectiveTaxCalculation_AuthorizationStatus_MaxLen_30")]
    public string? AuthorizationStatus { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculation_CalculationStatus_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveTaxCalculation_CalculationStatus_MaxLen_30")]
    public string CalculationStatus { get; set; } = "Calculated";

    [StringLength(1000, ErrorMessage = "RetrospectiveTaxCalculation_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }
}
