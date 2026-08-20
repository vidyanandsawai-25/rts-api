using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;

public class RetrospectiveRuleSummaryDto : BaseDtos
{
    public int RuleId { get; set; }
    public string? WhenSummary { get; set; }
    public string? TaxSummary { get; set; }
    public string? PenaltySummary { get; set; }
}

public class CreateRetrospectiveRuleSummaryDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleSummary_RuleId_Invalid")]
    public int RuleId { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_WhenSummary_MaxLen_1000")]
    public string? WhenSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_TaxSummary_MaxLen_1000")]
    public string? TaxSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_PenaltySummary_MaxLen_1000")]
    public string? PenaltySummary { get; set; }
}

public class UpdateRetrospectiveRuleSummaryDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleSummary_RuleId_Invalid")]
    public int RuleId { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_WhenSummary_MaxLen_1000")]
    public string? WhenSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_TaxSummary_MaxLen_1000")]
    public string? TaxSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_PenaltySummary_MaxLen_1000")]
    public string? PenaltySummary { get; set; }
}
