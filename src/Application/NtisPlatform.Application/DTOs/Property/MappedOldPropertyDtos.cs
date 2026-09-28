namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Top-level response DTO for GET /api/property/{propertyId}/mapped-old-properties
/// Returns all mapped old properties for a merged property.
/// </summary>
public class MappedOldPropertyResponseDto
{
    public int PropertyId { get; set; }
    public List<MappedOldPropertyMastDto> MappedOldProperties { get; set; } = new();
}


/// <summary>
/// DTO representing PropertyMastOld data for a mapped old property.
/// Contains all columns from the PropertyMastOld table.
/// </summary>
public class MappedOldPropertyMastDto : BaseDtos
{
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldEgovNo { get; set; }
    public int? OldPropertyTypeId { get; set; }
    public double? OldALV { get; set; }
    public double? OldRV { get; set; }
    public double? OldGeneralTax { get; set; }
    public double? OldTotalTax { get; set; }
    public string? OldZoneNo { get; set; }
    public string? OldSubZoneNo { get; set; }
    public string? OldPlotNo { get; set; }
    public string? OldCSN { get; set; }
    public double? OldPlotArea { get; set; }
    public int? OldAssessmentYear { get; set; }
    public string? OldFloor { get; set; }
    public string? OldConstructionTypeOfUseId { get; set; }
    public string? OldUseType { get; set; }
    public double? OldConstructionArea { get; set; }
    public int? OldConstructionYear { get; set; }
    public DateTime? OldOCDate { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldOccupierName { get; set; }
    public string? OldAddress { get; set; }
    public string? OldOwnerNameEnglish { get; set; }
    public string? OldOccupierNameEnglish { get; set; }
    public string? OldAddressEnglish { get; set; }
    public int? NoOfOldToilets { get; set; }
    public int? OldTotalRooms { get; set; }
    public string? OldSocietyName { get; set; }
    public string? OldEmailId { get; set; }
    public double? OldParkingAreaSqFt { get; set; }
    public double? OldParkingAreaSqMtr { get; set; }
    public DateTime? OldAssessmentDate { get; set; }
    public string? OldFlatOrShopNumber { get; set; }
    public string? OldWing { get; set; }
    public string? OldMobileNo { get; set; }
    public int? MappedNewBuildingId { get; set; }

    // --- Nested floor details from PropertyDetailsOld ---
    public List<MappedOldPropertyFloorDetailDto> FloorDetails { get; set; } = new();
}

/// <summary>
/// DTO representing PropertyDetailsOld data for a mapped old property.
/// Contains all columns from the PropertyDetailsOld table.
/// </summary>
public class MappedOldPropertyFloorDetailDto : BaseDtos
{
    public int PropertyMastOldId { get; set; }
    public int? OldFloorId { get; set; }
    public int? OldSubFloorId { get; set; }
    public int? OldConstructionYear { get; set; }
    public int? OldAssessmentYear { get; set; }
    public int? OldConstructionTypeId { get; set; }
    public int? OldTypeOfUseId { get; set; }
    public DateTime? OldOCDate { get; set; }
    public int? OldSubTypeOfUseId { get; set; }
    public double? OldCarpetAreaSqMeter { get; set; }
    public double? OldCarpetAreaSqFeet { get; set; }
    public double? OldBuiltupAreaSqMeter { get; set; }
    public double? OldBuiltupAreaSqFeet { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}