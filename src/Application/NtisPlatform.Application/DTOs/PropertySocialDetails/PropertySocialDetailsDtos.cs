using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertySocialDetails;

public class PropertySocialDetailsDto : BaseDtos
{
    public int PropertyId { get; set; }
    public int SocialAttributeId { get; set; }
    public bool? BitValue { get; set; }
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public string? TextValue { get; set; }
    public DateTime? DateValue { get; set; }
    public int? DocumentBindingId { get; set; }
    public string? Remark { get; set; }
    public string? SocialAttributeCode { get; set; }
    public string? SocialAttributeName { get; set; }

    public bool IsPhotoRequired { get; set; }
    public bool IsDocumentRequired { get; set; }
    public int? PhotoBindingId { get; set; }
    public Guid? PhotoGuid { get; set; }
    public Guid? DocumentGuid { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreatePropertySocialDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId_Range")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "PropertySocialDetails_SocialAttributeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertySocialDetails_SocialAttributeId_RangeMAX")]
    public int SocialAttributeId { get; set; }

    public bool? BitValue { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "PropertySocialDetails_IntValue_Range")]
    public int? IntValue { get; set; }

    [Range(0.0, double.MaxValue, ErrorMessage = "PropertySocialDetails_DecimalValue_RangeMAX")]
    public decimal? DecimalValue { get; set; }

    [MaxLength(500, ErrorMessage = "PropertySocialDetails_TextValue_MaxLen_500")]
    public string? TextValue { get; set; }

    public DateTime? DateValue { get; set; }

    public int? DocumentBindingId { get; set; }

    [MaxLength(500, ErrorMessage = "Remark_MaxLen_500")]
    public string? Remark { get; set; }
}

public class UpdatePropertySocialDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertySocialDetails_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertySocialDetails_PropertyId_Range")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "PropertySocialDetails_SocialAttributeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertySocialDetails_SocialAttributeId_Range")]
    public int SocialAttributeId { get; set; }

    public bool? BitValue { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "PropertySocialDetails_IntValue_Range")]
    public int? IntValue { get; set; }

    [Range(0.0, double.MaxValue, ErrorMessage = "PropertySocialDetails_DecimalValue_Range")]
    public decimal? DecimalValue { get; set; }

    [MaxLength(500, ErrorMessage = "PropertySocialDetails_TextValue_MaxLen_500")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\(\)&'\.\/,]*$", ErrorMessage = "PropertySocialDetails_TextValue_InvalidCharacters")]
    public string? TextValue { get; set; }

    public DateTime? DateValue { get; set; }

    public int? DocumentBindingId { get; set; }

    [MaxLength(500, ErrorMessage = "PropertySocialDetails_Remark_MaxLen_500")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\(\)&'\.\/,]*$", ErrorMessage = "PropertySocialDetails_Remark_InvalidCharacters")]
    public string? Remark { get; set; }
}

public class PropertySocialInfoItemDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "PropertySocialDetails_SocialAttributeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertySocialDetails_SocialAttributeId_Range")]
    public int SocialAttributeId { get; set; }

    public bool? BitValue { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "PropertySocialDetails_IntValue_Range")]
    public int? IntValue { get; set; }

    [Range(0.0, double.MaxValue, ErrorMessage = "PropertySocialDetails_DecimalValue_Range")]
    public decimal? DecimalValue { get; set; }

    [MaxLength(500, ErrorMessage = "PropertySocialDetails_TextValue_MaxLen_500")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\(\)&'\.\/,]*$", ErrorMessage = "PropertySocialDetails_TextValue_InvalidCharacters")]
    public string? TextValue { get; set; }

    public DateTime? DateValue { get; set; }

    public int? DocumentBindingId { get; set; }

    [MaxLength(500, ErrorMessage = "PropertySocialDetails_Remark_MaxLen_500")]
    [RegularExpression(@"^[^<>]*$", ErrorMessage = "PropertySocialDetails_Remark_InvalidCharacters")]
    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpsertPropertySocialInfoDto
{
    [Required(ErrorMessage = "PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId_Range")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "UpdatedBy_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UpdatedBy_Range")]
    public int UpdatedBy { get; set; }

    public List<PropertySocialInfoItemDto> SocialAttributes { get; set; } = new();

    public List<int> SocialAttributeIdsToRemove { get; set; } = new();
}

public class SocialAttributeHierarchyDto
{
    public int Id { get; set; }
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
    public bool IsActive { get; set; }

    // Current value from PropertySocialDetails
    public int? PropertySocialDetailId { get; set; }
    public bool? BitValue { get; set; }
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public string? TextValue { get; set; }
    public DateTime? DateValue { get; set; }
    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public int? PhotoBindingId { get; set; }
    public Guid? PhotoGuid { get; set; }
    public string? Remark { get; set; }

    // Child attributes
    public List<SocialAttributeHierarchyDto> Children { get; set; } = new();
}

public class PropertySocialInfoResponseDto
{
    public int PropertyId { get; set; }
    public List<SocialAttributeHierarchyDto> SocialAttributes { get; set; } = new();
}
