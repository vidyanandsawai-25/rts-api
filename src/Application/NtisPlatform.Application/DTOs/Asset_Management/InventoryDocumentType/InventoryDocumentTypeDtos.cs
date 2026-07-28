using System;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class InventoryDocumentTypeDto : BaseDtos
{
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateInventoryDocumentTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "InventoryDocumentType_DocumentTypeCode_Required")]
    [StringLength(50, ErrorMessage = "InventoryDocumentType_DocumentTypeCode_MaxLen_50")]
    public string DocumentTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryDocumentType_DocumentTypeName_Required")]
    [StringLength(200, ErrorMessage = "InventoryDocumentType_DocumentTypeName_MaxLen_200")]
    public string DocumentTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "InventoryDocumentType_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "InventoryDocumentType_DisplayOrder_CannotBeNegative")]
    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "InventoryDocumentType_IsRequired_Flag_Required")]
    public bool? IsRequired { get; set; }
}

public class UpdateInventoryDocumentTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "InventoryDocumentType_DocumentTypeCode_Required")]
    [StringLength(50, ErrorMessage = "InventoryDocumentType_DocumentTypeCode_MaxLen_50")]
    public string DocumentTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryDocumentType_DocumentTypeName_Required")]
    [StringLength(200, ErrorMessage = "InventoryDocumentType_DocumentTypeName_MaxLen_200")]
    public string DocumentTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "InventoryDocumentType_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "InventoryDocumentType_DisplayOrder_CannotBeNegative")]
    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "InventoryDocumentType_IsRequired_Flag_Required")]
    public bool? IsRequired { get; set; }
}
