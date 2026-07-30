using System.Text.Json.Serialization;

namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Response DTO for Assessment stage grid data (zone-wise breakdown)
/// </summary>
public class AssessmentGridResponseDto
{
    public List<AssessmentZoneDataDto> ZoneData { get; set; } = new();
    public AssessmentZoneDataDto TotalRow { get; set; } = new();
    public AssessmentZoneDataDto GrandTotalRow { get; set; } = new();
}

/// <summary>
/// Zone-wise data for Assessment stage grid
/// </summary>
public class AssessmentZoneDataDto
{
    public int? ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;

    // Total Structure and Unit
    public int TotalStructure { get; set; }
    public int TotalUnit { get; set; }

    // Property Classification breakdown (Assessed, Unassessed, Rented)
    public List<PropertyClassificationDto> Classifications { get; set; } = new();
}

/// <summary>
/// Property Classification (TYPE column in grid: Assessed, Unassessed, Rented)
/// </summary>
public class PropertyClassificationDto
{
    public string Type { get; set; } = string.Empty; // "Assessed", "Unassessed", "Rented"
    public int Structure { get; set; }
    public int Unit { get; set; }
     // Demand columns
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? OldDemand { get; set; }
    public decimal CurrentDemand { get; set; }
    public decimal RetroDemand { get; set; }
    public decimal TotalDemand { get; set; }
    public decimal AdditionalRevenueGenerated { get; set; }
}
