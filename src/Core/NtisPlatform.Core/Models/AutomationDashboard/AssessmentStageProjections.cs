
namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Projection for workflow-stage properties used by Assessment dashboard queries.
/// </summary>
public class AssessmentStagePropertyProjection : AutomationDashboardPropertyZoneDisplayDto
{
    public string? PartitionNo { get; set; }
    public int? AssessmentStatusId { get; set; }
    public bool IsRented { get; set; }
}

/// <summary>
/// Projection for assessed properties with old values needed for classification.
/// </summary>
public class AssessedClassificationPropertyProjection : AutomationDashboardPropertyZoneDisplayDto
{
    public int? PropertyMastOldId { get; set; }
    public string? PartitionNo { get; set; }
    public double? OldConstructionArea { get; set; }
    public string? OldUseType { get; set; }
    public double? OldRV { get; set; }
}

/// <summary>
/// Projection for assessed properties after service-side classification.
/// </summary>
public class AssessedClassifiedPropertyProjection : AutomationDashboardPropertyZoneDisplayDto
{
    public int? PropertyMastOldId { get; set; }
    public string? PartitionNo { get; set; }
    public string ClassificationType { get; set; } = string.Empty;
}

/// <summary>
/// Projection for unassessed properties before service-side type classification.
/// </summary>
public class UnassessedPropertyProjection : AutomationDashboardPropertyZoneDisplayDto
{
    public int? PropertyTypeId { get; set; }
    public string? PartitionNo { get; set; }
    public bool IsOpenPlot { get; set; }
}

/// <summary>
/// Projection for unassessed properties after service-side property-type classification.
/// </summary>
public class UnassessedClassifiedPropertyProjection : AutomationDashboardPropertyZoneDisplayDto
{
    public string? PartitionNo { get; set; }
    public string PropertyType { get; set; } = string.Empty;
}

/// <summary>
/// Projection for rented-tab properties after owner/renter classification.
/// </summary>
public class RentedClassifiedPropertyProjection : AutomationDashboardPropertyZoneDisplayDto
{
    public string? PartitionNo { get; set; }
    public string ClassificationType { get; set; } = string.Empty;
    public decimal OldDemand { get; set; }
    public decimal CurrentDemand { get; set; }
    public decimal RetroDemand { get; set; }
}

/// <summary>
/// Raw Rented tab row with renter flag and demand values.
/// </summary>
public class RentedPropertyDemandProjection : AutomationDashboardPropertyZoneDisplayDto
{
    public string? PartitionNo { get; set; }
    public bool HasRenterTaxLiability { get; set; }
    public decimal OldDemand { get; set; }
    public decimal CurrentDemand { get; set; }
    public decimal RetroDemand { get; set; }
}

/// <summary>
/// Projection for current property details and use-type metadata.
/// </summary>
public class AssessmentPropertyUseDetailProjection : AutomationDashboardPropertyKeyDto
{
    public double CarpetArea { get; set; }
    public bool IsOpenPlot { get; set; }
    public string? Type { get; set; }
    public string? TypeOfUseCode { get; set; }
    public string? TypeOfUseDescription { get; set; }
}

/// <summary>
/// Projection for zone-wise structure and unit counts.
/// </summary>
public class AssessmentZoneCountProjection : AutomationDashboardZoneDisplayDto
{
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}
