using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class UpdatePropertyMergeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyMerge_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "PropertyMerge_PropertyOldId_Required")]
    [MinLength(1, ErrorMessage = "PropertyMerge_PropertyOldIds_MinLength")]
    public List<int> PropertyOldIds { get; set; } = new List<int>();
    public bool IsPreviousDataUpdate { get; set; } = true;
}
