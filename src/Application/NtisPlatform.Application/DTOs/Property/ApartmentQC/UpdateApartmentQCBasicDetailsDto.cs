using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Payload for PATCH <c>apartmentqc/{propertyId}/basic</c>.
/// All fields are optional — only non-null values are written to the database.
/// At least one field must be supplied (validated by the service).
/// </summary>
public class UpdateApartmentQCBasicDetailsDto
{
    [StringLength(1000, ErrorMessage = "ApartmentQC_OwnerName_MaxLen_1000")]
    public string? OwnerName { get; set; }

    [StringLength(1000, ErrorMessage = "ApartmentQC_OccupierName_MaxLen_1000")]
    public string? OccupierName { get; set; }

    [StringLength(500, ErrorMessage = "ApartmentQC_RenterName_MaxLen_500")]
    public string? RenterName { get; set; }

    /// <summary>Id from PropertyTypeMaster.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ApartmentQC_PropertyType_Invalid")]
    public int? PropertyType { get; set; }

    [StringLength(20, ErrorMessage = "ApartmentQC_BHK_MaxLen_20")]
    public string? BHK { get; set; }

    [StringLength(13, ErrorMessage = "ApartmentQC_MobileNo_MaxLen_13")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "ApartmentQC_MobileNo_Invalid")]
    public string? MobileNo { get; set; }

    [StringLength(100, ErrorMessage = "ApartmentQC_EmailId_MaxLen_100")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "ApartmentQC_EmailId_Invalid")]
    public string? EmailId { get; set; }

    [StringLength(50, ErrorMessage = "ApartmentQC_Wing_MaxLen_50")]
    public string? Wing { get; set; }

    [StringLength(100, ErrorMessage = "ApartmentQC_FlatOrShopNo_MaxLen_100")]
    public string? FlatOrShopNo { get; set; }

    [StringLength(200, ErrorMessage = "ApartmentQC_FlatOrShopName_MaxLen_200")]
    public string? FlatOrShopName { get; set; }

    [StringLength(50, ErrorMessage = "ApartmentQC_OldPropertyNo_MaxLen_50")]
    public string? OldPropertyNo { get; set; }
}
