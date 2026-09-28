namespace NtisPlatform.Application.DTOs.PropertyDashboard;

/// <summary>
/// Projection DTO used for in-memory dashboard calculations.
/// </summary>
public class DashboardPropertyItemDto
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public string? PartitionNo { get; set; }
    public int? PropertySeqNo { get; set; }
    public int? PropertyTypeId { get; set; }
    public int? WingDetailId { get; set; }
    public bool? OpenPlot { get; set; }
    public string? Type { get; set; }
    public string? PartType { get; set; }
}
