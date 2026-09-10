namespace NtisPlatform.Application.DTOs.TaxApplicability;

/// <summary>
/// Summary header metrics for property tax applicability
/// </summary>
public class TaxApplicabilityHeaderSummaryDto
{
    public decimal TotalTax { get; set; }
    public decimal ResidentialRV { get; set; }
    public decimal CommercialRV { get; set; }
    public double Area { get; set; }
    public int Toilets { get; set; }
}

/// <summary>
/// Combined response DTO for tax applicability containing summary metrics, applicability lists, and calculations
/// </summary>
public class TaxApplicabilityResponseDto
{
    public int PropertyId { get; set; }
    public int AssessmentYearRangeId { get; set; }
    public int TypeOfUseId { get; set; }
    public int ApplicableCount { get; set; }
    public int ExemptedCount { get; set; }

    /// <summary>
    /// Summary cards metrics header (Total Tax, Residential RV, Commercial RV, Area, Toilets)
    /// </summary>
    public TaxApplicabilityHeaderSummaryDto Summary { get; set; } = new();

    public List<TaxApplicabilityDetailDto> ApplicableTaxes { get; set; } = new();
    public List<TaxApplicabilityDetailDto> ExemptedTaxes { get; set; } = new();
    public List<TaxApplicabilityCalculationDto> TaxCalculations { get; set; } = new();
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

/// <summary>
/// Response DTO for Tax Applicability Calculation summary based on Rateable Value and Special Tax Percentages
/// </summary>
public class TaxApplicabilityCalculationDto
{
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public int? CalculationModeId { get; set; }
    public string? CalculationMode { get; set; }
    public int? RuleDefinitionId { get; set; }
    public string? TypeOfUseIds { get; set; }
    public string? Descriptions { get; set; }
    public string? Types { get; set; }
    public string? BaseTypes { get; set; }
    public decimal? AverageTaxPercentage { get; set; }
    public string? ResultModes { get; set; }
    public string? ResultBases { get; set; }
    public string? ResultValues { get; set; }
    public string? MappingData { get; set; }
    public decimal? TaxAmount { get; set; }
    public bool IsApplicable { get; set; }
    public bool IsActive { get; set; }
    public bool AssessmentStatus { get; set; }
}

/// <summary>
/// Combined response DTO for calculation endpoint containing property metrics, counts, summary cards, and tax calculation items
/// </summary>
public class TaxApplicabilityCalculationResponseDto
{
    public int PropertyId { get; set; }
    public int AssessmentYearRangeId { get; set; }
    public int TypeOfUseId { get; set; }
    public int ApplicableCount { get; set; }
    public int ExemptedCount { get; set; }

    /// <summary>
    /// Property summary metrics header (Total Tax, Residential RV, Commercial RV, Area, Toilets)
    /// </summary>
    public TaxApplicabilityHeaderSummaryDto Summary { get; set; } = new();

    public List<TaxApplicabilityCalculationDto> TaxCalculations { get; set; } = new();
}
