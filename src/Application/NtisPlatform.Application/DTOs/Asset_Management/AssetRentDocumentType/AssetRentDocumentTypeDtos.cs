using System;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetRentDocumentTypeDto : BaseDtos
{
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetRentDocumentTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetRentDocumentType_DocumentTypeCode_Required")]
    [StringLength(50, ErrorMessage = "AssetRentDocumentType_DocumentTypeCode_MaxLen_50")]
    public string DocumentTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetRentDocumentType_DocumentTypeName_Required")]
    [StringLength(200, ErrorMessage = "AssetRentDocumentType_DocumentTypeName_MaxLen_200")]
    public string DocumentTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetRentDocumentType_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetRentDocumentType_DisplayOrder_CannotBeNegative")]
    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "AssetRentDocumentType_IsRequired_Flag_Required")]
    public bool? IsRequired { get; set; }
}

public class UpdateAssetRentDocumentTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetRentDocumentType_DocumentTypeCode_Required")]
    [StringLength(50, ErrorMessage = "AssetRentDocumentType_DocumentTypeCode_MaxLen_50")]
    public string DocumentTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetRentDocumentType_DocumentTypeName_Required")]
    [StringLength(200, ErrorMessage = "AssetRentDocumentType_DocumentTypeName_MaxLen_200")]
    public string DocumentTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetRentDocumentType_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetRentDocumentType_DisplayOrder_CannotBeNegative")]
    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "AssetRentDocumentType_IsRequired_Flag_Required")]
    public bool? IsRequired { get; set; }
}
