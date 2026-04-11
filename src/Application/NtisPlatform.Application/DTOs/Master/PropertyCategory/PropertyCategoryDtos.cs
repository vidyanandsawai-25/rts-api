using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class PropertyCategoryDto : BaseDtos
{
    public int Id { get; set; }
    public string PropertyCategoryName { get; set; } = string.Empty;
}

public class PropertyCategoryCreateDto : CreateBaseDtos
{
    private string _propertyCategoryName = string.Empty;

    [Required(ErrorMessage = "PropertyCategoryName_Required")]
    [StringLength(50, ErrorMessage = "PropertyCategoryName_MaxLen_50")]
    public string PropertyCategoryName
    {
        get => _propertyCategoryName;
        set => _propertyCategoryName = string.IsNullOrWhiteSpace(value) 
            ? string.Empty 
            : value.Trim();
    }
}

public class PropertyCategoryUpdateDto : UpdateBaseDtos
{
    private string _propertyCategoryName = string.Empty;

    [Required(ErrorMessage = "PropertyCategoryName_Required")]
    [StringLength(50, ErrorMessage = "PropertyCategoryName_MaxLen_50")]
    public string PropertyCategoryName
    {
        get => _propertyCategoryName;
        set => _propertyCategoryName = string.IsNullOrWhiteSpace(value) 
            ? string.Empty 
            : value.Trim();
    }
}
