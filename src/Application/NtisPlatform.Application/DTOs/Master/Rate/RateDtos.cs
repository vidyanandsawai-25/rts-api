using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;
//main dto
public class RateDto: BaseDtos
{
    public int RateId { get; set; } = 0;

    public int FloorId { get; set; }

    public int ConstructionTypeId { get; set; }

    public int TypeOfUseGroupId { get; set; }

    public int RateSectionId { get; set; }

    public int TaxZoneId { get; set; }

    public string RateRemark { get; set; } = string.Empty;

    public int? Year { get; set; }

    public int YearRangeRVId { get; set; }

    public decimal? RateSquareFeet { get; set; }

    public decimal? RateSquareMeter { get; set; }
}


public class CreateRateDto: CreateBaseDtos
{

    [Range(1, 9999, ErrorMessage = "Rate_Year_Range_1_9999")]
    public int? Year { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_TaxZoneId_Required")]
    public int TaxZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_FloorId_Required")]
    public int FloorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_ConstructionTypeId_Required")]
    public int ConstructionTypeId { get; set; } 

    // No StringLength/Range in entity for this (keeping as-is)
    public int TypeOfUseGroupId { get; set; }

    [Range(1, 9999, ErrorMessage = "Rate_YearRangeRVId_1_9999")]
    public int? YearRangeRVId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareMeter_Min_0")]
    public decimal? RateSquareMeter { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareFeet_Min_0")]
    public decimal? RateSquareFeet { get; set; }
    public int RateSectionId { get; set; } 

    [StringLength(20, ErrorMessage = "RateRemark must be at most 20 characters")]
    public string? RateRemark { get; set; } = string.Empty;
}

public class UpdateRateDto: UpdateBaseDtos
{

    [Range(1, 9999, ErrorMessage = "Rate_Year_Range_1_9999")]
    public int? Year { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_TaxZoneId_Required")]
    public int? TaxZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_FloorId_Required")]
    public int? FloorID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_ConstructionTypeId_Required")]
    public int? ConstructionTypeId { get; set; }

    public int? TypeOfUseGroupID { get; set; }

    [Range(1, 9999, ErrorMessage = "Rate_YearRangeRVId_1_9999")]
    public int? YearRangeRVId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareMeter_Min_0")]
    public decimal? RateSquareMeter { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareFeet_Min_0")]
    public decimal? RateSquareFeet { get; set; }
    public int? RateSectionId { get; set; }

    [StringLength(20, ErrorMessage = "RateRemark must be at most 20 characters")]
    public string? RateRemark { get; set; } = string.Empty;
}
