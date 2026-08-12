using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertySplit;

public class CreatePropertySplitDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertySplit_PropertyOldId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertySplit_PropertyOldId_MinLength")]
    public int PropertyOldId { get; set; }

    [Required(ErrorMessage = "PropertySplit_PropertyId_Required")]
    [MinLength(1, ErrorMessage = "PropertySplit_PropertyIds_MinLength")]
    public List<int> PropertyIds { get; set; } = new List<int>();

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertySplit_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertySplit_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertySplit_Location_MaxLen_500")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""|\\?.~`]+$", ErrorMessage = "PropertySplit_Location_InvalidCharacters")]
    public string? Location { get; set; }
    public bool IsOldDataUpdate { get; set; } = true;
}
