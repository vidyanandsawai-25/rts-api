namespace NtisPlatform.Application.DTOs;

/// <summary>Per-ward row for the Ward-wise Zoning Abstract drawer table.</summary>
public class WardZoningAbstractDto
{
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public int TotalProperties { get; set; }
    public int CoveredProperties { get; set; }
    public int PendingProperties { get; set; }
    public double CoveragePercent { get; set; }
    public List<WardZoningAbstractZoneCountDto> ZoneCounts { get; set; } = new();
}

public class WardZoningAbstractZoneCountDto
{
    public int TaxZoneId { get; set; }
    public string TaxZoneNo { get; set; } = string.Empty;
    public int Count { get; set; }
}
