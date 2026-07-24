using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetTypeOfUseDto : BaseDtos
{
    public int AssetCategoryId { get; set; }
    public string AssetCategoryName { get; set; } = string.Empty;
    public int AssetTypeId { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;
    public string TypeOfUseCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int? TypeOfUseGroupId { get; set; }
    public string? TypeOfUseGroupName { get; set; }
    public int? SearchSequence { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetTypeOfUseDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetTypeOfUse_AssetCategoryId_Required")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetTypeOfUse_AssetTypeId_Required")]
    public int AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetTypeOfUse_TypeOfUseCode_Required")]
    [StringLength(10, ErrorMessage = "AssetTypeOfUse_TypeOfUseCode_MaxLength")]
    public string TypeOfUseCode { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "AssetTypeOfUse_Description_MaxLength")]
    public string? Description { get; set; }

    [StringLength(5, ErrorMessage = "AssetTypeOfUse_Type_MaxLength")]
    public string? Type { get; set; }

    public int? TypeOfUseGroupId { get; set; }
    public int? SearchSequence { get; set; }
}

public class UpdateAssetTypeOfUseDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetTypeOfUse_AssetCategoryId_Required")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetTypeOfUse_AssetTypeId_Required")]
    public int AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetTypeOfUse_TypeOfUseCode_Required")]
    [StringLength(10, ErrorMessage = "AssetTypeOfUse_TypeOfUseCode_MaxLength")]
    public string TypeOfUseCode { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "AssetTypeOfUse_Description_MaxLength")]
    public string? Description { get; set; }

    [StringLength(5, ErrorMessage = "AssetTypeOfUse_Type_MaxLength")]
    public string? Type { get; set; }

    public int? TypeOfUseGroupId { get; set; }
    public int? SearchSequence { get; set; }
}
