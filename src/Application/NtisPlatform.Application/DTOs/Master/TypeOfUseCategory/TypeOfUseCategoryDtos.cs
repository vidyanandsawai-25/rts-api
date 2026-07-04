
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseCategoryDto : BaseDtos
{
    public int Id { get; set; }
    public string TypeOfUseCategoryCode { get; set; } = string.Empty;
    public string TypeOfUseCategoryName { get; set; } = string.Empty;
}

public class CreateTypeOfUseCategoryDto : CreateBaseDtos
{
    [Required(ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryCode_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryCode_MaxLen_50")]
    public string TypeOfUseCategoryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryName_Required")]
    [StringLength(100, ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryName_MaxLen_100")]
    public string TypeOfUseCategoryName { get; set; } = string.Empty;
}

public class UpdateTypeOfUseCategoryDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryCode_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryCode_MaxLen_50")]
    public string TypeOfUseCategoryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryName_Required")]
    [StringLength(100, ErrorMessage = "TypeOfUseCategory_TypeOfUseCategoryName_MaxLen_100")]
    public string TypeOfUseCategoryName { get; set; } = string.Empty;
}
