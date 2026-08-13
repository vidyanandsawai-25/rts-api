using NtisPlatform.Core.Enums;

namespace NtisPlatform.Core.Models;

/// <summary>
/// Request DTO for <c>PropertySearchByCategory</c> - a scoped property search selected by
/// <see cref="PropertySearchCategory"/> (Zone-wise, Ward-wise, Building-wise, or a From/To
/// property-number range within a ward).
/// </summary>
public class PropertySearchByCategoryRequestDto
{
    public PropertySearchCategory SearchCategory { get; set; }

    /// <summary>
    /// Required when <see cref="SearchCategory"/> is <see cref="PropertySearchCategory.ZoneWise"/>.
    /// </summary>
    public int? ZoneId { get; set; }

    /// <summary>
    /// Required when <see cref="SearchCategory"/> is <see cref="PropertySearchCategory.WardWise"/>,
    /// <see cref="PropertySearchCategory.BuildingWise"/>, or <see cref="PropertySearchCategory.FromToProperty"/>.
    /// </summary>
    public int? WardId { get; set; }

    /// <summary>
    /// Required when <see cref="SearchCategory"/> is <see cref="PropertySearchCategory.BuildingWise"/>.
    /// </summary>
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Optional refinement for <see cref="PropertySearchCategory.BuildingWise"/>.
    /// </summary>
    public string? PartitionNo { get; set; }

    /// <summary>
    /// Required when <see cref="SearchCategory"/> is <see cref="PropertySearchCategory.FromToProperty"/>.
    /// Format: "{PropertyNo}" or "{PropertyNo}-{PartitionNo}" (e.g. "1" or "1-A9").
    /// </summary>
    public string? PropertyFrom { get; set; }

    /// <summary>
    /// Optional upper bound for <see cref="PropertySearchCategory.FromToProperty"/>. When omitted,
    /// the range is open-ended from <see cref="PropertyFrom"/> onward.
    /// Format: "{PropertyNo}" or "{PropertyNo}-{PartitionNo}" (e.g. "1-S2").
    /// </summary>
    public string? PropertyTo { get; set; }

    /// <summary>
    /// Optional filter by PropertyTypeMaster.PartType. Comma-separated for multiple values
    /// (e.g. "R,C,Amenity"). Applies in addition to the SearchCategory scope.
    /// </summary>
    public string? PartType { get; set; }

    /// <summary>
    /// Optional filter by PropertyCategoryMaster.PropertyCategoryName. Comma-separated for
    /// multiple values (e.g. "Apartment,Individual"). Applies in addition to the SearchCategory scope.
    /// </summary>
    public string? PropertyCategoryName { get; set; }

    /// <summary>
    /// Optional filter by PropertyMast.PropertyAssessmentStatusId. Comma-separated for multiple
    /// values (e.g. "1,2"). Applies in addition to the SearchCategory scope.
    /// </summary>
    public string? PropertyAssessmentStatusId { get; set; }

    /// <summary>
    /// Optional filter by the computed IsWing flag (whether PartitionNo matches a PTIS.WingMaster.WingNo).
    /// Applies in addition to the SearchCategory scope.
    /// </summary>
    public bool? IsWing { get; set; }

    /// <summary>
    /// Optional free-text filter matched against WardNo, PropertyNo, PartitionNo, and the
    /// composed Property code (any match). Applies in addition to the SearchCategory scope.
    /// </summary>
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Response DTO for <c>PropertySearchByCategory</c> results.
/// </summary>
public class PropertySearchByCategoryResponseDto
{
    public int PropertyId { get; set; }
    public int TaxZoneId { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string PartitionNo { get; set; } = string.Empty;

    /// <summary>
    /// Composed property code: "{WardNo}-{PropertyNo}" or, when PartitionNo is set,
    /// "{WardNo}-{PropertyNo}-{PartitionNo}".
    /// </summary>
    public string? Property { get; set; }
    public string? MobileNo { get; set; }
    public string? UPICId { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? PartType { get; set; }
    public int? CategoryId { get; set; }
    public string? PropertyCategoryName { get; set; }
    public bool IsWing { get; set; }
    public int PropertyAssessmentStatusId { get; set; }
}
