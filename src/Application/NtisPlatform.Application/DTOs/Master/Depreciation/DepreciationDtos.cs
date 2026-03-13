using System.ComponentModel.DataAnnotations;
namespace NtisPlatform.Application.DTOs;


public class DepreciationDtos : BaseDtos
{
    public int ID { get; set; }
    public string ConstructionId { get; set; } = string.Empty;
    public int MinYear { get; set; }
    public int MaxYear { get; set; }
    public decimal Rate { get; set; }
    public int Year { get; set; }
}

public class CreateDepreciationDto : CreateBaseDtos
{
    [Range(0, int.MaxValue, ErrorMessage = "ID_Min_0")]
    public int ID { get; set; }

    [Required(ErrorMessage = "ConstructionId_Required")]
    [StringLength(7, ErrorMessage = "ConstructionId_MaxLen_7")]
    public string ConstructionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "MinYear_Required")]
    [Range(0, 100, ErrorMessage = "MinYear_Range_0_9999")]
    public int MinYear { get; set; }

    [Required(ErrorMessage = "MaxYear_Required")]
    [Range(0, 100, ErrorMessage = "MaxYear_Range_0_9999")]
    public int MaxYear { get; set; }

    [Required(ErrorMessage = "Rate_Required")]
    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "Rate_Range_0_999999999999_99")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "Year_Required")]
    [Range(1900, 9999, ErrorMessage = "Year_Range_1900_9999")]
    public int Year { get; set; }
}
public class UpdateDepreciationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ID_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ID_Min_1")]
    public int ID { get; set; }

    [Required(ErrorMessage = "ConstructionId_Required")]
    [StringLength(7, ErrorMessage = "ConstructionId_MaxLen_7")]
    public string ConstructionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "MinYear_Required")]
    [Range(0, 100, ErrorMessage = "MinYear_Range_0_9999")]
    public int MinYear { get; set; }

    [Required(ErrorMessage = "MaxYear_Required")]
    [Range(0, 100, ErrorMessage = "MaxYear_Range_0_9999")]
    public int MaxYear { get; set; }

    [Required(ErrorMessage = "Rate_Required")]
    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "Rate_Range_0_999999999999_99")]
    public decimal Rate { get; set; }

    [Required(ErrorMessage = "Year_Required")]
    [Range(1900, 9999, ErrorMessage = "Year_Range_1900_9999")]
    public int Year { get; set; }
}