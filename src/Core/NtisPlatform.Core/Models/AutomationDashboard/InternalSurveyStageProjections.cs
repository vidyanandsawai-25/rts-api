namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Property row used for Internal Survey dashboard aggregation.
/// </summary>
public  class InternalSurveyStagePropertyProjection
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public int ZoneId { get; set; }
    public string? PartitionNo { get; set; }
    public string? PropertyTypeCode { get; set; }
    public int? AssessmentStatusId { get; set; }
}

/// <summary>
/// Property use row used for Internal Survey property-type grouping.
/// </summary>
public  class InternalSurveyPropertyUseSourceProjection
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public int ZoneId { get; set; }
    public string? Type { get; set; }
    public string? TypeOfUseCode { get; set; }
}

/// <summary>
/// Photo count grouped by zone or ward for Internal Survey.
/// </summary>
public  class InternalSurveyPhotoCountProjection
{
    public int? ZoneId { get; set; }
    public int? WardId { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Service snapshot assembled from raw Internal Survey grid reads.
/// </summary>
public  class InternalSurveyGridSnapshotProjection
{
    public bool WorkflowStageExists { get; set; }
    public int GeoSequencingStageId { get; set; }
    public int AssessedStatusId { get; set; }
    public int UnassessedStatusId { get; set; }
    public int PropertyPhotoTypeId { get; set; }
    public List<(int ZoneId, string ZoneName, string ZoneNo)> Zones { get; set; } = new();
    public List<InternalSurveyStagePropertyProjection> GeoSequencingProperties { get; set; } = new();
    public List<InternalSurveyStagePropertyProjection> InternalSurveyProperties { get; set; } = new();
    public List<InternalSurveyPropertyUseSourceProjection> InternalSurveyPropertyUses { get; set; } = new();
    public List<InternalSurveyPhotoCountProjection> PhotoCountsByZone { get; set; } = new();
}

/// <summary>
/// Service snapshot assembled from raw Internal Survey ward-wise reads.
/// </summary>
public  class InternalSurveyWardWiseSnapshotProjection
{
    public bool IsValid { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;
    public int GeoSequencingStageId { get; set; }
    public int AssessedStatusId { get; set; }
    public int UnassessedStatusId { get; set; }
    public int PropertyPhotoTypeId { get; set; }
    public List<(int WardId, string WardNo)> Wards { get; set; } = new();
    public List<InternalSurveyStagePropertyProjection> GeoSequencingProperties { get; set; } = new();
    public List<InternalSurveyStagePropertyProjection> InternalSurveyProperties { get; set; } = new();
    public List<InternalSurveyPropertyUseSourceProjection> InternalSurveyPropertyUses { get; set; } = new();
    public List<InternalSurveyPhotoCountProjection> PhotoCountsByWard { get; set; } = new();
}
