namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property KYC Details Tab - includes joined data from multiple tables
/// Used for the GET /{propertyId}/kyc-details API endpoint
/// </summary>
public class PropertyKycDetailsDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    
    // From PropertyMastDetails
    public int? OwnerTypeId { get; set; }
    public string? AdharCardNo { get; set; }
    
    // From OwnerTypeMaster
    public string? OwnerType { get; set; }
    
    // From PropertyMast - Owner Information
    public string? OwnerTitle { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerTitleEnglish { get; set; }
    public string? OwnerNameEnglish { get; set; }
    
    // From PropertyMast - Occupier Information
    public string? OccupierTitle { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierTitleEnglish { get; set; }
    public string? OccupierNameEnglish { get; set; }
    
    // From PropertyMast - Address Information
    public string? Address { get; set; }
    public string? Location { get; set; }
    public string? AddressEnglish { get; set; }
    public string? LocationEnglish { get; set; }
    
    // From PropertyMast - Flat/Shop Information
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNameEnglish { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? FlatOrShopNoEnglish { get; set; }
    
    // From PropertyMast - Contact Information
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
}
