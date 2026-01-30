using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class RetentionFactWiseDto : CommonBaseDtos
{
    public int ID { get; set; } 
    public double? FromFactor { get; set; }
    public double? ToFactor { get; set; }
    public double? FactorValue { get; set; }

}
public class CreateRetentionFactWiseDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "FromFactor_Required")]
    [Range(0, 100, ErrorMessage = "FromFactor_Range")]
    public double FromFactor { get; set; }

    [Required(ErrorMessage = "ToFactor_Required")]
    [Range(0, 100, ErrorMessage = "ToFactor_Range")]
    public double ToFactor { get; set; }

    [Required(ErrorMessage = "FactorValue_Required")]
    [Range(0, 100, ErrorMessage = "FactorValue_Range")]
    public double FactorValue { get; set; }
}

public class UpdateRetentionFactWiseDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "FromFactor_Required")]
    [Range(0, 100, ErrorMessage = "FromFactor_Range")]
    public double FromFactor { get; set; }

    [Required(ErrorMessage = "ToFactor_Required")]
    [Range(0, 100, ErrorMessage = "ToFactor_Range")]
    public double ToFactor { get; set; }

    [Required(ErrorMessage = "FactorValue_Required")]
    [Range(0, 100, ErrorMessage = "FactorValue_Range")]
    public double FactorValue { get; set; }
}
