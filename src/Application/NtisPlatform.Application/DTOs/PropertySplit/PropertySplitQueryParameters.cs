using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertySplit;

public class PropertySplitQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "PropertySplit_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertySplit_PropertyId_Invalid")]
    public int PropertyId { get; set; }
    public string WingName { get; set; } = string.Empty;
}
