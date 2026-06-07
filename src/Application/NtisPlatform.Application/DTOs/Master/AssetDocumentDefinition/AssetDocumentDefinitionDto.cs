using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetDocumentDefinitionDto : BaseDtos
{
    public int AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int MaxFileSizeMB { get; set; }
    public string AllowedExtensions { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class CreateAssetDocumentDefinitionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetDocumentDefinition_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetTypeId_InvalidRange")]
    public int? AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentCode_Required")]
    [StringLength(50, ErrorMessage = "AssetDocumentDefinition_DocumentCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_DocumentCode_Invalid")]
    public string DocumentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentName_Required")]
    [StringLength(200, ErrorMessage = "AssetDocumentDefinition_DocumentName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_DocumentName_Invalid")]
    public string DocumentName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetDocumentDefinition_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_Description_Invalid")]
    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    [Required(ErrorMessage = "AssetDocumentDefinition_MaxFileSizeMB_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_MaxFileSizeMB_InvalidRange")]
    public int MaxFileSizeMB { get; set; } = 10;

    [Required(ErrorMessage = "AssetDocumentDefinition_AllowedExtensions_Required")]
    [StringLength(200, ErrorMessage = "AssetDocumentDefinition_AllowedExtensions_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_AllowedExtensions_Invalid")]
    public string AllowedExtensions { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetDocumentDefinition_DisplayOrder_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_DisplayOrder_InvalidRange")]
    public int DisplayOrder { get; set; }
}

public class UpdateAssetDocumentDefinitionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetDocumentDefinition_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_AssetTypeId_InvalidRange")]
    public int? AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentCode_Required")]
    [StringLength(50, ErrorMessage = "AssetDocumentDefinition_DocumentCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_DocumentCode_Invalid")]
    public string DocumentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetDocumentDefinition_DocumentName_Required")]
    [StringLength(200, ErrorMessage = "AssetDocumentDefinition_DocumentName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_DocumentName_Invalid")]
    public string DocumentName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetDocumentDefinition_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_Description_Invalid")]
    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    [Required(ErrorMessage = "AssetDocumentDefinition_MaxFileSizeMB_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_MaxFileSizeMB_InvalidRange")]
    public int MaxFileSizeMB { get; set; }

    [Required(ErrorMessage = "AssetDocumentDefinition_AllowedExtensions_Required")]
    [StringLength(200, ErrorMessage = "AssetDocumentDefinition_AllowedExtensions_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetDocumentDefinition_AllowedExtensions_Invalid")]
    public string AllowedExtensions { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetDocumentDefinition_DisplayOrder_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetDocumentDefinition_DisplayOrder_InvalidRange")]
    public int DisplayOrder { get; set; }
}
