using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class PropertyMergeQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "UnMergeProperty_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UnMergeProperty_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "UnMergeProperty_PropertyType_Required")]
    [RegularExpression("^(Old|New)$", ErrorMessage = "UnMergePropertyd_PropertyType_Invalid")]
    public string PropertyType { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
}
