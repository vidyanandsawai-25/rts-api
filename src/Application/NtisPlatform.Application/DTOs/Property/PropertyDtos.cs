using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Property DTO for read operations - includes all fields from PropertyMast table
/// </summary>
public class PropertyDto : BaseDtos
{
    public int Id { get; set; }
    
    // Location Information (Foreign Keys)
    public int TaxZoneId { get; set; }
    public int WardId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    
    // Property Classification
    public int? PropertyTypeId { get; set; }
    public string? UPICId { get; set; }
    public bool? OpenPlot { get; set; }
    public string? CSN { get; set; }
    public string? SubZoneNo { get; set; }
    public string? PlotNo { get; set; }
    public int? CategoryId { get; set; }
    public string? Type { get; set; }
    
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
    public int? SocietyDetailId { get; set; }
 
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
public class CreatePropertyDto : CreateBaseDtos
{
    private string? _propertyNo;
    private string? _partitionNo;
    private string? _upicId;
    private string? _csn;
    private string? _subZoneNo;
    private string? _plotNo;
    private string? _type; 
    private string? _ownerTitle;
    private string? _ownerName;
    private string? _ownerTitleEnglish;
    private string? _ownerNameEnglish;
    private string? _occupierTitle;
    private string? _occupierName;
    private string? _occupierTitleEnglish;
    private string? _occupierNameEnglish;
    private string? _flatOrShopNo;
    private string? _flatOrShopName;
    private string? _flatOrShopNoEnglish;
    private string? _flatOrShopNameEnglish;
    private string? _address;
    private string? _location;
    private string? _addressEnglish;
    private string? _locationEnglish;
    private string? _mobileNo;
    private string? _emailId;

    // Required Foreign Keys
    [Required(ErrorMessage = "Property_TaxZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Property_TaxZoneId_Invalid")]
    public int TaxZoneId { get; set; }

    [Required(ErrorMessage = "Property_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Property_WardId_Invalid")]
    public int WardId { get; set; }

