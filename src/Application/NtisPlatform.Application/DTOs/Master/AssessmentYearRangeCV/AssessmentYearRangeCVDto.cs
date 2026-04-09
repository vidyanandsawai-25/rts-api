using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

    public class AssessmentYearRangeCVDto : BaseDtos
    {
        public int Id { get; set; }
        public int FromYear { get; set; }
        public int ToYear { get; set; }
    }
    public class CreateAssessmentYearRangeCVDto : CreateBaseDtos, IValidatableObject
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

    public class UpdateAssessmentYearRangeCVDto : UpdateBaseDtos, IValidatableObject
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

