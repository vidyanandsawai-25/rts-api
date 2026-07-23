using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.DataEntrySameAs;

/// <summary>
/// Lookup parameters for the assessable property units under a building (same Ward + PropertyNo).
/// The query always excludes amenity part-types, building/wing-level rows, and inactive/deleted
/// properties; the optional fields below narrow the result further.
/// </summary>
public class DataEntrySameAsUnitsQueryParameters
{
    [Required]
    public int WardId { get; set; }

    [Required]
    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>Optional. When supplied, only this partition is returned.</summary>
    public string? PartitionNo { get; set; }

    /// <summary>Optional exact filter on PropertyTypeMaster.PartType (applied on top of the != Amenity rule).</summary>
    public string? PartType { get; set; }

    /// <summary>Optional exact filter on PropertyCategoryMaster.PropertyCategoryName.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Optional exact filter on PropertyMast.Type.</summary>
    public string? Type { get; set; }

    /// <summary>Optional free-text search matched (Contains) across PartType, PropertyCategoryName and Type.</summary>
    public string? SearchTerm { get; set; }
}
