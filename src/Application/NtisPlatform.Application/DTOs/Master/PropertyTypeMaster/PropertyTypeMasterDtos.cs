using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;

public class PropertyTypeMasterDto : BaseDtos
{
    public int Id { get; set; }
    public string PropertyDescription { get; set; } = string.Empty;
    public string? Type { get; set; } = string.Empty;
    public string? PropertyTypeGroup { get; set; } = string.Empty;
    public int? SearchSequence { get; set; }
    public int? PropertyTypeCategoryId { get; set; }
}

public class CreatePropertyTypeMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyDescription_Required")]
    [StringLength(100, ErrorMessage = "PropertyDescription_MaxLen_100")]
    public string PropertyDescription { get; set; } = string.Empty;

    [StringLength(5, ErrorMessage = "Type_MaxLen_5")]
    public string? Type { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PropertyTypeGroup_MaxLen_50")]
    public string? PropertyTypeGroup { get; set; } = string.Empty;

    public int? SearchSequence { get; set; }

    public int? PropertyTypeCategoryId { get; set; }
}

public class UpdatePropertyTypeMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyDescription_Required")]
    [StringLength(100, ErrorMessage = "PropertyDescription_MaxLen_100")]
    public string PropertyDescription { get; set; } = string.Empty;

    [StringLength(5, ErrorMessage = "Type_MaxLen_5")]
    public string? Type { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PropertyTypeGroup_MaxLen_50")]
    public string? PropertyTypeGroup { get; set; } = string.Empty;

    public int? SearchSequence { get; set; }

    public int? PropertyTypeCategoryId { get; set; }
}
