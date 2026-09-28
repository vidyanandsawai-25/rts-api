namespace NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;

/// <summary>
/// Identifiers accepted by <c>GET api/ApartmentQC/Taxdetails</c>. An apartment complex shares one
/// Ward + PropertyNo across every unit (PropertyMast row); <see cref="WingMasterId"/> (the
/// WingMaster/WingEntity primary key, resolved via WingDetailsMast.WingMasterId) narrows the sum
/// to a single wing's units instead of the whole complex. <see cref="TaxType"/> selects which
/// current-tax table(s) to read; arrears (TransMast) are always returned regardless of it.
/// </summary>
public sealed class ApartmentTaxDetailsQueryParameters
{
    public int WardId { get; set; }
    public string PropertyNo { get; set; } = null!;
    public int? WingMasterId { get; set; }
    public ApartmentTaxType TaxType { get; set; }
}
