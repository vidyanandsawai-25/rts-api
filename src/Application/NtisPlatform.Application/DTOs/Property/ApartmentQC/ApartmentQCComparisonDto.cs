namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Wrapper DTO containing New Survey, Old Survey, and calculated Differences.
/// </summary>
public class ApartmentQCComparisonDto
{
    /// <summary>Details from the New Survey property.</summary>
    public NewSurveyPropertyDto NewSurvey { get; set; } = new();

    /// <summary>Details from the mapped Old Survey property.</summary>
    public OldSurveyPropertyDto OldSurvey { get; set; } = new();

    /// <summary>Column-by-column calculated differences between New Survey and Old Survey.</summary>
    public ApartmentQCPropertyDifferenceDto Difference { get; set; } = new();
}
