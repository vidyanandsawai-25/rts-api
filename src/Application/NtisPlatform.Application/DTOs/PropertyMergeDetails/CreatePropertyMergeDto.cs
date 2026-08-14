using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class CreatePropertyMergeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyMerge_PropertyOldId_Required")]
    [MinLength(1, ErrorMessage = "PropertyMerge_PropertyOldIds_MinLength")]
    public List<int> PropertyOldIds { get; set; } = new List<int>();

    [Required(ErrorMessage = "PropertyMerge_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyMerge_Location_MaxLen_500")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""|\\?.~`]+$", ErrorMessage = "PropertyMerge_Location_InvalidCharacters")]
    public string? Location { get; set; }
    public bool IsOldDataUpdate { get; set; } = true;
}
