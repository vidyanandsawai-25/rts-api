using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class UpdatePropertyMergeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyDemerge_PropertyId_Required")]
    [MinLength(1, ErrorMessage = "PropertyDemerge_PropertyIds_MinLength")]
    public List<int> PropertyIds { get; set; } = new List<int>();

    [Required(ErrorMessage = "PropertyDemerge_PropertyOldId_Required")]
    [MinLength(1, ErrorMessage = "PropertyDemerge_PropertyOldIds_MinLength")]
    public List<int> PropertyOldIds { get; set; } = new List<int>();

    [RegularExpression("^(Old|New)$", ErrorMessage = "PropertyDemerge_PropertySide_Invalid")]
    public string? PropertySide { get; set; }
}
