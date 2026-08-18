using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;

/// <summary>
/// Custom DTOs (not <see cref="BaseDtos"/>/<see cref="CreateBaseDtos"/>/<see cref="UpdateBaseDtos"/>):
/// RetrospectiveRuleSummaryEntity has no UpdatedBy/UpdatedDate columns, since a summary is
/// regenerated (not edited) whenever the owning rule changes.
/// </summary>
public class RetrospectiveRuleSummaryDto
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public string? WhenSummary { get; set; }
    public string? TaxSummary { get; set; }
    public string? PenaltySummary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateRetrospectiveRuleSummaryDto
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleSummary_RuleId_Invalid")]
    public int RuleId { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_WhenSummary_MaxLen_1000")]
    public string? WhenSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_TaxSummary_MaxLen_1000")]
    public string? TaxSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_PenaltySummary_MaxLen_1000")]
    public string? PenaltySummary { get; set; }

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }
}

public class UpdateRetrospectiveRuleSummaryDto
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleSummary_RuleId_Invalid")]
    public int RuleId { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_WhenSummary_MaxLen_1000")]
    public string? WhenSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_TaxSummary_MaxLen_1000")]
    public string? TaxSummary { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleSummary_PenaltySummary_MaxLen_1000")]
    public string? PenaltySummary { get; set; }

    public bool IsActive { get; set; }
}
