using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMapDetails;

public class UpdatePropertyMapDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyMapping_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMapping_PropertyId_Invalid")]
    public int PropertyId { get; set; }
    public List<SocietyDetails> SocietyDetails { get; set; } = new List<SocietyDetails>();
}

public class SocietyDetails
{
    [Required(ErrorMessage = "PropertyMapping_OldSocietyName_Required")]
    [MaxLength(300, ErrorMessage = "PropertyMapping_OldSocietyName_Invalid")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""'|\\?.~`]+$",
    ErrorMessage = "PropertyMapping_OldSocietyName_Invalid")]
    public string OldSocietyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "PropertyMapping_OldWardNo_Required")]
    [MaxLength(20, ErrorMessage = "PropertyMapping_OldWardNoLength_Invalid")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,]+$",
    ErrorMessage = "PropertyMapping_OldWardNo_Invalid")]
    public string OldWardNo { get; set; } = string.Empty;
}
