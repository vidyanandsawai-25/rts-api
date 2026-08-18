using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Generated human-readable summary shown on the Rule Summary screen section.
/// Does not inherit <see cref="BaseEntity"/>: the table has no UpdatedBy/UpdatedDate columns,
/// since summaries are regenerated (not edited) whenever the owning rule changes.
/// </summary>
[Table("RetrospectiveRuleSummary", Schema = "PTIS")]
public class RetrospectiveRuleSummaryEntity
{
    public int Id { get; set; }

    public int RuleId { get; set; }

    public string? WhenSummary { get; set; }

    public string? TaxSummary { get; set; }

    public string? PenaltySummary { get; set; }

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }
}
