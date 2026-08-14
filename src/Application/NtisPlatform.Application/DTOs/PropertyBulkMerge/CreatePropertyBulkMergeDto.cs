using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyBulkMerge;

public class CreatePropertyBulkMergeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyBulkMerge_PropertyIdList_Required")]
    [MinLength(1, ErrorMessage = "PropertyBulkMerge_PropertyIdList_MinOne")]
    public List<PropertyBulkMergeDetailsDto> PropertyIdList { get; set; } = new();

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyBulkMerge_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyBulkMerge_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyBulkMerge_Location_MaxLen_500")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""|\\?.~`]+$", ErrorMessage = "PropertyBulkMerge_Location_InvalidCharacters")]
    public string? Location { get; set; }
    public bool IsOldDataUpdate { get; set; } = true;
}

public class PropertyBulkMergeDetailsDto
{
    [Range(1, int.MaxValue, ErrorMessage = "PropertyBulkMerge_PropertyOldId_Range")]
    public int PropertyOldId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PropertyBulkMerge_PropertyId_Range")]
    public int PropertyId { get; set; }
}
