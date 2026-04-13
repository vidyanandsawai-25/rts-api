using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class PropertyTypeCategoryDto : BaseDtos
{
    public int Id { get; set; }
    public string? PropertyTypeCategory { get; set; }
}

public class CreatePropertyTypeCategoryDto : CreateBaseDtos
{
    private string? _propertyTypeCategory;

    [Required(ErrorMessage = "PropertyTypeCategory_Required")]
    [StringLength(100, ErrorMessage = "PropertyTypeCategory_MaxLen_100")]
    public string? PropertyTypeCategory
    {
        get => _propertyTypeCategory;
        set => _propertyTypeCategory = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

public class UpdatePropertyTypeCategoryDto : UpdateBaseDtos
{
    private string? _propertyTypeCategory;

    [Required(ErrorMessage = "PropertyTypeCategory_Required")]
    [StringLength(100, ErrorMessage = "PropertyTypeCategory_MaxLen_100")]
    public string? PropertyTypeCategory
    {
        get => _propertyTypeCategory;
        set => _propertyTypeCategory = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
