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
    public string? Remark { get; set; }
    public string? SocialAttributeCode { get; set; }
    public string? SocialAttributeName { get; set; }
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

    [MaxLength(1000, ErrorMessage = "Remark_MaxLen_1000")]
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

    [MaxLength(1000, ErrorMessage = "PropertySocialDetails_Remark_MaxLen_1000")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\(\)&'\.\/,]*$", ErrorMessage = "PropertySocialDetails_Remark_InvalidCharacters")]
    public string? Remark { get; set; }
}
