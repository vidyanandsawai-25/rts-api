using System.ComponentModel.DataAnnotations;
namespace NtisPlatform.Application.DTOs;


public class DepreciationDtos : BaseDtos
{
    public int ConstructionTypeId { get; set; }
    public int MinYear { get; set; }
    public int MaxYear { get; set; }
    public decimal Rate { get; set; }
    public int YearRangeRVId { get; set; }
}

public class CreateDepreciationDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Depreciation_ConstructionId_Required")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "Depreciation_MinYear_Required")]
    [Range(0, 999, ErrorMessage = "Depreciation_MinYear_Range_0_999")]
    public int MinYear { get; set; }

    [Required(ErrorMessage = "Depreciation_MaxYear_Required")]
    [Range(0, 999, ErrorMessage = "Depreciation_MaxYear_Range_0_999")]
    public int MaxYear { get; set; }

    [Required(ErrorMessage = "Depreciation_Rate_Required")]
    [Range(typeof(decimal), "0", "9999", ErrorMessage = "Depreciation_Rate_Range_0_9999")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "Depreciation_YearRangeRVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Depreciation_YearRangeRVId_Range_1_IntMax")]
    public int YearRangeRVId { get; set; }

}
public class UpdateDepreciationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Depreciation_ID_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Depreciation_ID_Range_1_IntMax")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Depreciation_ConstructionId_Required")]
    public int ConstructionTypeId { get; set; } = 0;

    [Required(ErrorMessage = "Depreciation_MinYear_Required")]
    [Range(0, 999, ErrorMessage = "Depreciation_MinYear_Range_0_999")]
    public int MinYear { get; set; }

    [Required(ErrorMessage = "Depreciation_MaxYear_Required")]
    [Range(0, 999, ErrorMessage = "Depreciation_MaxYear_Range_0_999")]
    public int MaxYear { get; set; }

    [Required(ErrorMessage = "Depreciation_Rate_Required")]
    [Range(typeof(decimal), "0", "9999", ErrorMessage = "Depreciation_Rate_Range_0_9999")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "Depreciation_YearRangeRVId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Depreciation_YearRangeRVId_Range_1_IntMax")]
    public int YearRangeRVId { get; set; }
}