    // Location Information with auto-trim
    [StringLength(10, ErrorMessage = "Property_PropertyNo_MaxLen_10")]
    public string? PropertyNo
    {
        get => _propertyNo;
        set => _propertyNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(10, ErrorMessage = "Property_PartitionNo_MaxLen_10")]
    public string? PartitionNo
    {
        get => _partitionNo;
        set => _partitionNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Property Classification
    [Range(1, int.MaxValue, ErrorMessage = "Property_PropertyTypeId_Invalid")]
    public int? PropertyTypeId { get; set; }

    [StringLength(30, ErrorMessage = "Property_UPICId_MaxLen_30")]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "Property_UPICId_Invalid")]
    public string? UPICId
    {
        get => _upicId;
        set => _upicId = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public bool? OpenPlot { get; set; }

    [StringLength(30, ErrorMessage = "Property_CSN_MaxLen_30")]
    public string? CSN
    {
        get => _csn;
        set => _csn = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_SubZoneNo_MaxLen_20")]
    public string? SubZoneNo
    {
        get => _subZoneNo;
        set => _subZoneNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_PlotNo_MaxLen_20")]
    public string? PlotNo
    {
        get => _plotNo;
        set => _plotNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Range(1, int.MaxValue, ErrorMessage = "Property_CategoryId_Invalid")]
    public int? CategoryId { get; set; }


    // Owner Information
    [StringLength(20, ErrorMessage = "Property_OwnerTitle_MaxLen_20")]
    public string? OwnerTitle
    {
        get => _ownerTitle;
        set => _ownerTitle = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OwnerName_MaxLen_1000")]
    public string? OwnerName
    {
        get => _ownerName;
        set => _ownerName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_OwnerTitleEnglish_MaxLen_20")]
    public string? OwnerTitleEnglish
    {
        get => _ownerTitleEnglish;
        set => _ownerTitleEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OwnerNameEnglish_MaxLen_1000")]
    public string? OwnerNameEnglish
    {
        get => _ownerNameEnglish;
        set => _ownerNameEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Occupier Information
    [StringLength(20, ErrorMessage = "Property_OccupierTitle_MaxLen_20")]
    public string? OccupierTitle
    {
        get => _occupierTitle;
        set => _occupierTitle = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OccupierName_MaxLen_1000")]
    public string? OccupierName
    {
        get => _occupierName;
        set => _occupierName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_OccupierTitleEnglish_MaxLen_20")]
    public string? OccupierTitleEnglish
    {
        get => _occupierTitleEnglish;
        set => _occupierTitleEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OccupierNameEnglish_MaxLen_1000")]
    public string? OccupierNameEnglish
    {
        get => _occupierNameEnglish;
        set => _occupierNameEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Flat/Shop Information
    [StringLength(100, ErrorMessage = "Property_FlatOrShopNo_MaxLen_100")]
    public string? FlatOrShopNo
    {
        get => _flatOrShopNo;
        set => _flatOrShopNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopName_MaxLen_200")]
    public string? FlatOrShopName
    {
        get => _flatOrShopName;
        set => _flatOrShopName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(100, ErrorMessage = "Property_FlatOrShopNoEnglish_MaxLen_100")]
    public string? FlatOrShopNoEnglish
    {
        get => _flatOrShopNoEnglish;
        set => _flatOrShopNoEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopNameEnglish_MaxLen_200")]
    public string? FlatOrShopNameEnglish
    {
        get => _flatOrShopNameEnglish;
        set => _flatOrShopNameEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Address Information
    [StringLength(500, ErrorMessage = "Property_Address_MaxLen_500")]
    public string? Address
    {
        get => _address;
        set => _address = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_Location_MaxLen_200")]
    public string? Location
    {
        get => _location;
        set => _location = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(500, ErrorMessage = "Property_AddressEnglish_MaxLen_500")]
    public string? AddressEnglish
    {
        get => _addressEnglish;
        set => _addressEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_LocationEnglish_MaxLen_200")]
    public string? LocationEnglish
    {
        get => _locationEnglish;
        set => _locationEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Contact Information
    [StringLength(13, ErrorMessage = "Property_MobileNo_MaxLen_13")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Property_MobileNo_Invalid")]
    public string? MobileNo
    {
        get => _mobileNo;
        set => _mobileNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(100, ErrorMessage = "Property_EmailId_MaxLen_100")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Property_EmailId_Invalid")]
    public string? EmailId
    {
        get => _emailId;
        set => _emailId = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }

    // Society Information
    [Range(1, int.MaxValue, ErrorMessage = "Property_SocietyDetailId_Invalid")]
    public int? SocietyDetailId { get; set; }

    // Status
    public bool MarkedForDeletion { get; set; } = false;
}

/// <summary>
/// DTO for updating existing Property records
/// </summary>
public class UpdatePropertyDto : UpdateBaseDtos
{
    private string? _propertyNo;
    private string? _partitionNo;
    private string? _upicId;
    private string? _csn;
    private string? _subZoneNo;
    private string? _plotNo;
    private string? _type;
    private string? _ownerTitle;
    private string? _ownerName;
    private string? _ownerTitleEnglish;
    private string? _ownerNameEnglish;
    private string? _occupierTitle;
    private string? _occupierName;
    private string? _occupierTitleEnglish;
    private string? _occupierNameEnglish;
    private string? _flatOrShopNo;
    private string? _flatOrShopName;
    private string? _flatOrShopNoEnglish;
    private string? _flatOrShopNameEnglish;
    private string? _address;
    private string? _location;
    private string? _addressEnglish;
    private string? _locationEnglish;
    private string? _mobileNo;
    private string? _emailId;

    // Required Foreign Keys
    [Required(ErrorMessage = "Property_TaxZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Property_TaxZoneId_Invalid")]
    public int TaxZoneId { get; set; }

    [Required(ErrorMessage = "Property_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Property_WardId_Invalid")]
    public int WardId { get; set; }

    // Location Information with auto-trim
    [StringLength(10, ErrorMessage = "Property_PropertyNo_MaxLen_10")]
    public string? PropertyNo
    {
        get => _propertyNo;
        set => _propertyNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(10, ErrorMessage = "Property_PartitionNo_MaxLen_10")]
    public string? PartitionNo
    {
        get => _partitionNo;
        set => _partitionNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Property Classification
    [Range(1, int.MaxValue, ErrorMessage = "Property_PropertyTypeId_Invalid")]
    public int? PropertyTypeId { get; set; }

    [StringLength(30, ErrorMessage = "Property_UPICId_MaxLen_30")]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "Property_UPICId_Invalid")]
    public string? UPICId
    {
        get => _upicId;
        set => _upicId = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public bool? OpenPlot { get; set; }

    [StringLength(30, ErrorMessage = "Property_CSN_MaxLen_30")]
    public string? CSN
    {
        get => _csn;
        set => _csn = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_SubZoneNo_MaxLen_20")]
    public string? SubZoneNo
    {
        get => _subZoneNo;
        set => _subZoneNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_PlotNo_MaxLen_20")]
    public string? PlotNo
    {
        get => _plotNo;
        set => _plotNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Range(1, int.MaxValue, ErrorMessage = "Property_CategoryId_Invalid")]
    public int? CategoryId { get; set; }

    // Owner Information
    [StringLength(20, ErrorMessage = "Property_OwnerTitle_MaxLen_20")]
    public string? OwnerTitle
    {
        get => _ownerTitle;
        set => _ownerTitle = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OwnerName_MaxLen_1000")]
    public string? OwnerName
    {
        get => _ownerName;
        set => _ownerName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_OwnerTitleEnglish_MaxLen_20")]
    public string? OwnerTitleEnglish
    {
        get => _ownerTitleEnglish;
        set => _ownerTitleEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OwnerNameEnglish_MaxLen_1000")]
    public string? OwnerNameEnglish
    {
        get => _ownerNameEnglish;
        set => _ownerNameEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Occupier Information
    [StringLength(20, ErrorMessage = "Property_OccupierTitle_MaxLen_20")]
    public string? OccupierTitle
    {
        get => _occupierTitle;
        set => _occupierTitle = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OccupierName_MaxLen_1000")]
    public string? OccupierName
    {
        get => _occupierName;
        set => _occupierName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(20, ErrorMessage = "Property_OccupierTitleEnglish_MaxLen_20")]
    public string? OccupierTitleEnglish
    {
        get => _occupierTitleEnglish;
        set => _occupierTitleEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(1000, ErrorMessage = "Property_OccupierNameEnglish_MaxLen_1000")]
    public string? OccupierNameEnglish
    {
        get => _occupierNameEnglish;
        set => _occupierNameEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Flat/Shop Information
    [StringLength(100, ErrorMessage = "Property_FlatOrShopNo_MaxLen_100")]
    public string? FlatOrShopNo
    {
        get => _flatOrShopNo;
        set => _flatOrShopNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopName_MaxLen_200")]
    public string? FlatOrShopName
    {
        get => _flatOrShopName;
        set => _flatOrShopName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(100, ErrorMessage = "Property_FlatOrShopNoEnglish_MaxLen_100")]
    public string? FlatOrShopNoEnglish
    {
        get => _flatOrShopNoEnglish;
        set => _flatOrShopNoEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_FlatOrShopNameEnglish_MaxLen_200")]
    public string? FlatOrShopNameEnglish
    {
        get => _flatOrShopNameEnglish;
        set => _flatOrShopNameEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Address Information
    [StringLength(500, ErrorMessage = "Property_Address_MaxLen_500")]
    public string? Address
    {
        get => _address;
        set => _address = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_Location_MaxLen_200")]
    public string? Location
    {
        get => _location;
        set => _location = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(500, ErrorMessage = "Property_AddressEnglish_MaxLen_500")]
    public string? AddressEnglish
    {
        get => _addressEnglish;
        set => _addressEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "Property_LocationEnglish_MaxLen_200")]
    public string? LocationEnglish
    {
        get => _locationEnglish;
        set => _locationEnglish = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Contact Information
    [StringLength(13, ErrorMessage = "Property_MobileNo_MaxLen_13")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Property_MobileNo_Invalid")]
    public string? MobileNo
    {
        get => _mobileNo;
        set => _mobileNo = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(100, ErrorMessage = "Property_EmailId_MaxLen_100")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Property_EmailId_Invalid")]
    public string? EmailId
    {
        get => _emailId;
        set => _emailId = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }

    // Society Information
    [Range(1, int.MaxValue, ErrorMessage = "Property_SocietyDetailId_Invalid")]
    public int? SocietyDetailId { get; set; }

    public double? TotalPlotArea { get; set; }
    public double? Length { get; set; }
    public double? Width { get; set; }
    // Status
    public bool MarkedForDeletion { get; set; } = false;
}
