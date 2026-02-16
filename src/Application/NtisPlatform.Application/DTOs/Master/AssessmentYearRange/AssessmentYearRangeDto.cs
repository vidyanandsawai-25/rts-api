using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class AssessmentYearRangeDto : CommonBaseDtos
{
    public int YearId { get; set; }
    public int FromYear { get; set; }
    public int ToYear { get; set; }   

}
public class CreateAssessmentYearRangeDto : CreateCommonBaseDtos, IValidatableObject
{
    [Required(ErrorMessage = "FromYear_Required")]
    [Range(1900, 9999, ErrorMessage = "FromYear_MustBe4Digits")]
    public int FromYear { get; set; }

    [Required(ErrorMessage = "ToYear_Required")]
    [Range(1900, 9999, ErrorMessage = "ToYear_MustBe4Digits")]
    public int ToYear { get; set; }

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

public class UpdateAssessmentYearRangeDto : UpdateCommonBaseDtos, IValidatableObject
{
   
    [Required(ErrorMessage = "FromYear_Required")]
    [Range(1900, 9999, ErrorMessage = "FromYear_MustBe4Digits")]
    public int FromYear { get; set; }

    [Required(ErrorMessage = "ToYear_Required")]
    [Range(1900, 9999, ErrorMessage = "ToYear_MustBe4Digits")]
    public int ToYear { get; set; }

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
