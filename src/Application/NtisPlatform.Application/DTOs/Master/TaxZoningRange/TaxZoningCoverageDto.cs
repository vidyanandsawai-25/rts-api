namespace NtisPlatform.Application.DTOs;

/// <summary>Dashboard KPI counters for the Tax Zoning coverage cards.</summary>
public class TaxZoningCoverageDto
{
    public int TotalProperties { get; set; }
    public int CoveredProperties { get; set; }
    public int PendingProperties { get; set; }
    public List<TaxZoningZoneWiseCountDto> ZoneWiseCounts { get; set; } = new();
}

public class TaxZoningZoneWiseCountDto
{
    public int TaxZoneId { get; set; }
    public string TaxZoneNo { get; set; } = string.Empty;
    public int Count { get; set; }
}
