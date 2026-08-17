using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeSingle;

public class PropertyMergeSingleQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "PropertyMergeSingle_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMergeSingle_PropertyId_Invalid")]
    public int PropertyId { get; set; }
    public string WingName { get; set; } = string.Empty;
}
