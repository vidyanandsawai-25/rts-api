using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.TaxApplicability;

/// <summary>
/// Request DTO for fetching tax applicability details
/// </summary>
public class TaxApplicabilityRequestDto : BaseQueryParameters
{
    /// <summary>
    /// Property identifier
    /// </summary>
    public int PropertyId { get; set; }

    /// <summary>
    /// Assessment Year Range identifier
    /// </summary>
    public int AssessmentYearRangeId { get; set; }

    /// <summary>
    /// Type of Use identifier (from PTIS.TypeOfUseMaster)
    /// </summary>
    public int TypeOfUseId { get; set; }

    /// <summary>
    /// Rateable Value or Capital Value indicator / Calculation Type (e.g. RV or CV)
    /// </summary>
    public string CalculationType { get; set; } = string.Empty;
}
