using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;
//main dto
public class RateDto : BaseDtos
{
    public int Id { get; set; } = 0;
    public int FloorId { get; set; }
    public int ConstructionTypeId { get; set; }
    public int TypeOfUseGroupId { get; set; }
    public int RateSectionId { get; set; }
    public int TaxZoneId { get; set; }
    public string RateRemark { get; set; } = string.Empty;

    public int YearRangeRVId { get; set; }
    public decimal? RateSquareFeet { get; set; }
    public decimal? RateSquareMeter { get; set; }
}

public class DetailedRateDto : BaseDtos
{
    public string TaxZone { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string ConstructionType { get; set; } = string.Empty;
    public string TypeOfUseGroup { get; set; } = string.Empty;
    public string YearRangeRV { get; set; } = string.Empty;
    public string RateSection { get; set; } = string.Empty;
    public string RateRemark { get; set; } = string.Empty;
    public decimal? RateSquareFeet { get; set; }
    public decimal? RateSquareMeter { get; set; }
    public int TaxZoneId { get; set; }
    public int FloorId { get; set; }
    public int ConstructionTypeId { get; set; }
    public int TypeOfUseGroupId { get; set; }
    public int YearRangeRVId { get; set; }
    public int RateSectionId { get; set; }

    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreateRateDto : CreateBaseDtos
{

    [Range(1, int.MaxValue, ErrorMessage = "Rate_TaxZoneId_Required")]
    public int TaxZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_FloorId_Required")]
    public int FloorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_ConstructionTypeId_Required")]
    public int ConstructionTypeId { get; set; }

    // No StringLength/Range in entity for this (keeping as-is)
    public int TypeOfUseGroupId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_YearRangeRVId_Required")]
    public int? YearRangeRVId { get; set; }

    [Range(typeof(decimal), "0", "99999", ErrorMessage = "Rate_RateSquareMeter_Range_0_99999")]
    public decimal? RateSquareMeter { get; set; }

    [Range(typeof(decimal), "0", "1076390", ErrorMessage = "Rate_RateSquareFeet_Range_0_1076390")]
    public decimal? RateSquareFeet { get; set; }
    
    public int RateSectionId { get; set; }

    [StringLength(40, ErrorMessage = "Rate_RateRemark_MaxLen_40")]
    public string? RateRemark { get; set; } = string.Empty;
}

public class CreateOpenPlotRateDto : CreateBaseDtos
{

    [Range(1, int.MaxValue, ErrorMessage = "Rate_TaxZoneId_Required")]
    public int TaxZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_FloorId_Required")]
    public int FloorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_TypeOfUseGroupId_Required")]
    public int TypeOfUseGroupId { get; set; }


    [Range(1, int.MaxValue, ErrorMessage = "Rate_YearRangeRVId_Required")]
    public int? YearRangeRVId { get; set; }

    [Range(typeof(decimal), "0", "99999", ErrorMessage = "Rate_RateSquareMeter_Range_0_99999")]
    public decimal? RateSquareMeter { get; set; }

    [Range(typeof(decimal), "0", "1076390", ErrorMessage = "Rate_RateSquareFeet_Range_0_1076390")]
    public decimal? RateSquareFeet { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_RateSectionId_Required")]
    public int RateSectionId { get; set; }

    [StringLength(40, ErrorMessage = "Rate_RateRemark_MaxLen_40")]
    public string? RateRemark { get; set; } = string.Empty;
}

public class UpdateRateDto : UpdateBaseDtos
{


    [Range(1, int.MaxValue, ErrorMessage = "Rate_TaxZoneId_Required")]
    public int? TaxZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_FloorId_Required")]
    public int? FloorID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_ConstructionTypeId_Required")]
    public int? ConstructionTypeId { get; set; }

    public int? TypeOfUseGroupID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rate_YearRangeRVId_Required")]
    public int? YearRangeRVId { get; set; }

    [Range(typeof(decimal), "0", "99999", ErrorMessage = "Rate_RateSquareMeter_Range_0_99999")]
    public decimal? RateSquareMeter { get; set; }

    [Range(typeof(decimal), "0", "1076390", ErrorMessage = "Rate_RateSquareFeet_Range_0_1076390")]
    public decimal? RateSquareFeet { get; set; }
    public int? RateSectionId { get; set; }

    [StringLength(40, ErrorMessage = "Rate_RateRemark_MaxLen_40")]
    public string? RateRemark { get; set; } = string.Empty;
}

public class TypeOfUseDetailsDto
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public string? TypeOfUseCode { get; set; }
    public int TypeOfUseGroupId { get; set; }
    public int? TypeOfUseCategoryId { get; set; }
    public string? TypeOfUseCategoryName { get; set; }
    public string? TypeOfUseCategoryCode { get; set; }
    public string? TypeOfUseGroupCode { get; set; }
    public string? GroupName { get; set; }
}