using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Actual evidence dates used during a property/floor retrospective calculation.
/// </summary>
[Table("RetrospectiveCalculationEvidence", Schema = "PTIS")]
public class RetrospectiveCalculationEvidenceEntity : BaseEntity, IHardDeletable
{
    public int CalculationId { get; set; }

    public int EvidenceTypeId { get; set; }

    public DateTime? EvidenceDate { get; set; }

    public bool IsAvailable { get; set; }

    public string? SourceReference { get; set; }

    public bool MarkedForDeletion { get; set; }

    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual RetrospectiveTaxCalculationEntity? Calculation { get; set; }

    public virtual EvidenceTypeMasterEntity? EvidenceType { get; set; }
}
