namespace NtisPlatform.Application.DTOs.TaxApplicability;

/// <summary>
/// Response DTO for tax applicability
/// </summary>
public class TaxApplicabilityResponseDto
{
    public int PropertyId { get; set; }
    public int AssessmentYearRangeId { get; set; }
    public int TypeOfUseId { get; set; }
    public int ApplicableCount { get; set; }
    public int ExemptedCount { get; set; }
    public List<TaxApplicabilityDetailDto> ApplicableTaxes { get; set; } = new();
    public List<TaxApplicabilityDetailDto> ExemptedTaxes { get; set; } = new();
}

/// <summary>
/// Tax detail DTO for individual tax in tax applicability
/// </summary>
public class TaxApplicabilityDetailDto
{
    public int TaxId { get; set; }
    public string TaxHead { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string? CalculationType { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsApplicable { get; set; }
    public bool IsActive { get; set; }
    public bool AssessmentStatus { get; set; }
}
