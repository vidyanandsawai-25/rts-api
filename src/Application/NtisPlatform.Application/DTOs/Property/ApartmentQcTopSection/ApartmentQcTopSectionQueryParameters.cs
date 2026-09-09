namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Identifiers accepted by <c>GET api/ApartmentQcTopSection</c> to resolve a single property.
/// Checked in this order — the first one satisfied wins: <see cref="PropertyId"/>,
/// <see cref="Upic"/>, <see cref="WardId"/> + <see cref="PropertyNo"/> (optionally narrowed by
/// <see cref="PartitionNo"/>), <see cref="WingDetailsId"/>, then <see cref="SocietyId"/>.
/// </summary>
public sealed class ApartmentQcTopSectionQueryParameters
{
    public int? PropertyId { get; set; }
    public string? Upic { get; set; }
    public int? WardId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int? WingDetailsId { get; set; }
    public int? SocietyId { get; set; }

    public bool HasAnyIdentifier() =>
        PropertyId.HasValue
        || !string.IsNullOrWhiteSpace(Upic)
        || (WardId.HasValue && !string.IsNullOrWhiteSpace(PropertyNo))
        || WingDetailsId.HasValue
        || SocietyId.HasValue;
}
