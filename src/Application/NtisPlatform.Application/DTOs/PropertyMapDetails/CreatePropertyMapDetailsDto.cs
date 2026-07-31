using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMapDetails;

public class CreatePropertyMapDetailsDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMapping_PropertyId_Invalid")]
    public int? PropertyId { get; set; }
    public List<int?> PropertyOldId { get; set; } = new();

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMapping_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMapping_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyMapping_LocationLength_Invalid")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""'|\\?.~`]+$",
    ErrorMessage = "PropertyMapping_Location_Invalid")]
    public string? Location { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyMapping_RemarkLength_Invalid")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,]+$",
    ErrorMessage = "PropertyMapping_Remark_Invalid")]
    public string? Remark { get; set; }

    [Required(ErrorMessage = "PropertyMapping_Flag_Required")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,]+$",
    ErrorMessage = "PropertyMapping_Flag_Invalid")]
    public string Flag { get; set; } = string.Empty;
}
