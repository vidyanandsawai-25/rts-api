namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Full response for <c>GET api/ApartmentQC/top-section</c> — the property overview strip,
/// the property-info panel, and the additional revenue details shown at the top of the QC screen.
/// </summary>
public sealed class PropertyTopSectionDto
{
    public PropertyOverviewDto PropertyOverview { get; init; } = new();
    public PropertyInfoDto PropertyInfo { get; init; } = new();
    public AdditionalRevenueDto AdditionalRevenue { get; init; } = new();
}
