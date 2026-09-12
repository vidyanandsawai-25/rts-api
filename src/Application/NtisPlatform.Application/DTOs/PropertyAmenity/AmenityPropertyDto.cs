using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property;

public class AmenityPropertyDto
{
    public int Id { get; set; }
    public int? SocietyDetailId { get; set; }
    public int? PropertyDetailsId { get; set; }
    public int? PropertyTypeId { get; set; }
    public int TaxZoneId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int? CategoryId { get; set; }

    // Added to mirror UpdateAmenityDto in response
    public bool? OpenPlot { get; set; }
    public int? PropertySeqNo { get; set; }
    public int? NoOfFlat { get; set; }
    public int? NoOfShop { get; set; }
    public int? WorkflowStageId { get; set; }
    public string? Bhk { get; set; }
    public int? PropertyFloorId { get; set; }
    public int NoOfFloorAttachToAmenity { get; set; }
    public int? FloorId { get; set; }
    public int? ConstructionTypeId { get; set; }
    public int? TypeOfUseId { get; set; }
    public int? SubTypeOfUseId { get; set; }
    public int? NoOfRooms { get; set; }
    public string? ConstructionYear { get; set; }
    public int? SubFloorId { get; set; }
    public string? AssessmentYear { get; set; }
    public double? CarpetAreaSqMeter { get; set; }
    public double? CarpetAreaSqFeet { get; set; }
    public double? BuiltupAreaSqMeter { get; set; }
    public double? BuiltupAreaSqFeet { get; set; }
    public string? DocumentType { get; set; }
    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public bool IsVerified { get; set; }
    public bool IsMerged { get; set; }
}

public class UpdateAmenityDto : UpdateBaseDtos
{
    [Required]
    public int TaxZoneId { get; set; }

    public int? PropertyTypeId { get; set; }

    [StringLength(10, ErrorMessage = "Amenity_PartitionNo_MaxLen_10")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "Amenity_PartitionNo_InvalidCharacters")]
    public string? PartitionNo { get; set; }

    public int? CategoryId { get; set; }
    public bool? OpenPlot { get; set; }
    public int? PropertySeqNo { get; set; }
    public int? NoOfFlat { get; set; }
    public int? NoOfShop { get; set; }
    public int? WorkflowStageId { get; set; }

    [StringLength(10, ErrorMessage = "Amenity_Bhk_MaxLen_10")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "Amenity_Bhk_InvalidCharacters")]
    public string? Bhk { get; set; }

    // PropertyDetails fields
    [Required]
    public int FloorId { get; set; }

    public int? PropertyFloorId { get; set; }
    public int? SubFloorId { get; set; }

    [Required]
    public int ConstructionTypeId { get; set; }

    [Required]
    public int TypeOfUseId { get; set; }

    public int? SubTypeOfUseId { get; set; }
    public int? NoOfRooms { get; set; }

    [StringLength(4, ErrorMessage = "Amenity_ConstructionYear_MaxLen_4")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "Amenity_ConstructionYear_InvalidCharacters")]
    public string? ConstructionYear { get; set; }

    [StringLength(4, ErrorMessage = "Amenity_AssessmentYear_MaxLen_4")]
    [RegularExpression(@"^[^<>{}]*$", ErrorMessage = "Amenity_AssessmentYear_InvalidCharacters")]
    public string? AssessmentYear { get; set; }

    public double? CarpetAreaSqMeter { get; set; }
    public double? CarpetAreaSqFeet { get; set; }
    public double? BuiltupAreaSqMeter { get; set; }
    public double? BuiltupAreaSqFeet { get; set; }
}


/// <summary>
/// Query parameters for getting max amenity partition number.
/// Used to find the next available amenity partition (e.g., AM1, AM2, AM3...) 
/// for a specific property within a ward.
/// </summary>
public class MaxPropertyAmenityQueryParameters
{
    /// <summary>
    /// The user ID requesting the max amenity partition.
    /// Must be greater than 0.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Property_UserId_Invalid")]
    [Required(ErrorMessage = "BasePropertyNo_Is_Required")]
    public int UserId { get; set; }

    /// <summary>
    /// The ward ID to search within.
    /// Must be greater than 0.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Property_WardId_Invalid")]
    [Required(ErrorMessage = "BasePropertyNo_Is_Required")]
    public int WardId { get; set; }

    /// <summary>
    /// The base property number to which amenities belong.
    /// Example: "1", "775787"
    /// Required for amenity partition generation.
    /// </summary>
    [Required(ErrorMessage = "BasePropertyNo_Is_Required")]
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Optional: Filter amenity partitions by WingDetailsId from WingDetailsMast.
    /// When provided, only partitions belonging to this specific wing are considered
    /// when calculating the next available amenity partition number.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "WingDetailsId is Invalid.")]
    public int? WingDetailsId { get; set; } = null;
}

/// <summary>
/// Response DTO for max amenity partition number lookup.
/// Contains information about the last used amenity partition and the next available one.
/// Wrapped by ApiResponse&lt;T&gt; at the controller level for status/message.
/// </summary>
public class GetMaxPropertyAmenityResponseDto
{
    /// <summary>
    /// The property id of the base property.
    /// </summary>
    public int? PropertyId { get; set; }

    /// <summary>
    /// The property number for which amenity partitions are being queried.
    /// </summary>
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Optional: The wing details ID used for filtering.
    /// </summary>
    public int? WingDetailsId { get; set; }

    /// <summary>
    /// The prefix found in the partition (e.g., "AM" for AM1, AM2).
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// The numeric part of the last amenity partition found.
    /// Example: For "AM5", this would be 5.
    /// </summary>
    public int LastPartitionNumber { get; set; }

    /// <summary>
    /// The complete last amenity partition found.
    /// Example: "AM5"
    /// </summary>
    public string? LastAmenityPartition { get; set; }

    /// <summary>
    /// The numeric part of the next available amenity partition.
    /// Example: For next partition "AM6", this would be 6.
    /// </summary>
    public int NextPartitionNumber { get; set; }

    /// <summary>
    /// The complete next available amenity partition.
    /// Example: "AM6"
    /// This is the value to use when creating a new amenity partition.
    /// </summary>
    public string? NextAmenityPartition { get; set; }
}