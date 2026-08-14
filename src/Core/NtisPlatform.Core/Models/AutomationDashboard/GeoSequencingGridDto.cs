namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Response DTO for Geo-Sequencing grid data showing zone-wise property breakdown
/// </summary>
public class GeoSequencingGridResponseDto
{
    public List<GeoSequencingZoneDataDto> Zones { get; set; } = new();
    public GeoSequencingZoneDataDto TotalRow { get; set; } = new();
}

public class GeoSequencingWardWiseSummaryResponseDto
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<GeoSequencingWardDataDto> WardData { get; set; } = new();
    public GeoSequencingWardDataDto TotalRow { get; set; } = new();
}

/// <summary>
/// Zone-wise data for Geo-Sequencing grid
/// </summary>
public class GeoSequencingZoneDataDto : AutomationDashboardZoneDisplayDto
{
    /// <summary>
    /// Total registered properties in this zone
    /// </summary>
    public int RegisteredProperties { get; set; }

    /// <summary>
    /// Geo-sequenced properties breakdown
    /// </summary>
    public GeoSequencedPropertiesDto GeoSequencedProperties { get; set; } = new();

    /// <summary>
    /// Property type breakdown
    /// </summary>
    public PropertyTypesBreakdownDto PropertyTypeBreakdown { get; set; } = new();

    /// <summary>
    /// Assessment status breakdown
    /// </summary>
    public AssessmentStatusBreakdownDto AssessmentStatusBreakdown { get; set; } = new();
}

public class GeoSequencingWardDataDto : AutomationDashboardWardDisplayDto
{
    public int RegisteredProperties { get; set; }
    public GeoSequencedPropertiesDto GeoSequencedProperties { get; set; } = new();
    public PropertyTypesBreakdownDto PropertyTypeBreakdown { get; set; } = new();
    public AssessmentStatusBreakdownDto AssessmentStatusBreakdown { get; set; } = new();
}

/// <summary>
/// Geo-sequenced properties structure/unit counts
/// </summary>
public class GeoSequencedPropertiesDto
{
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}

/// <summary>
/// Property type category wise breakdown
/// </summary>
public class PropertyTypesBreakdownDto
{
    /// <summary>
    /// Residential (निवासी) properties count
    /// </summary>
    public int Residential { get; set; }

    /// <summary>
    /// Non-Residential (अनिवासी) properties count
    /// </summary>
    public int NonResidential { get; set; }

    /// <summary>
    /// Mixed (मिश्रित) properties count
    /// </summary>
    public int Mixed { get; set; }

    /// <summary>
    /// Public Utility properties count
    /// </summary>
    public int PublicUtility { get; set; }

    /// <summary>
    /// Under Construction properties count
    /// </summary>
    public int UnderConstruction { get; set; }
}

/// <summary>
/// Assessment status wise breakdown
/// </summary>
public class AssessmentStatusBreakdownDto
{
    /// <summary>
    /// Assessed properties breakdown
    /// </summary>
    public StructureUnitCountDto Assessed { get; set; } = new();

    /// <summary>
    /// Unassessed properties breakdown
    /// </summary>
    public StructureUnitCountDto Unassessed { get; set; } = new();

    /// <summary>
    /// Newly assessed properties found
    /// </summary>
    public StructureUnitCountDto NewlyAssessedFound { get; set; } = new();

    /// <summary>
    /// Assessment in-process properties
    /// </summary>
    public StructureUnitCountDto AssessmentInProcess { get; set; } = new();
}

/// <summary>
/// Structure and Unit count breakdown
/// </summary>
public class StructureUnitCountDto
{
    public int StatusId { get; set; }
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}
