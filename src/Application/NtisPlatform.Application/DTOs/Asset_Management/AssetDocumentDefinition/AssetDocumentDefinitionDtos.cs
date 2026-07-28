using System.ComponentModel.DataAnnotations;
using NtisPlatform.Core;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.DTOs.Asset_Management;

[LocalizableEntity(typeof(AssetDocumentDefinitionEntity))]
public class AssetDocumentDefinitionDto : BaseDtos
{
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public string? AssetCategoryName { get; set; }
    public string? AssetTypeName { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int? DisplayOrder { get; set; }
}

public class CreateAssetDocumentDefinitionDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetCategoryId_InvalidRange")]
    public int? AssetCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetTypeId_InvalidRange")]
    public int? AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentCode_Required")]
    [StringLength(50, ErrorMessage = "AssetDocumentDefinition_DocumentCode_MaxLengthExceeded_50")]
    public string DocumentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentName_Required")]
    [StringLength(200, ErrorMessage = "AssetDocumentDefinition_DocumentName_MaxLengthExceeded_200")]
    public string DocumentName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetDocumentDefinition_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_DisplayOrder_InvalidRange")]
    public int? DisplayOrder { get; set; }
}

public class UpdateAssetDocumentDefinitionDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetCategoryId_InvalidRange")]
    public int? AssetCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetTypeId_InvalidRange")]
    public int? AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentCode_Required")]
    [StringLength(50, ErrorMessage = "AssetDocumentDefinition_DocumentCode_MaxLengthExceeded_50")]
    public string DocumentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentName_Required")]
    [StringLength(200, ErrorMessage = "AssetDocumentDefinition_DocumentName_MaxLengthExceeded_200")]
    public string DocumentName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetDocumentDefinition_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_DisplayOrder_InvalidRange")]
    public int? DisplayOrder { get; set; }
}
