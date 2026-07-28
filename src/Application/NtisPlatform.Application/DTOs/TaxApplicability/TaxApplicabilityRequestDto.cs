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
    /// Financial Year identifier
    /// </summary>
    public int FinancialYearId { get; set; }

    /// <summary>
    /// Type of Use Group identifier
    /// </summary>
    public int TypeOfUseGroupId { get; set; }

    /// <summary>
    /// Rateable Value or Capital Value indicator / Calculation Type (e.g. RV or CV)
    /// </summary>
    public string CalculationType { get; set; } = string.Empty;
}
