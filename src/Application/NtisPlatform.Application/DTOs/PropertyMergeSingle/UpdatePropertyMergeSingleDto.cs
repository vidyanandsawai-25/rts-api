using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeSingle;

public class UpdatePropertyMergeSingleDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyMergeSingle_PropertyOldId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMergeSingle_PropertyId_Invalid")]
    public int PropertyOldId { get; set; }

    [Required(ErrorMessage = "PropertyMergeSingle_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMergeSingle_PropertyId_Invalid")]
    public int PropertyId { get; set; }
    public bool IsPreviousDataUpdate { get; set; } = true;
}
