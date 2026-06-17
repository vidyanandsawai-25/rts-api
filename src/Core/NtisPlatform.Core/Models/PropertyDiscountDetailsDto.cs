using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Discount Information Tab - includes all social attributes where IsDiscountApplicable=1
/// Used for the GET /{propertyId}/discount-details API endpoint
/// </summary>
public class PropertyDiscountInfoResponseDto
{
    public int PropertyId { get; set; }
    public List<DiscountAttributeDto> DiscountAttributes { get; set; } = new();
}

/// <summary>
/// Represents a single discount-applicable social attribute with its current value for a property
/// </summary>
public class DiscountAttributeDto
{
    /// <summary>
    /// The ID of the SocialAttribute master record
    /// </summary>
    public int Id { get; set; }
    public string SocialAttributeCode { get; set; } = string.Empty;
    public string SocialAttributeName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsDiscountApplicable { get; set; }

    // Current value from PropertySocialDetails
    public int? PropertySocialDetailId { get; set; }
    public bool? BitValue { get; set; }
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public string? TextValue { get; set; }
    public DateTime? DateValue { get; set; }

    public bool IsPhotoRequired { get; set; }
    public bool IsDocumentRequired { get; set; }

    /// <summary>
    /// Document GUID for viewing/downloading. Only present if a valid, active document exists.
    /// Use with DocumentController: GET /api/documents/{documentGuid}/view
    /// </summary>
    public Guid? DocumentGuid { get; set; }

    public int? DocumentBindingId { get; set; }

    public int? PhotoBindingId { get; set; }
    public Guid? PhotoGuid { get; set; }

    public string? Remark { get; set; }

    /// <summary>
    /// Whether this discount attribute is currently active/enabled for this property.
    /// true = toggle ON, false = toggle OFF (no saved record yet or soft-deleted).
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for updating discount information for a property
/// Used for the PUT /{propertyId}/discount-details API endpoint
/// </summary>
public class UpsertPropertyDiscountInfoDto
{
    [Required(ErrorMessage = "PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId_Range")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "UpdatedBy_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "UpdatedBy_Range")]
    public int UpdatedBy { get; set; }

    public List<DiscountAttributeItemDto> DiscountAttributes { get; set; } = new();
}

/// <summary>
/// Represents a single discount attribute value to be saved
/// </summary>
public class DiscountAttributeItemDto
{
    /// <summary>
    /// The ID of the existing PropertySocialDetails record (for updates). Null for new records.
    /// </summary>
    public int? PropertySocialDetailId { get; set; }

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

    public bool IsActive { get; set; } = true;
}
