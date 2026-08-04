using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class PropertyMergeMultipleDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyMerge_PropertyIdList_Required")]
    [MinLength(1, ErrorMessage = "PropertyMerge_PropertyIdList_MinOne")]
    public List<PropertyMergeMultipleListDto> PropertyIdList { get; set; } = new();

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyMerge_Location_MaxLen_500")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""|\\?.~`]+$", ErrorMessage = "PropertyMerge_Location_InvalidCharacters")]
    public string? Location { get; set; }
}
public class PropertyMergeMultipleListDto
{
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_PropertyOldId_Range")]
    public int PropertyOldId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_PropertyId_Range")]
    public int PropertyId { get; set; }
}

public class PropertyDemergeMultipleDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyDemerge_PropertyIdList_Required")]
    [MinLength(1, ErrorMessage = "PropertyDemerge_PropertyIdList_MinOne")]
    public List<PropertyMergeMultipleListDto> PropertyIdList { get; set; } = new();
}
