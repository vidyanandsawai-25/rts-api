using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetPhotoTypeDto : BaseDtos
{
    public string PhotoTypeCode { get; set; } = string.Empty;
    public string PhotoTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public int? AssetCategoryId { get; set; }
    public string? AssetCategoryName { get; set; }
    public int? AssetTypeId { get; set; }
    public string? AssetTypeName { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSubUnit { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetPhotoTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetPhotoType_PhotoTypeCode_Required")]
    [StringLength(50, ErrorMessage = "AssetPhotoType_PhotoTypeCode_MaxLength")]
    public string PhotoTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetPhotoType_PhotoTypeName_Required")]
    [StringLength(100, ErrorMessage = "AssetPhotoType_PhotoTypeName_MaxLength")]
    public string PhotoTypeName { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "AssetPhotoType_Description_MaxLength")]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsSubUnit { get; set; } = false;
}

public class UpdateAssetPhotoTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetPhotoType_PhotoTypeCode_Required")]
    [StringLength(50, ErrorMessage = "AssetPhotoType_PhotoTypeCode_MaxLength")]
    public string PhotoTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetPhotoType_PhotoTypeName_Required")]
    [StringLength(100, ErrorMessage = "AssetPhotoType_PhotoTypeName_MaxLength")]
    public string PhotoTypeName { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "AssetPhotoType_Description_MaxLength")]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsSubUnit { get; set; } = false;
}
