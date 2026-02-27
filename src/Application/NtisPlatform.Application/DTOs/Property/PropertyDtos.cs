using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Property DTO for read operations - includes all fields from PropertyMast table
/// </summary>
public class PropertyDto : CommonBaseDtos
{
    public int OwnerID { get; set; }
    
    // Location Information
    public string? TaxZone { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    
    // Property Classification
    public int? PropertyTypeID { get; set; }
    public string? UPICID { get; set; }
    public bool? OpenPlot { get; set; }
    public string? CSN { get; set; }
    public string? SubZoneNo { get; set; }
    public string? PlotNo { get; set; }
    public int? CategoryID { get; set; }
    public string? Type { get; set; }
    public string? PartType { get; set; }
    
    // Owner Information
    public string? OwnerTitle { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerTitleEnglish { get; set; }
    public string? OwnerNameEnglish { get; set; }
    
    // Occupier Information
    public string? OccupierTitle { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierTitleEnglish { get; set; }
    public string? OccupierNameEnglish { get; set; }
    
    // Flat/Shop Information
    public string? FlatOrShopNo { get; set; }
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNoEnglish { get; set; }
    public string? FlatOrShopNameEnglish { get; set; }
    
    // Address Information
    public string? Address { get; set; }
    public string? Location { get; set; }
    public string? AddressEnglish { get; set; }
    public string? LocationEnglish { get; set; }
    
    // Contact Information
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    
    // Society Information
    public int? SocietyID { get; set; }
    
    // Status
    public bool MarkedForDeletion { get; set; }

    // Computed field for display
    public string DisplayProperty =>
     string.IsNullOrWhiteSpace(PropertyNo)
         ? string.IsNullOrWhiteSpace(PartitionNo)
             ? string.Empty
             : $"-{PartitionNo}"
         : string.IsNullOrWhiteSpace(PartitionNo)
             ? PropertyNo
             : $"{PropertyNo}-{PartitionNo}";
}

/// <summary>
/// DTO for creating new Property records
/// </summary>
public class CreatePropertyDto : CreateCommonBaseDtos
{
    // Required Fields
    [Required(ErrorMessage = "Property_WardNo_Required")]
    [StringLength(10, ErrorMessage = "Property_WardNo_MaxLen_10")]
    public string WardNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Property_PropertyNo_Required")]
    [StringLength(10, ErrorMessage = "Property_PropertyNo_MaxLen_10")]
    public string PropertyNo { get; set; } = string.Empty;

    // Location Information
    [StringLength(10, ErrorMessage = "Property_TaxZone_MaxLen_10")]
    public string? TaxZone { get; set; }

    [StringLength(10, ErrorMessage = "Property_PartitionNo_MaxLen_10")]
    public string? PartitionNo { get; set; }

    // Property Classification
    public int? PropertyTypeID { get; set; }

    [StringLength(30, ErrorMessage = "Property_UPICID_MaxLen_30")]
    public string? UPICID { get; set; }

    public bool? OpenPlot { get; set; }

    [StringLength(30, ErrorMessage = "Property_CSN_MaxLen_30")]
    public string? CSN { get; set; }

    [StringLength(20, ErrorMessage = "Property_SubZoneNo_MaxLen_20")]
    public string? SubZoneNo { get; set; }

    [StringLength(20, ErrorMessage = "Property_PlotNo_MaxLen_20")]
    public string? PlotNo { get; set; }

    public int? CategoryID { get; set; }

    [StringLength(5, ErrorMessage = "Property_Type_MaxLen_5")]
    public string? Type { get; set; }

    [StringLength(20, ErrorMessage = "Property_PartType_MaxLen_20")]
    public string? PartType { get; set; }

    // Owner Information
    [StringLength(10, ErrorMessage = "Property_OwnerTitle_MaxLen_10")]
    public string? OwnerTitle { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OwnerName_MaxLen_1000")]
    public string? OwnerName { get; set; }

    [StringLength(10, ErrorMessage = "Property_OwnerTitleEnglish_MaxLen_10")]
    public string? OwnerTitleEnglish { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OwnerNameEnglish_MaxLen_1000")]
    public string? OwnerNameEnglish { get; set; }

    // Occupier Information
    [StringLength(10, ErrorMessage = "Property_OccupierTitle_MaxLen_10")]
    public string? OccupierTitle { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OccupierName_MaxLen_1000")]
    public string? OccupierName { get; set; }

    [StringLength(10, ErrorMessage = "Property_OccupierTitleEnglish_MaxLen_10")]
    public string? OccupierTitleEnglish { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OccupierNameEnglish_MaxLen_1000")]
    public string? OccupierNameEnglish { get; set; }

    // Flat/Shop Information
    [StringLength(100, ErrorMessage = "Property_FlatOrShopNo_MaxLen_100")]
    public string? FlatOrShopNo { get; set; }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopName_MaxLen_200")]
    public string? FlatOrShopName { get; set; }

    [StringLength(100, ErrorMessage = "Property_FlatOrShopNoEnglish_MaxLen_100")]
    public string? FlatOrShopNoEnglish { get; set; }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopNameEnglish_MaxLen_200")]
    public string? FlatOrShopNameEnglish { get; set; }

    // Address Information
    [StringLength(500, ErrorMessage = "Property_Address_MaxLen_500")]
    public string? Address { get; set; }

    [StringLength(200, ErrorMessage = "Property_Location_MaxLen_200")]
    public string? Location { get; set; }

    [StringLength(500, ErrorMessage = "Property_AddressEnglish_MaxLen_500")]
    public string? AddressEnglish { get; set; }

    [StringLength(200, ErrorMessage = "Property_LocationEnglish_MaxLen_200")]
    public string? LocationEnglish { get; set; }

    // // Intentionally permissive to support legacy and regional phone formats.
    [StringLength(13, ErrorMessage = "Property_MobileNo_MaxLen_13")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Property_MobileNo_Invalid")]
    public string? MobileNo { get; set; }

    [StringLength(100, ErrorMessage = "Property_EmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "Property_EmailId_Invalid")]
    public string? EmailId { get; set; }

    // Society Information
    public int? SocietyID { get; set; }

    // Status
    public bool MarkedForDeletion { get; set; } = false;
}

/// <summary>
/// DTO for updating existing Property records
/// </summary>
public class UpdatePropertyDto : UpdateCommonBaseDtos
{
    // Required Fields
    [Required(ErrorMessage = "Property_WardNo_Required")]
    [StringLength(10, ErrorMessage = "Property_WardNo_MaxLen_10")]
    public string WardNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Property_PropertyNo_Required")]
    [StringLength(10, ErrorMessage = "Property_PropertyNo_MaxLen_10")]
    public string PropertyNo { get; set; } = string.Empty;

    // Location Information
    [StringLength(10, ErrorMessage = "Property_TaxZone_MaxLen_10")]
    public string? TaxZone { get; set; }

    [StringLength(10, ErrorMessage = "Property_PartitionNo_MaxLen_10")]
    public string? PartitionNo { get; set; }

    // Property Classification
    public int? PropertyTypeID { get; set; }

    [StringLength(30, ErrorMessage = "Property_UPICID_MaxLen_30")]
    public string? UPICID { get; set; }

    public bool? OpenPlot { get; set; }

    [StringLength(30, ErrorMessage = "Property_CSN_MaxLen_30")]
    public string? CSN { get; set; }

    [StringLength(20, ErrorMessage = "Property_SubZoneNo_MaxLen_20")]
    public string? SubZoneNo { get; set; }

    [StringLength(20, ErrorMessage = "Property_PlotNo_MaxLen_20")]
    public string? PlotNo { get; set; }

    public int? CategoryID { get; set; }

    [StringLength(5, ErrorMessage = "Property_Type_MaxLen_5")]
    public string? Type { get; set; }

    [StringLength(20, ErrorMessage = "Property_PartType_MaxLen_20")]
    public string? PartType { get; set; }

    // Owner Information
    [StringLength(10, ErrorMessage = "Property_OwnerTitle_MaxLen_10")]
    public string? OwnerTitle { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OwnerName_MaxLen_1000")]
    public string? OwnerName { get; set; }

    [StringLength(10, ErrorMessage = "Property_OwnerTitleEnglish_MaxLen_10")]
    public string? OwnerTitleEnglish { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OwnerNameEnglish_MaxLen_1000")]
    public string? OwnerNameEnglish { get; set; }

    // Occupier Information
    [StringLength(10, ErrorMessage = "Property_OccupierTitle_MaxLen_10")]
    public string? OccupierTitle { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OccupierName_MaxLen_1000")]
    public string? OccupierName { get; set; }

    [StringLength(10, ErrorMessage = "Property_OccupierTitleEnglish_MaxLen_10")]
    public string? OccupierTitleEnglish { get; set; }

    [StringLength(1000, ErrorMessage = "Property_OccupierNameEnglish_MaxLen_1000")]
    public string? OccupierNameEnglish { get; set; }

    // Flat/Shop Information
    [StringLength(100, ErrorMessage = "Property_FlatOrShopNo_MaxLen_100")]
    public string? FlatOrShopNo { get; set; }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopName_MaxLen_200")]
    public string? FlatOrShopName { get; set; }

    [StringLength(100, ErrorMessage = "Property_FlatOrShopNoEnglish_MaxLen_100")]
    public string? FlatOrShopNoEnglish { get; set; }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopNameEnglish_MaxLen_200")]
    public string? FlatOrShopNameEnglish { get; set; }

    // Address Information
    [StringLength(500, ErrorMessage = "Property_Address_MaxLen_500")]
    public string? Address { get; set; }

    [StringLength(200, ErrorMessage = "Property_Location_MaxLen_200")]
    public string? Location { get; set; }

    [StringLength(500, ErrorMessage = "Property_AddressEnglish_MaxLen_500")]
    public string? AddressEnglish { get; set; }

    [StringLength(200, ErrorMessage = "Property_LocationEnglish_MaxLen_200")]
    public string? LocationEnglish { get; set; }

    // // Intentionally permissive to support legacy and regional phone formats.
    [StringLength(13, ErrorMessage = "Property_MobileNo_MaxLen_13")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Property_MobileNo_Invalid")]
    public string? MobileNo { get; set; }

    [StringLength(100, ErrorMessage = "Property_EmailId_MaxLen_100")]
    [EmailAddress(ErrorMessage = "Property_EmailId_Invalid")]
    public string? EmailId { get; set; }

    // Society Information
    public int? SocietyID { get; set; }

    // Status
    public bool MarkedForDeletion { get; set; } = false;
}
