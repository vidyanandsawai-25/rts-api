using System;
using System.Collections.Generic;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

// ─── Transaction record DTOs ──────────────────────────────────────────────────

/// <summary>Flattened row from PTIS.TransMast (new property, TaxId=21).</summary>
public class TransMastDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int FinanceYearId { get; set; }
    public string CalculationType { get; set; } = string.Empty;
    public decimal CalculationValue { get; set; }
    public int TaxId { get; set; }
    public decimal TaxAmount { get; set; }
}

/// <summary>Flattened row from PTIS.TransMastOld (old property).</summary>
public class TransMastOldDto
{
    public int Id { get; set; }
    public int PropertyMastOldId { get; set; }
    public int FinanceYearId { get; set; }
    public string CalculationType { get; set; } = string.Empty;
    public decimal CalculationValue { get; set; }
    public int TaxId { get; set; }
    public decimal TaxAmount { get; set; }
}

public class PropertyMapDetailReturnDto
{
    // ── Old Property Info (PropertyMastOld) ──────────────────────────────────
    public int PropertyId { get; set; }
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
    public List<PropertyDetailsOldDto> PropertyDetailsOld { get; set; } = new();

    // ── New Property Info (PropertyEntity / PropertyMast) ────────────────────
    public NewPropertyInfoDto? NewPropertyInfo { get; set; }

    /// <summary>Active detail rows from PropertyDetails for this new property.</summary>
    public List<NewPropertyDetailDto> NewPropertyDetails { get; set; } = new();

    /// <summary>TransMast rows for the new property where TaxId=21, IsActive=1.</summary>
    public List<TransMastDto> TransMastRecords { get; set; } = new();

    /// <summary>TransMastOld rows for the old property, IsActive=1.</summary>
    public List<TransMastOldDto> TransMastOldRecords { get; set; } = new();
}

// ─── New property master block ─────────────────────────────────────────────

public class NewPropertyInfoDto
{
    public int Id { get; set; }
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

    // Ward
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? WardDescription { get; set; }

    // Tax Zone
    public int TaxZoneId { get; set; }
    public string? TaxZoneNo { get; set; }
    public string? TaxZoneRemark { get; set; }

    // Property Type
    public int? PropertyTypeId { get; set; }
    public string? PropertyTypeDescription { get; set; }

    // Category
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

// ─── New property detail rows ─────────────────────────────────────────────

public class NewPropertyDetailDto
{
    public int Id { get; set; }

    // Floor
    public int? FloorId { get; set; }
    public string? FloorCode { get; set; }
    public string? FloorDescription { get; set; }

    // Sub Floor
    public int? SubFloorId { get; set; }
    public string? SubFloorCode { get; set; }
    public string? SubFloorDescription { get; set; }

    // Type of Use
    public int TypeOfUseId { get; set; }
    public string? TypeOfUseCode { get; set; }
    public string? TypeOfUseDescription { get; set; }

    // Sub Type of Use
    public int? SubTypeOfUseId { get; set; }
    public string? SubTypeOfUseDescription { get; set; }

    // Construction Type
    public int? ConstructionTypeId { get; set; }
    public string? ConstructionCode { get; set; }
    public string? ConstructionTypeDescription { get; set; }

    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public double? CarpetAreaSqMeter { get; set; }
    public double? CarpetAreaSqFeet { get; set; }
    public double? BuiltupAreaSqMeter { get; set; }
    public double? BuiltupAreaSqFeet { get; set; }
    public int? NoOfRooms { get; set; }
    public bool? IsRenter { get; set; }
    public bool? IsTaxable { get; set; }
    public bool? IsOpenPlot { get; set; }
}
