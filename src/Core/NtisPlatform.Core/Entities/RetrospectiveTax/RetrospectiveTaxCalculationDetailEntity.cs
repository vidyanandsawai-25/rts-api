using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Floor-wise and year-wise breakup of a retrospective tax calculation.
/// </summary>
[Table("RetrospectiveTaxCalculationDetail", Schema = "PTIS")]
public class RetrospectiveTaxCalculationDetailEntity : BaseEntity, IHardDeletable
{
    public int CalculationId { get; set; }

    public int PropertyId { get; set; }

    public int FloorId { get; set; }

    /// <summary>Example: 2024-25</summary>
    public string FinancialYear { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public string? RateMode { get; set; }

    public string? PercentageMode { get; set; }

    public decimal BaseTaxAmount { get; set; }

    public decimal TaxMultiplier { get; set; } = 1.00m;

    public decimal RetrospectiveTaxAmount { get; set; }

    public decimal? PenaltyPercent { get; set; }

    public decimal PenaltyAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public bool MarkedForDeletion { get; set; }

    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual RetrospectiveTaxCalculationEntity? Calculation { get; set; }
}
