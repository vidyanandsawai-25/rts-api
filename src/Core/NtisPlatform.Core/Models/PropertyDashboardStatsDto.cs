namespace NtisPlatform.Core.Models;

/// <summary>
/// Dashboard statistics for property search screen
/// Shows counts at the top of the property search page
/// </summary>
public class PropertyDashboardStatsDto
{
    /// <summary>
    /// Total count of all registered properties in PropertyMast
    /// </summary>
    public int RegisteredPropertyCount { get; set; }

    /// <summary>
    /// Count of properties with PropertyNo present (geo-sequencing completed)
    /// </summary>
    public int GeoSequencingPropertyCount { get; set; }

    /// <summary>
    /// Count of survey properties - Currently 0 (Work in Progress)
    /// </summary>
    public int SurveyPropertyCount { get; set; }

    /// <summary>
    /// Count of data processing properties - Currently 0 (Work in Progress)
    /// </summary>
    public int DataProcessingPropertyCount { get; set; }

    /// <summary>
    /// Count of quality analysis properties - Currently 0 (Work in Progress)
    /// </summary>
    public int QualityAnalysisPropertyCount { get; set; }

    /// <summary>
    /// Count of assessment completed properties - Currently 0 (Work in Progress)
    /// </summary>
    public int AssessmentCompletedPropertyCount { get; set; }
}
