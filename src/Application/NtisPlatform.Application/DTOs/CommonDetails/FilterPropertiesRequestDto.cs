using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class FilterPropertiesRequestDto : BaseQueryParameters, IValidatableObject
{
    [Required(ErrorMessage = "CommonDetails_WardId_Required")]
    public int WardId { get; set; }
    public string? FromPropertyNo { get; set; }
    public string? ToPropertyNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? Wing { get; set; }

    [Required(ErrorMessage = "CommonDetails_UpdateCode_Required")]
    public string UpdateCode { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasFrom = !string.IsNullOrWhiteSpace(FromPropertyNo);
        var hasTo = !string.IsNullOrWhiteSpace(ToPropertyNo);
        var hasPropertyNo = !string.IsNullOrWhiteSpace(PropertyNo);
        var hasRange = hasFrom || hasTo;

        if (hasPropertyNo && hasRange)
        {
            yield return new ValidationResult(
                "CommonDetails_PropertyNo_OrRangeOnly",
                new[] { nameof(PropertyNo), nameof(FromPropertyNo), nameof(ToPropertyNo) }
            );
        }
        else if (!hasPropertyNo && !hasRange)
        {
            yield return new ValidationResult(
                "CommonDetails_PropertyNo_OrRangeRequired",
                new[] { nameof(PropertyNo), nameof(FromPropertyNo), nameof(ToPropertyNo) }
            );
        }
        else if (hasRange && hasFrom != hasTo)
        {
            yield return new ValidationResult(
                "CommonDetails_FromToPropertyNo_BothRequired",
                new[] { nameof(FromPropertyNo), nameof(ToPropertyNo) }
            );
        }
    }
}
