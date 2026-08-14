using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertySplit;

public class UpdatePropertySplitDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertySplit_PropertyOldId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertySplit_PropertyOldId_MinLength")]
    public int PropertyOldId { get; set; }

    [Required(ErrorMessage = "PropertySplit_PropertyId_Required")]
    [MinLength(1, ErrorMessage = "PropertySplit_PropertyIds_MinLength")]
    public List<int> PropertyIds { get; set; } = new List<int>();
    public bool IsPreviousDataUpdate { get; set; } = true;
}
