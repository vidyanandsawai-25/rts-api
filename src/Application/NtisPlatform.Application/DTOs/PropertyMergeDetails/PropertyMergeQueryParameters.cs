using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class PropertyMergeQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "PropertyMerge_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_PropertyId_Invalid")]
    public int PropertyId { get; set; }
}
