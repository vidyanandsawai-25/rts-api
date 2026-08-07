using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>HYBRID strategy configuration for a tax.</summary>
public class TaxHybridConfigDto
{
    public int TaxId { get; set; }

    /// <summary>MASTER_THEN_CONDITION | CONDITION_THEN_MASTER.</summary>
    [Required]
    public string EvaluationPriority { get; set; } = "MASTER_THEN_CONDITION";

    /// <summary>DEFAULT_ZERO | CONDITION_RULE.</summary>
    [Required]
    public string FallbackStrategy { get; set; } = "DEFAULT_ZERO";

    /// <summary>NONE | RV | ALV.</summary>
    [Required]
    public string ResultBase { get; set; } = "NONE";

    public int? UpdatedBy { get; set; }
}
