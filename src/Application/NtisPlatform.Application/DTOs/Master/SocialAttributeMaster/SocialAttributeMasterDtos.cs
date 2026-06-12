using System.ComponentModel.DataAnnotations;
namespace NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;

public class SocialAttributeDto : BaseDtos
{
    public string SocialAttributeCode { get; set; } = string.Empty;
    public string SocialAttributeName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public int? DisplayOrder { get; set; }
    public int? ParentAttributeId { get; set; }
    public bool IsRequiredWhenParentTrue { get; set; }
    public bool IsDiscountApplicable { get; set; }
    public int? PhotoTypeId { get; set; }
    public bool IsPhotoRequired { get; set; }
    public bool IsDocumentRequired { get; set; }
}

public class CreateSocialAttributeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "SocialAttribute_Code_Required")]
    [MinLength(3, ErrorMessage = "SocialAttribute_Code_MinLength")]
    [MaxLength(100, ErrorMessage = "SocialAttribute_Code_MaxLength")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "SocialAttribute_Code_InvalidFormat")]
    public string SocialAttributeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SocialAttribute_Name_Required")]
    [MinLength(2, ErrorMessage = "SocialAttribute_Name_MinLength")]
    [MaxLength(200, ErrorMessage = "SocialAttribute_Name_MaxLength")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\(\)&'\.]+$", ErrorMessage = "SocialAttribute_Name_InvalidFormat")]
    public string SocialAttributeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "SocialAttribute_DataType_Required")]
    [MaxLength(30, ErrorMessage = "SocialAttribute_DataType_MaxLength")]
    [RegularExpression(@"^(BIT|INT|DECIMAL|TEXT|DATE)$", ErrorMessage = "SocialAttribute_DataType_InvalidFormat")]
    public string DataType { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "SocialAttribute_Unit_MaxLength")]
    [RegularExpression(@"^[a-zA-Z0-9\s\/%\-\.]*$", ErrorMessage = "SocialAttribute_Unit_InvalidFormat")]
    public string? Unit { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SocialAttribute_DisplayOrder_Range")]
    public int? DisplayOrder { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SocialAttribute_ParentAttributeId_Range")]
    public int? ParentAttributeId { get; set; }

    public bool IsRequiredWhenParentTrue { get; set; } = false;
    public bool IsDiscountApplicable { get; set; } = false;

    [Range(1, int.MaxValue, ErrorMessage = "SocialAttribute_PhotoTypeId_Range")]
    public int? PhotoTypeId { get; set; }

    public bool IsPhotoRequired { get; set; } = false;
    public bool IsDocumentRequired { get; set; } = false;
}

public class UpdateSocialAttributeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "SocialAttribute_Code_Required")]
    [MinLength(3, ErrorMessage = "SocialAttribute_Code_MinLength")]
    [MaxLength(100, ErrorMessage = "SocialAttribute_Code_MaxLength")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "SocialAttribute_Code_InvalidFormat")]
    public string SocialAttributeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SocialAttribute_Name_Required")]
    [MinLength(2, ErrorMessage = "SocialAttribute_Name_MinLength")]
    [MaxLength(200, ErrorMessage = "SocialAttribute_Name_MaxLength")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\(\)&'\.]+$", ErrorMessage = "SocialAttribute_Name_InvalidFormat")]
    public string SocialAttributeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "SocialAttribute_DataType_Required")]
    [MaxLength(30, ErrorMessage = "SocialAttribute_DataType_MaxLength")]
    [RegularExpression(@"^(BIT|INT|DECIMAL|TEXT|DATE)$", ErrorMessage = "SocialAttribute_DataType_InvalidFormat")]
    public string DataType { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "SocialAttribute_Unit_MaxLength")]
    [RegularExpression(@"^[a-zA-Z0-9\s\/\-\.]*$", ErrorMessage = "SocialAttribute_Unit_InvalidFormat")]
    public string? Unit { get; set; }

    [Range(1, 10000, ErrorMessage = "SocialAttribute_DisplayOrder_Range")]
    public int? DisplayOrder { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SocialAttribute_ParentAttributeId_Range")]
    public int? ParentAttributeId { get; set; }

    public bool IsRequiredWhenParentTrue { get; set; }
    public bool IsDiscountApplicable { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SocialAttribute_PhotoTypeId_Range")]
    public int? PhotoTypeId { get; set; }

    public bool IsPhotoRequired { get; set; }
    public bool IsDocumentRequired { get; set; }
}
