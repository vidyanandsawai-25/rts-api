using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;

public class PropertyDescriptionAndTypeOfUseValidationDto : BaseDtos
{
    public int Id { get; set; }
    public int PropertyTypeId { get; set; }
    public int TypeOfUseId { get; set; }
}

public class CreatePropertyDescriptionAndTypeOfUseValidationDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyTypeId_MustBeGreaterThanZero")]
    public int PropertyTypeId { get; set; }

    [Required(ErrorMessage = "TypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "TypeOfUseId_MustBeGreaterThanZero")]
    public int TypeOfUseId { get; set; }
}

public class UpdatePropertyDescriptionAndTypeOfUseValidationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyTypeId_MustBeGreaterThanZero")]
    public int PropertyTypeId { get; set; }

    [Required(ErrorMessage = "TypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "TypeOfUseId_MustBeGreaterThanZero")]
    public int TypeOfUseId { get; set; }
}
