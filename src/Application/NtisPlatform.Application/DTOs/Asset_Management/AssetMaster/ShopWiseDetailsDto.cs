namespace NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

/// <summary>
/// DTO for shop-wise details including asset and renter information.
/// </summary>
public class ShopWiseDetailsDto
{
    public int SerialNo { get; set; }
    public string? AssetId { get; set; }
    public string? Floor { get; set; }
    public string? ShopNo { get; set; }
    public string? ShopName { get; set; }
    public decimal? Area { get; set; }
    public string? Occupier { get; set; }
    public string? Contact { get; set; }
    public decimal? AnnualRent { get; set; }
    public string? AgreementPeriod { get; set; }
    public string? Status { get; set; }
    public string? Condition { get; set; }
}
