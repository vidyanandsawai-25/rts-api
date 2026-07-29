namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Response DTO for Data Entry & Quality Analyst stage grid data (division-wise breakdown)
/// </summary>
public class DataEntryGridResponseDto
{
    public List<DataEntryDivisionDataDto> DivisionData { get; set; } = new();
    public DataEntryDivisionDataDto TotalRow { get; set; } = new();
}

public class DataEntryWardWiseSummaryResponseDto
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<DataEntryWardDataDto> WardData { get; set; } = new();
    public DataEntryWardDataDto TotalRow { get; set; } = new();
}

/// <summary>
/// Division-wise data for Data Entry & Quality Analyst stage grid
/// </summary>
public class DataEntryDivisionDataDto
{
    public int? DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;

    // Total Structure and Unit
    public int Structure { get; set; }
    public int Unit { get; set; }

    // Internal Survey breakdown
    public InternalSurveyBreakdownDto InternalSurvey { get; set; } = new();

    // Data Entry breakdown
    public DataEntryBreakdownDto DataEntry { get; set; } = new();

    // Photo breakdown
    public PhotoBreakdownDto Photo { get; set; } = new();

    // Plan breakdown
    public PlanBreakdownDto Plan { get; set; } = new();

    // Quality Analyst breakdown
    public QualityAnalystBreakdownDto QualityAnalyst { get; set; } = new();

    // Property Type breakdown
    public DataEntryPropertyTypeBreakdownDto PropertyType { get; set; } = new();

    // Assessment Status breakdown
    public AssessmentStatusBreakdownDto AssessmentStatusBreakdown { get; set; } = new();
}

public class DataEntryWardDataDto
{
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public int Structure { get; set; }
    public int Unit { get; set; }
    public InternalSurveyBreakdownDto InternalSurvey { get; set; } = new();
    public DataEntryBreakdownDto DataEntry { get; set; } = new();
    public PhotoBreakdownDto Photo { get; set; } = new();
    public PlanBreakdownDto Plan { get; set; } = new();
    public QualityAnalystBreakdownDto QualityAnalyst { get; set; } = new();
    public DataEntryPropertyTypeBreakdownDto PropertyType { get; set; } = new();
    public AssessmentStatusBreakdownDto AssessmentStatusBreakdown { get; set; } = new();
}

/// <summary>
/// Internal Survey stage breakdown (Structure and Unit counts only)
/// </summary>
public class InternalSurveyBreakdownDto
{
    public int Structure { get; set; }
    public int Unit { get; set; }
}

/// <summary>
/// Data Entry stage breakdown (Completed/Pending by Structure and Unit)
/// </summary>
public class DataEntryBreakdownDto
{
    public int CompletedStructure { get; set; }
    public int CompletedUnit { get; set; }
    public int PendingStructure { get; set; }
    public int PendingUnit { get; set; }
}

/// <summary>
/// Photo breakdown (Complete/Pending counts)
/// </summary>
public class PhotoBreakdownDto
{
    public int Complete { get; set; }
    public int Pending { get; set; }
}

/// <summary>
/// Plan breakdown (Complete/Pending counts)
/// </summary>
public class PlanBreakdownDto
{
    public int Complete { get; set; }
    public int Pending { get; set; }
}

/// <summary>
/// Quality Analyst breakdown (Completed/Pending by Structure and Unit)
/// </summary>
public class QualityAnalystBreakdownDto
{
    public int CompletedStructure { get; set; }
    public int CompletedUnit { get; set; }
    public int PendingStructure { get; set; }
    public int PendingUnit { get; set; }
}

public class DataEntryPropertyTypeBreakdownDto
{
    public int Residential { get; set; }
    public int NonResidential { get; set; }
    public int Mixed { get; set; }
    public int PublicUtility { get; set; }
}
