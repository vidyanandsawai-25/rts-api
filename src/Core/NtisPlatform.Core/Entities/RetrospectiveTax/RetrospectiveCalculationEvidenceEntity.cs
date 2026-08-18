using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Actual evidence dates used during a property/floor retrospective calculation.
/// </summary>
[Table("RetrospectiveCalculationEvidence", Schema = "PTIS")]
public class RetrospectiveCalculationEvidenceEntity
{
    public long Id { get; set; }

    public long CalculationId { get; set; }

    public int EvidenceTypeId { get; set; }

    public DateTime? EvidenceDate { get; set; }

    public bool IsAvailable { get; set; }

    public string? SourceReference { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual RetrospectiveTaxCalculationEntity? Calculation { get; set; }

    public virtual EvidenceTypeMasterEntity? EvidenceType { get; set; }
}
