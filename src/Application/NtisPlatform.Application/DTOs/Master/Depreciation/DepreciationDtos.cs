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
    [Required(ErrorMessage = "ConstructionId_Required")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "MinYear_Required")]
    [Range(0, 100, ErrorMessage = "MinYear_Range_0_9999")]
    public int MinYear { get; set; }

    [Required(ErrorMessage = "MaxYear_Required")]
    [Range(0, 100, ErrorMessage = "MaxYear_Range_0_9999")]
    public int MaxYear { get; set; }

    [Required(ErrorMessage = "Rate_Required")]
    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "Rate_Range_0_999999999999_99")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "YearRangeRVId_Required")]
    [Range(0, 9999, ErrorMessage = "YearRangeRVId_Range_0_9999")]
    public int YearRangeRVId { get; set; }
}
public class UpdateDepreciationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ID_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ID_Min_1")]
    public int Id { get; set; }

    [Required(ErrorMessage = "ConstructionId_Required")]
    public int ConstructionTypeId { get; set; } = 0;

    [Required(ErrorMessage = "MinYear_Required")]
    [Range(0, 100, ErrorMessage = "MinYear_Range_0_9999")]
    public int MinYear { get; set; }

    [Required(ErrorMessage = "MaxYear_Required")]
    [Range(0, 100, ErrorMessage = "MaxYear_Range_0_9999")]
    public int MaxYear { get; set; }

    [Required(ErrorMessage = "Rate_Required")]
    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "Rate_Range_0_999999999999_99")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "YearRangeRVId_Required")]
    [Range(0, 9999, ErrorMessage = "YearRangeRVId_Range_0_9999")]
    public int YearRangeRVId { get; set; }
}