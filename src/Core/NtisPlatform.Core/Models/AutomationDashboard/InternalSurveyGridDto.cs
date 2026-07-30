namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Response DTO for Internal Survey stage grid data (division-wise breakdown)
/// </summary>
public class InternalSurveyGridResponseDto
{
    public List<InternalSurveyDivisionDataDto> DivisionData { get; set; } = new();
    public InternalSurveyDivisionDataDto TotalRow { get; set; } = new();
}

public class InternalSurveyWardWiseSummaryResponseDto
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<InternalSurveyWardDataDto> WardData { get; set; } = new();
    public InternalSurveyWardDataDto TotalRow { get; set; } = new();
}

/// <summary>
/// Division-wise data for Internal Survey stage grid
/// </summary>
public class InternalSurveyDivisionDataDto
{
    public int? DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;

    // Geo-Sequencing Properties
    public GeoSequencingPropertiesDto GeoSequencingProperties { get; set; } = new();

    // Survey Properties
    public SurveyPropertiesDto SurveyProperties { get; set; } = new();

    // Property Type breakdown
    public PropertyTypesBreakdownDto PropertyType { get; set; } = new();

    // Assessed Properties
    public AssessedPropertiesSimpleDto AssessedProperties { get; set; } = new();

    // Unassessed Properties
    public UnassessedPropertiesDto UnassessedProperties { get; set; } = new();

    // Newly Assessed Found
    public NewlyAssessedFoundDto NewlyAssessedFound { get; set; } = new();

    // Assessment Inprocess
    public AssessmentInprocessDto AssessmentInprocess { get; set; } = new();

    // Photo Count
    public int PhotoCount { get; set; }
}

public class InternalSurveyWardDataDto
{
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public GeoSequencingPropertiesDto GeoSequencingProperties { get; set; } = new();
    public SurveyPropertiesDto SurveyProperties { get; set; } = new();
    public PropertyTypesBreakdownDto PropertyType { get; set; } = new();
    public AssessedPropertiesSimpleDto AssessedProperties { get; set; } = new();
    public UnassessedPropertiesDto UnassessedProperties { get; set; } = new();
    public NewlyAssessedFoundDto NewlyAssessedFound { get; set; } = new();
    public AssessmentInprocessDto AssessmentInprocess { get; set; } = new();
    public int PhotoCount { get; set; }
}

/// <summary>
/// Geo-Sequencing Properties breakdown
/// </summary>
public class GeoSequencingPropertiesDto
{
    public int Structure { get; set; }
    public int Unit { get; set; }
}

/// <summary>
/// Survey Properties breakdown
/// </summary>
public class SurveyPropertiesDto
{
    public int Structure { get; set; }
    public int Unit { get; set; }
}

/// <summary>
/// Assessed Properties (simple structure/unit count)
/// </summary>
public class AssessedPropertiesSimpleDto
{
    public int Structure { get; set; }
    public int Units { get; set; }
}

/// <summary>
/// Unassessed Properties breakdown
/// </summary>
public class UnassessedPropertiesDto
{
    public int Structure { get; set; }
    public int Units { get; set; }
}

/// <summary>
/// Newly Assessed Found breakdown
/// </summary>
public class NewlyAssessedFoundDto
{
    public int Structure { get; set; }
    public int Unit { get; set; }
}

/// <summary>
/// Assessment Inprocess breakdown
/// </summary>
public class AssessmentInprocessDto
{
    public int Structure { get; set; }
    public int Unit { get; set; }
}
