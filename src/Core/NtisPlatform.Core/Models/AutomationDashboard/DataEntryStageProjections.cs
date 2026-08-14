namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Property row used for Data Entry dashboard stage aggregation.
/// </summary>
public class DataEntryStagePropertyProjection : AutomationDashboardStagePropertyZoneDto
{
    public string? PartitionNo { get; set; }
}

/// <summary>
/// Data Entry grid count row grouped by zone.
/// </summary>
public class DataEntryZoneCountProjection : AutomationDashboardZoneKeyDto
{
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}

/// <summary>
/// Property id that has a completed photo or plan.
/// </summary>
public class DataEntryCompletedPhotoProjection : AutomationDashboardPropertyKeyDto
{
    public int PhotoTypeId { get; set; }
}

/// <summary>
/// Property type source row used for Data Entry dashboard type grouping.
/// </summary>
public class DataEntryPropertyTypeSourceProjection : AutomationDashboardPropertyZoneDto
{
    public string? PropertyType { get; set; }
}

/// <summary>
/// Property use source row used for Data Entry dashboard type grouping.
/// </summary>
public class DataEntryPropertyUseSourceProjection : AutomationDashboardPropertyZoneDto
{
    public string? Type { get; set; }
    public string? TypeOfUseCode { get; set; }
}

/// <summary>
/// Assessment status count row grouped by zone and status.
/// </summary>
public class DataEntryAssessmentStatusCountProjection : AutomationDashboardZoneKeyDto
{
    public int StatusId { get; set; }
    public int PropertyCount { get; set; }
    public int UnitsOnlyCount { get; set; }
}

/// <summary>
/// One-shot raw data snapshot for the Data Entry grid.
/// </summary>
public  class DataEntryGridSnapshotProjection
{
    public bool WorkflowStageExists { get; set; }
    public int InternalSurveyStageId { get; set; }
    public int AssessmentStageId { get; set; }
    public int PropertyPhotoTypeId { get; set; }
    public int PlanPhotoTypeId { get; set; }
    public List<(int ZoneId, string ZoneName, string ZoneNo)> Zones { get; set; } = new();
    public List<DataEntryStagePropertyProjection> StageProperties { get; set; } = new();
    public List<DataEntryZoneCountProjection> ZoneTotals { get; set; } = new();
    public List<DataEntryCompletedPhotoProjection> CompletedPhotos { get; set; } = new();
    public List<DataEntryPropertyTypeSourceProjection> PropertyTypeSources { get; set; } = new();
    public List<DataEntryPropertyUseSourceProjection> PropertyUseSources { get; set; } = new();
    public Dictionary<string, int> AssessmentStatusIdsByName { get; set; } = new();
    public List<DataEntryAssessmentStatusCountProjection> AssessmentStatusCounts { get; set; } = new();
}

/// <summary>
/// Raw ward-wise data snapshot used by the application service to build Data Entry ward summaries.
/// </summary>
public  class DataEntryWardWiseSummaryProjection
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int DataEntryStageId { get; set; }
    public int InternalSurveyStageId { get; set; }
    public int AssessmentStageId { get; set; }
    public int PropertyPhotoTypeId { get; set; }
    public int PlanPhotoTypeId { get; set; }
    public List<(int WardId, string WardNo)> Wards { get; set; } = new();
    public List<DataEntryWardStageProjection> StageRows { get; set; } = new();
    public List<DataEntryWardCountProjection> WardTotalRows { get; set; } = new();
    public List<DataEntryCompletedPhotoProjection> CompletedPhotoRows { get; set; } = new();
    public List<DataEntryPropertyTypeSourceProjection> PropertyTypeRows { get; set; } = new();
    public List<DataEntryPropertyUseSourceProjection> PropertyUseRows { get; set; } = new();
    public Dictionary<string, int> AssessmentStatusIdsByName { get; set; } = new();
}

public class DataEntryWardStageProjection : AutomationDashboardStagePropertyWardDto
{
    public string? PartitionNo { get; set; }
    public int? PropertyTypeId { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? PropertyAssessmentStatusId { get; set; }
}

public class DataEntryWardCountProjection : AutomationDashboardWardKeyDto
{
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}
