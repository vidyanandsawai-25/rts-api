using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for updating Property KYC Details Tab
/// Used for the PUT /{propertyId}/kyc-details API endpoint
/// </summary>
public class UpdatePropertyKycDetailsDto
{
    // PropertyMastDetails fields
    [Range(1, int.MaxValue, ErrorMessage = "OwnerTypeId must be greater than 0.")]
    public int? OwnerTypeId { get; set; }

    [StringLength(12, ErrorMessage = "AdharCardNo must be exactly 12 digits.")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "AdharCardNo must be exactly 12 digits.")]
    public string? AdharCardNo { get; set; }

    // PropertyMast - Owner Information
    [StringLength(20, ErrorMessage = "OwnerTitle cannot exceed 20 characters.")]
    public string? OwnerTitle { get; set; }

    [StringLength(1000, ErrorMessage = "OwnerName cannot exceed 1000 characters.")]
    public string? OwnerName { get; set; }

    [StringLength(20, ErrorMessage = "OwnerTitleEnglish cannot exceed 20 characters.")]
    public string? OwnerTitleEnglish { get; set; }

    [StringLength(1000, ErrorMessage = "OwnerNameEnglish cannot exceed 1000 characters.")]
    public string? OwnerNameEnglish { get; set; }

    // PropertyMast - Occupier Information
    [StringLength(20, ErrorMessage = "OccupierTitle cannot exceed 20 characters.")]
    public string? OccupierTitle { get; set; }

    [StringLength(1000, ErrorMessage = "OccupierName cannot exceed 1000 characters.")]
    public string? OccupierName { get; set; }

    [StringLength(20, ErrorMessage = "OccupierTitleEnglish cannot exceed 20 characters.")]
    public string? OccupierTitleEnglish { get; set; }

    [StringLength(1000, ErrorMessage = "OccupierNameEnglish cannot exceed 1000 characters.")]
    public string? OccupierNameEnglish { get; set; }

    // PropertyMast - Address Information
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string? Address { get; set; }

    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string? Location { get; set; }

    [StringLength(500, ErrorMessage = "AddressEnglish cannot exceed 500 characters.")]
    public string? AddressEnglish { get; set; }

    [StringLength(200, ErrorMessage = "LocationEnglish cannot exceed 200 characters.")]
    public string? LocationEnglish { get; set; }

    // PropertyMast - Flat/Shop Information
    [StringLength(200, ErrorMessage = "FlatOrShopName cannot exceed 200 characters.")]
    public string? FlatOrShopName { get; set; }

    [StringLength(200, ErrorMessage = "FlatOrShopNameEnglish cannot exceed 200 characters.")]
    public string? FlatOrShopNameEnglish { get; set; }

    [StringLength(100, ErrorMessage = "FlatOrShopNo cannot exceed 100 characters.")]
    public string? FlatOrShopNo { get; set; }

    [StringLength(100, ErrorMessage = "FlatOrShopNoEnglish cannot exceed 100 characters.")]
    public string? FlatOrShopNoEnglish { get; set; }

    // PropertyMast - Contact Information
    [StringLength(13, ErrorMessage = "MobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "MobileNo contains invalid characters.")]
    public string? MobileNo { get; set; }

    [StringLength(100, ErrorMessage = "EmailId cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "EmailId is not a valid email address.")]
    public string? EmailId { get; set; }
}
