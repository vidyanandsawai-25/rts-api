using NtisPlatform.Core;
using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyPhotoType;

[LocalizableEntity(typeof(PropertyPhotoTypeEntity))]
public class PropertyPhotoTypeDto : BaseDtos
{
    public int Id { get; set; }
    public string PhotoTypeCode { get; set; } = string.Empty;
    public string PhotoTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}

public class CreatePropertyPhotoTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PhotoTypeCode_Required")]
    [StringLength(50, ErrorMessage = "PhotoTypeCode_MaxLen_50")]
    public string PhotoTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "PhotoTypeName_Required")]
    [StringLength(200, ErrorMessage = "PhotoTypeName_MaxLen_200")]
    public string PhotoTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description_MaxLen_500")]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
}

public class UpdatePropertyPhotoTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PhotoTypeCode_Required")]
    [StringLength(50, ErrorMessage = "PhotoTypeCode_MaxLen_50")]
    public string PhotoTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "PhotoTypeName_Required")]
    [StringLength(200, ErrorMessage = "PhotoTypeName_MaxLen_200")]
    public string PhotoTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description_MaxLen_500")]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
}
