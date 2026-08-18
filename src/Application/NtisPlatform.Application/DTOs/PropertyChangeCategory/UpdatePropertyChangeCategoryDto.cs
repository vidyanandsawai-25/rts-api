using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyChangeCategory;

public class UpdatePropertyChangeCategoryDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyChangeCategory_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyChangeCategory_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "PropertyChangeCategory_CategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyChangeCategory_CategoryId_Invalid")]
    public int CategoryId { get; set; }
}
