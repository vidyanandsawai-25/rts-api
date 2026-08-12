using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeSingle;

public class CreatePropertyMergeSingleDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyMergeSingle_PropertyOldId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMergeSingle_PropertyId_Invalid")]
    public int PropertyOldId { get; set; } 

    [Required(ErrorMessage = "PropertyMergeSingle_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMergeSingle_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMergeSingle_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMergeSingle_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyMergeSingle_Location_MaxLen_500")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""|\\?.~`]+$", ErrorMessage = "PropertyMergeSingle_Location_InvalidCharacters")]
    public string? Location { get; set; }
    public bool IsOldDataUpdate { get; set; } = true;
}
