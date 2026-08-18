using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Floor-wise and year-wise breakup of a retrospective tax calculation.
/// </summary>
[Table("RetrospectiveTaxCalculationDetail", Schema = "PTIS")]
public class RetrospectiveTaxCalculationDetailEntity
{
    public long Id { get; set; }

    public long CalculationId { get; set; }

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

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual RetrospectiveTaxCalculationEntity? Calculation { get; set; }
}
