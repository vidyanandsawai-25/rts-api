using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveCalculationEvidence;

/// <summary>
/// Custom DTOs (not <see cref="BaseDtos"/>): RetrospectiveCalculationEvidenceEntity uses a BIGINT
/// key and has no CreatedBy/IsActive/UpdatedBy/UpdatedDate columns.
/// </summary>
public class RetrospectiveCalculationEvidenceDto
{
    public long Id { get; set; }
    public long CalculationId { get; set; }
    public int EvidenceTypeId { get; set; }
    public DateTime? EvidenceDate { get; set; }
    public bool IsAvailable { get; set; }
    public string? SourceReference { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateRetrospectiveCalculationEvidenceDto
{
    [Range(1, long.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_CalculationId_Invalid")]
    public long CalculationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_EvidenceTypeId_Invalid")]
    public int EvidenceTypeId { get; set; }

    public DateTime? EvidenceDate { get; set; }
    public bool IsAvailable { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveCalculationEvidence_SourceReference_MaxLen_200")]
    public string? SourceReference { get; set; }
}

public class UpdateRetrospectiveCalculationEvidenceDto
{
    [Range(1, long.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_CalculationId_Invalid")]
    public long CalculationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveCalculationEvidence_EvidenceTypeId_Invalid")]
    public int EvidenceTypeId { get; set; }

    public DateTime? EvidenceDate { get; set; }
    public bool IsAvailable { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveCalculationEvidence_SourceReference_MaxLen_200")]
    public string? SourceReference { get; set; }
}
