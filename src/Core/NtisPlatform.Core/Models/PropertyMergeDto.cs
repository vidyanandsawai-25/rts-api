using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

public class PropertyMergeDto
{
    [Required(ErrorMessage = "PropertyMerge_PropertyOldId_Required")]
    [MinLength(1, ErrorMessage = "PropertyMerge_PropertyOldIds_MinLength")]
    public List<int> PropertyOldIds { get; set; } = new List<int>();
    public int? PropertyMapId { get; set; }

    [Required(ErrorMessage = "PropertyMerge_PropertyId_Required")]
    [MinLength(1, ErrorMessage = "PropertyMerge_PropertyIds_MinLength")]
    public List<int> PropertyIds { get; set; } = new List<int>();

    [Required(ErrorMessage = "PropertyMerge_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_WardId_Range")]
    public int WardId { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyMerge_Location_MaxLen_500")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""|\\?.~`]+$", ErrorMessage = "PropertyMerge_Location_InvalidCharacters")]
    public string? Location { get; set; }

    [Required(ErrorMessage = "PropertyMerge_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_UserId_Range")]
    public int UserId { get; set; }
}

public class PropertyDemergeDto
{
    [Required(ErrorMessage = "PropertyDemerge_PropertyId_Required")]
    [MinLength(1, ErrorMessage = "PropertyDemerge_PropertyIds_MinLength")]
    public List<int> PropertyIds { get; set; } = new List<int>();

    [Required(ErrorMessage = "PropertyDemerge_PropertyOldId_Required")]
    [MinLength(1, ErrorMessage = "PropertyDemerge_PropertyOldIds_MinLength")]
    public List<int> PropertyOldIds { get; set; } = new List<int>();

    [Required(ErrorMessage = "PropertyDemerge_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyDemerge_UserId_Range")]
    public int UserId { get; set; }

    [RegularExpression("^(Old|New)$",ErrorMessage = "PropertyDemerge_PropertySide_Invalid")]
    public string? PropertySide { get; set; }
}

public class PropertyMergeDetailDto
{
    public int Id { get; set; }
    public int WardId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }

    // Old Property Details
    public int PropertyOldId { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldMobileNo { get; set; }
    public string? OldOccupierName { get; set; }
    public string? OldAddress { get; set; }
    public string? OldSocietyName { get; set; }
    public double? OldRV { get; set; }
    public double? OldTotalTax { get; set; }
    public double? OldPlotArea { get; set; }
    public double? OldGeneralTax { get; set; }
    public int? OldConstructionYear { get; set; }
    public double? OldConstructionArea { get; set; }
}

public class PropertyMergeDetailRequestDto
{
    /// <summary>
    /// List of property IDs to get merge details for
    /// </summary>
    public List<int> PropertyIds { get; set; } = new();
}

public class PropertyMergeDetailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<PropertyMergeDetailDto> Data { get; set; } = new();
}


public class PropertyMergeMultipleDto
{
    [Required(ErrorMessage = "PropertyMerge_PropertyIdList_Required")]
    [MinLength(1, ErrorMessage = "PropertyMerge_PropertyIdList_MinOne")]
    public List<PropertyMergeMultipleListDto> PropertyIdList { get; set; } = new();
    public int? PropertyMapId { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Latitude_Invalid")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "PropertyMerge_Longitude_Invalid")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "PropertyMerge_Location_MaxLen_500")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""|\\?.~`]+$", ErrorMessage = "PropertyMerge_Location_InvalidCharacters")]
    public string? Location { get; set; }

    [Required(ErrorMessage = "PropertyMerge_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_UserId_Range")]
    public int UserId { get; set; }
}

public class PropertyMergeMultipleListDto
{
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_PropertyOldId_Range")]
    public int PropertyOldId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PropertyMerge_PropertyId_Range")]
    public int PropertyId { get; set; }
}

public class PropertyDemergeMultipleDto
{
    [Required(ErrorMessage = "PropertyDemerge_PropertyIdList_Required")]
    [MinLength(1, ErrorMessage = "PropertyDemerge_PropertyIdList_MinOne")]
    public List<PropertyMergeMultipleListDto> PropertyIdList { get; set; } = new();

    [Required(ErrorMessage = "PropertyDemerge_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyDemerge_UserId_Range")]
    public int UserId { get; set; }
}

public sealed class PropertyDemergePair
{
    public int PropertyOldId { get; init; }
    public int PropertyId { get; init; }
}

public sealed class PropertyMappingSelection
{
    public int Id { get; init; }

    public int PropertyMapId { get; init; }

    public string? PropertySide { get; init; }

    public int? PropertyIdOld { get; init; }

    public int? PropertyIdNew { get; init; }

    public string? PropertyNo { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
        
}






