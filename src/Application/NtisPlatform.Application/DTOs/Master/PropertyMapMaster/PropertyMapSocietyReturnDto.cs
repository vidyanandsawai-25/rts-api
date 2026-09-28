using System;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

/// <summary>
/// Lightweight DTO for GET /api/PropertyMapMaster/mapped-properties-society-wise.
/// Contains flat new property fields and flat old mapped property fields without heavy nested collections.
/// </summary>
public class PropertyMapSocietyReturnDto
{
    // ── New Property Info (PropertyEntity / PropertyMast) ────────────────────
    public int PropertyId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierNameEnglish { get; set; }
    public string? Address { get; set; }
    public string? AddressEnglish { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? CSN { get; set; }
    public string? PlotNo { get; set; }
    public int WardId { get; set; }
    public int TaxZoneId { get; set; }
    public int? PropertyTypeId { get; set; }
    public int? CategoryId { get; set; }
    public int? WingDetailId { get; set; }

    // ── Mapping & Old Property Info (PropertyMapMaster & PropertyMastOld) ───
    public string MappingCategory { get; set; } = string.Empty;
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
    public string? OldPlotNo { get; set; }
    public string? OldCSN { get; set; }
    public double? OldPlotArea { get; set; }
    public string? OldConstructionYear { get; set; }
    public int? OldAssessmentYear { get; set; }
    public string? OldFloor { get; set; }
    public string? OldConstructionTypeOfUseId { get; set; }
    public string? OldUseType { get; set; }
    public double? OldConstructionArea { get; set; }
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
}
