namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Returned by the old-property lookup endpoint.
/// When the UI user changes OldPropertyNo, this payload auto-fills the remaining
/// old-property fields without requiring a full property reload.
/// </summary>
public sealed class OldPropertyLookupDto
{
    public string?  OldPropertyNo       { get; set; }
    public decimal? OldConstructionArea { get; set; }
    public decimal? OldRV               { get; set; }
    public decimal? OldTotalTax         { get; set; }
    public string?  OldUseType          { get; set; }
    public string?  OldConstructionYear { get; set; }
    public string?  OldConstructionType { get; set; }
    public string?  OldCSN { get; set; }
}
