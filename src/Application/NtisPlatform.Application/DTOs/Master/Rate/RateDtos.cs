using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;
//main dto
public class RateDto: CommonBaseDtos
{
    public int? ID { get; set; }
    public int? Year { get; set; }
    public string? TaxZoneNo { get; set; } = string.Empty;
    public string? FloorID { get; set; } = string.Empty;
    public string? ConstructionID { get; set; } = string.Empty;
    public string? TypeOfUseGroupID { get; set; } = string.Empty;
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public decimal? RateSquareMeter { get; set; }
    public decimal? RateSquareFeet { get; set; }
    public string? RateSectionNo { get; set; } = string.Empty;
    public string? RateRemark { get; set; } = string.Empty;   
}


public class CreateRateDto: CreateCommonBaseDtos
{
    public int? ID { get; set; }

    [Range(1, 9999, ErrorMessage = "Rate_Year_Range_1_9999")]
    public int? Year { get; set; }

    [StringLength(10, ErrorMessage = "Rate_TaxZoneNo_MaxLen_10")]
    public string TaxZoneNo { get; set; } = string.Empty;

    [StringLength(5, ErrorMessage = "Rate_FloorID_MaxLen_5")]
    public string FloorID { get; set; } = string.Empty;

    [StringLength(7, ErrorMessage = "Rate_ConstructionID_MaxLen_7")]
    public string ConstructionID { get; set; } = string.Empty;

    // No StringLength/Range in entity for this (keeping as-is)
    public string TypeOfUseGroupID { get; set; } = string.Empty;

    [Range(1, 9999, ErrorMessage = "Rate_MinYear_Range_1_9999")]
    public int? MinYear { get; set; }

    [Range(1, 9999, ErrorMessage = "Rate_MaxYear_Range_1_9999")]
    public int? MaxYear { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareMeter_Min_0")]
    public decimal? RateSquareMeter { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareFeet_Min_0")]
    public decimal? RateSquareFeet { get; set; }
    public string RateSectionNo { get; set; } = string.Empty;
    [StringLength(20, ErrorMessage = "RateRemark must be at most 20 characters")]
    public string? RateRemark { get; set; } = string.Empty;
}

public class UpdateRateDto: UpdateCommonBaseDtos
{
    public int? ID { get; set; }

    [Range(1, 9999, ErrorMessage = "Rate_Year_Range_1_9999")]
    public int? Year { get; set; }

    [StringLength(10, ErrorMessage = "Rate_TaxZoneNo_MaxLen_10")]
    public string? TaxZoneNo { get; set; } = string.Empty;

    [StringLength(5, ErrorMessage = "Rate_FloorID_MaxLen_5")]
    public string? FloorID { get; set; } = string.Empty;

    [StringLength(7, ErrorMessage = "Rate_ConstructionID_MaxLen_7")]
    public string? ConstructionID { get; set; } = string.Empty;

    public string? TypeOfUseGroupID { get; set; } = string.Empty;

    [Range(1, 9999, ErrorMessage = "Rate_MinYear_Range_1_9999")]
    public int? MinYear { get; set; }

    [Range(1, 9999, ErrorMessage = "Rate_MaxYear_Range_1_9999")]
    public int? MaxYear { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareMeter_Min_0")]
    public decimal? RateSquareMeter { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Rate_RateSquareFeet_Min_0")]
    public decimal? RateSquareFeet { get; set; }
    public string? RateSectionNo { get; set; } = string.Empty;
    [StringLength(20, ErrorMessage = "RateRemark must be at most 20 characters")]
    public string? RateRemark { get; set; } = string.Empty;
}
