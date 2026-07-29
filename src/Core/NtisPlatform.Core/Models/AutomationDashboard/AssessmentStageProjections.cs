namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Projection for workflow-stage properties used by Assessment dashboard queries.
/// </summary>
public sealed class AssessmentStagePropertyProjection
{
    public int PropertyId { get; set; }
    public string? PartitionNo { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int? AssessmentStatusId { get; set; }
    public bool IsRented { get; set; }
}

/// <summary>
/// Projection for assessed properties with old values needed for classification.
/// </summary>
public sealed class AssessedClassificationPropertyProjection
{
    public int PropertyId { get; set; }
    public int? PropertyMastOldId { get; set; }
    public string? PartitionNo { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public double? OldConstructionArea { get; set; }
    public string? OldUseType { get; set; }
    public double? OldRV { get; set; }
}

/// <summary>
/// Projection for assessed properties after service-side classification.
/// </summary>
public sealed class AssessedClassifiedPropertyProjection
{
    public int PropertyId { get; set; }
    public int? PropertyMastOldId { get; set; }
    public string? PartitionNo { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ClassificationType { get; set; } = string.Empty;
}

/// <summary>
/// Projection for unassessed properties before service-side type classification.
/// </summary>
public sealed class UnassessedPropertyProjection
{
    public int PropertyId { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? PartitionNo { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public bool IsOpenPlot { get; set; }
}

/// <summary>
/// Projection for unassessed properties after service-side property-type classification.
/// </summary>
public sealed class UnassessedClassifiedPropertyProjection
{
    public int PropertyId { get; set; }
    public string? PartitionNo { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
}

/// <summary>
/// Projection for rented-tab properties after owner/renter classification.
/// </summary>
public sealed class RentedClassifiedPropertyProjection
{
    public int PropertyId { get; set; }
    public string? PartitionNo { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ClassificationType { get; set; } = string.Empty;
    public decimal OldDemand { get; set; }
    public decimal CurrentDemand { get; set; }
    public decimal RetroDemand { get; set; }
}

/// <summary>
/// Raw Rented tab row with renter flag and demand values.
/// </summary>
public sealed class RentedPropertyDemandProjection
{
    public int PropertyId { get; set; }
    public string? PartitionNo { get; set; }
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public bool HasRenterTaxLiability { get; set; }
    public decimal OldDemand { get; set; }
    public decimal CurrentDemand { get; set; }
    public decimal RetroDemand { get; set; }
}

/// <summary>
/// Projection for current property details and use-type metadata.
/// </summary>
public sealed class AssessmentPropertyUseDetailProjection
{
    public int PropertyId { get; set; }
    public double CarpetArea { get; set; }
    public bool IsOpenPlot { get; set; }
    public string? Type { get; set; }
    public string? TypeOfUseCode { get; set; }
    public string? TypeOfUseDescription { get; set; }
}

/// <summary>
/// Projection for zone-wise structure and unit counts.
/// </summary>
public sealed class AssessmentZoneCountProjection
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}
