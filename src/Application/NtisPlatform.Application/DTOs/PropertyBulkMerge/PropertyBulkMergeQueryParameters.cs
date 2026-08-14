using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyBulkMerge;

public class PropertyBulkMergeQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "PropertyBulkMerge_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyBulkMerge_PropertyId_Invalid")]
    public int PropertyId { get; set; }
}
