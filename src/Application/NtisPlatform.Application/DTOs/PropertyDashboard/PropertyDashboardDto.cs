namespace NtisPlatform.Application.DTOs.PropertyDashboard;

public class PropertyDashboardDto
{
    public int TotalOldProperties { get; set; }
    public int GeoSequencingProperties { get; set; }
    public PropertyAssessmentDto PropertyAssessment { get; set; } = new();
    public ApartmentDashboardDto Apartment { get; set; } = new();
    public CategoryDashboardDto Individual { get; set; } = new();
    public CategoryDashboardDto Industrial { get; set; } = new();
    public CategoryDashboardDto Plot { get; set; } = new();
}

public class PropertyAssessmentDto
{
    public int AssessedProperties { get; set; }
    public int UnassessedProperties { get; set; }
}

public class ApartmentDashboardDto
{
    public int TotalProperty { get; set; }
    public int TotalBuilding { get; set; }
    public int TotalUnits { get; set; }
    public int TotalAmenities { get; set; }
    public AssessedStatusDto AssessedStatus { get; set; } = new();
}

public class CategoryDashboardDto
{
    public int TotalProperty { get; set; }
    public int MainProperty { get; set; }
    public int PartitionProperty { get; set; }
    public AssessedStatusDto AssessedStatus { get; set; } = new();
}

public class AssessedStatusDto
{
    public int Assessed { get; set; }
    public int Unassessed { get; set; }
}
