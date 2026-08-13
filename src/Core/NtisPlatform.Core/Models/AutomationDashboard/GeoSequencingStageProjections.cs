namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Property row used for Geo-Sequencing dashboard aggregation.
/// </summary>
public  class GeoSequencingStagePropertyProjection
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public int ZoneId { get; set; }
    public string? PartitionNo { get; set; }
    public string? PropertyTypeCode { get; set; }
    public string? PropertyCategoryName { get; set; }
    public int? AssessmentStatusId { get; set; }
}

/// <summary>
/// Property use row used for Geo-Sequencing property-type grouping.
/// </summary>
public  class GeoSequencingPropertyUseProjection
{
    public int PropertyId { get; set; }
    public string? Type { get; set; }
    public string? TypeOfUseCode { get; set; }
}
