namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Intermediate projection DTO for joining property master data in GetApartmentDetailsWingWise service.
/// </summary>
public class JoinedPropertyRowDto
{
    public int Id { get; set; }
    public int TaxZoneId { get; set; }
    public int WardId { get; set; }
    public string PropertyNo { get; set; } = string.Empty;
    public string? PartitionNo { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNoEnglish { get; set; }
    public string? FlatOrShopNameEnglish { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierNameEnglish { get; set; }
    public string? PartType { get; set; }
    public int PropertyType { get; set; }
    public string? PropertyTypeName { get; set; }
    public int? WingDetailId { get; set; }
    public string? BHK { get; set; }
    public string? Wing { get; set; }
    public string? ApartmentType { get; set; }
}
