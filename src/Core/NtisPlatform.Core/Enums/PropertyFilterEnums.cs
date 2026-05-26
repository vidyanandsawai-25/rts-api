namespace NtisPlatform.Core.Enums;

/// <summary>
/// Dashboard card filter type for property search
/// Used when clicking on dashboard stats cards
/// </summary>
public enum DashboardFilterType
{
    /// <summary>
    /// No dashboard filter applied
    /// </summary>
    None = 0,

    /// <summary>
    /// Show all registered properties from PropertyMast
    /// </summary>
    RegisteredProperty = 1,

    /// <summary>
    /// Show properties with PropertyNo present (geo-sequencing completed)
    /// </summary>
    GeoSequencing = 2,

    /// <summary>
    /// Show survey properties - Currently shows "Work in Progress"
    /// </summary>
    Survey = 3,

    /// <summary>
    /// Show data processing properties - Currently shows "Work in Progress"
    /// </summary>
    DataProcessing = 4,

    /// <summary>
    /// Show quality analysis properties - Currently shows "Work in Progress"
    /// </summary>
    QualityAnalysis = 5,

    /// <summary>
    /// Show assessment completed properties - Currently shows "Work in Progress"
    /// </summary>
    AssessmentCompleted = 6
}

/// <summary>
/// Property process filter type for the Type dropdown
/// Used for filtering properties by their processing stage
/// </summary>
public enum PropertyProcessFilterType
{
    /// <summary>
    /// No process filter applied
    /// </summary>
    None = 0,

    /// <summary>
    /// Survey Completed - Currently shows "Work in Progress"
    /// </summary>
    SurveyCompleted = 1,

    /// <summary>
    /// Data Entry Completed - Currently shows "Work in Progress"
    /// </summary>
    DataEntryCompleted = 2,

    /// <summary>
    /// QC Completed - Currently shows "Work in Progress"
    /// </summary>
    QCCompleted = 3,

    /// <summary>
    /// Notice Distributed - Currently shows "Work in Progress"
    /// </summary>
    NoticeDistributed = 4
}
