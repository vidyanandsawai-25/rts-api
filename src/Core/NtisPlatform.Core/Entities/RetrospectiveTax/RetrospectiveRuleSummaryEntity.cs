using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Generated human-readable summary shown on the Rule Summary screen section.
/// </summary>
[Table("RetrospectiveRuleSummary", Schema = "PTIS")]
public class RetrospectiveRuleSummaryEntity : BaseEntity
{
    public int RuleId { get; set; }

    public string? WhenSummary { get; set; }

    public string? TaxSummary { get; set; }

    public string? PenaltySummary { get; set; }

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }
}
