using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class RetentionYearWiseDto : BaseDtos
{
    public int RetentionPolicyYearId { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
    public double? FactorValue { get; set; }

}
public class CreateRetentionYearWiseDto : CreateBaseDtos, IValidatableObject
{
    [Required(ErrorMessage = "FromYear_Required")]
    [Range(1900, 9999, ErrorMessage = "FromYear_MustBe4Digits")]
    public int FromYear { get; set; }

    [Required(ErrorMessage = "ToYear_Required")]
    [Range(1900, 9999, ErrorMessage = "ToYear_MustBe4Digits")]
    public int ToYear { get; set; }

    [Required(ErrorMessage = "FactorValue_Required")]
    [Range(0, 100, ErrorMessage = "FactorValue_Range")]
    public double FactorValue { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromYear > ToYear)
        {
            yield return new ValidationResult(
                "FromYear_MustBeLessThanOrEqualToYear",
                new[] { nameof(FromYear), nameof(ToYear) }
            );
        }
    }
}

public class UpdateRetentionYearWiseDto : UpdateBaseDtos, IValidatableObject
{

    [Required(ErrorMessage = "FromYear_Required")]
    [Range(1900, 9999, ErrorMessage = "FromYear_MustBe4Digits")]
    public int FromYear { get; set; }

    [Required(ErrorMessage = "ToYear_Required")]
    [Range(1900, 9999, ErrorMessage = "ToYear_MustBe4Digits")]
    public int ToYear { get; set; }

    [Required(ErrorMessage = "FactorValue_Required")]
    [Range(0, 100, ErrorMessage = "FactorValue_Range")]
    public double FactorValue { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromYear > ToYear)
        {
            yield return new ValidationResult(
                "FromYear_MustBeLessThanOrEqualToYear",
                new[] { nameof(FromYear), nameof(ToYear) }
            );
        }
    }
}