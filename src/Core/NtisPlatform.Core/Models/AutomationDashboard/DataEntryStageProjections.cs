namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Property row used for Data Entry dashboard stage aggregation.
/// </summary>
public sealed class DataEntryStagePropertyProjection
{
    public int PropertyId { get; set; }
    public int WorkflowStageId { get; set; }
    public int ZoneId { get; set; }
    public string? PartitionNo { get; set; }
}

/// <summary>
/// Data Entry grid count row grouped by zone.
/// </summary>
public sealed class DataEntryZoneCountProjection
{
    public int ZoneId { get; set; }
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}

/// <summary>
/// Property id that has a completed photo or plan.
/// </summary>
public sealed class DataEntryCompletedPhotoProjection
{
    public int PropertyId { get; set; }
    public int PhotoTypeId { get; set; }
}

/// <summary>
/// Property type source row used for Data Entry dashboard type grouping.
/// </summary>
public sealed class DataEntryPropertyTypeSourceProjection
{
    public int PropertyId { get; set; }
    public int ZoneId { get; set; }
    public string? PropertyType { get; set; }
}

/// <summary>
/// Property use source row used for Data Entry dashboard type grouping.
/// </summary>
public sealed class DataEntryPropertyUseSourceProjection
{
    public int PropertyId { get; set; }
    public int ZoneId { get; set; }
    public string? Type { get; set; }
    public string? TypeOfUseCode { get; set; }
}

/// <summary>
/// Assessment status count row grouped by zone and status.
/// </summary>
public sealed class DataEntryAssessmentStatusCountProjection
{
    public int ZoneId { get; set; }
    public int StatusId { get; set; }
    public int PropertyCount { get; set; }
    public int UnitsOnlyCount { get; set; }
}

/// <summary>
/// One-shot raw data snapshot for the Data Entry grid.
/// </summary>
public sealed class DataEntryGridSnapshotProjection
{
    public bool WorkflowStageExists { get; set; }
    public int InternalSurveyStageId { get; set; }
    public int AssessmentStageId { get; set; }
    public int PropertyPhotoTypeId { get; set; }
    public int PlanPhotoTypeId { get; set; }
    public List<(int ZoneId, string ZoneName)> Zones { get; set; } = new();
    public List<DataEntryStagePropertyProjection> StageProperties { get; set; } = new();
    public List<DataEntryZoneCountProjection> ZoneTotals { get; set; } = new();
    public List<DataEntryCompletedPhotoProjection> CompletedPhotos { get; set; } = new();
    public List<DataEntryPropertyTypeSourceProjection> PropertyTypeSources { get; set; } = new();
    public List<DataEntryPropertyUseSourceProjection> PropertyUseSources { get; set; } = new();
    public Dictionary<string, int> AssessmentStatusIdsByName { get; set; } = new();
    public List<DataEntryAssessmentStatusCountProjection> AssessmentStatusCounts { get; set; } = new();
}
