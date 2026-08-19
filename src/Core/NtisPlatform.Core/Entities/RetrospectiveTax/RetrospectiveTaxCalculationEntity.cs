using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Actual retrospective tax calculation transaction for a property or floor. Inherits
/// <see cref="BaseEntity"/> plus <see cref="IHardDeletable"/> for soft-mark-then-purge of
/// transaction records via the same Purge/BulkPurge endpoints used by the other 11 tables.
/// </summary>
[Table("RetrospectiveTaxCalculation", Schema = "PTIS")]
public class RetrospectiveTaxCalculationEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }

    /// <summary>PROPERTY / FLOOR</summary>
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

    /// <summary>AUTHORIZED / UNAUTHORIZED / UNDETERMINED</summary>
    public string? AuthorizationStatus { get; set; }

    /// <summary>Calculated / ManualReview / Failed</summary>
    public string CalculationStatus { get; set; } = "Calculated";

    public string? Remarks { get; set; }

    public bool MarkedForDeletion { get; set; }

    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual RetrospectiveRuleMasterEntity? AppliedRule { get; set; }

    public virtual RetrospectiveTaxPolicyEntity? AppliedTaxPolicy { get; set; }
}
