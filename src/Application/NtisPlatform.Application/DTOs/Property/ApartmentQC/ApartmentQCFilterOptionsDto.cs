namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Distinct filter-option values for the Apartment QC filter panel.
/// Each list contains every unique value present in the full (un-column-filtered) dataset
/// for the given scope (WardId / PropertyId / PartType / Type).
/// The UI uses these lists to populate dropdowns before the user applies a column filter.
/// </summary>
public sealed class ApartmentQCFilterOptionsDto
{
    public IReadOnlyList<string> Wings         { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ApartmentTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FlatOrShopNos { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int>    PropertyTypes  { get; init; } = Array.Empty<int>();
}
