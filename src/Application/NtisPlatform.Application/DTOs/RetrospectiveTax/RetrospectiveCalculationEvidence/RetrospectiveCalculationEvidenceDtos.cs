using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveCalculationEvidence;

/// <summary>
/// MarkedForDeletion/MarkedForDeletionDate are system-managed via the Purge endpoints, not user
/// input, so they're declared directly here rather than in <see cref="BaseDtos"/>.
/// </summary>
public class RetrospectiveCalculationEvidenceDto : BaseDtos
{
    public int CalculationId { get; set; }
    public int EvidenceTypeId { get; set; }
    public DateTime? EvidenceDate { get; set; }
    public bool IsAvailable { get; set; }
    public string? SourceReference { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateRetrospectiveCalculationEvidenceDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_CalculationId_Invalid")]
    public int CalculationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_EvidenceTypeId_Invalid")]
    public int EvidenceTypeId { get; set; }

    public DateTime? EvidenceDate { get; set; }
    public bool IsAvailable { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveCalculationEvidence_SourceReference_MaxLen_200")]
    public string? SourceReference { get; set; }
}

public class UpdateRetrospectiveCalculationEvidenceDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_CalculationId_Invalid")]
    public int CalculationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_EvidenceTypeId_Invalid")]
    public int EvidenceTypeId { get; set; }

    public DateTime? EvidenceDate { get; set; }
    public bool IsAvailable { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveCalculationEvidence_SourceReference_MaxLen_200")]
    public string? SourceReference { get; set; }
}
