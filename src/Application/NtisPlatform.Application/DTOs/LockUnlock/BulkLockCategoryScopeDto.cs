using NtisPlatform.Core.Enums;

namespace NtisPlatform.Application.DTOs.LockUnlock;

/// <summary>
/// Bulk lock/unlock scope selector - the same SearchCategory concept as PropertySearchByCategory
/// (1=ZoneWise, 2=WardWise, 3=BuildingWise, 4=FromToProperty), but intentionally limited to just
/// the scoping fields. Pagination and the extra grid filters (PartType/PropertyCategoryName/
/// PropertyAssessmentStatusId/IsWing/SearchTerm) don't apply to a bulk action over every matching
/// property, so they're not exposed here.
/// </summary>
public class BulkLockCategoryScopeDto
{
    /// <summary>
    /// Selects which of the fields below are required. 1=ZoneWise, 2=WardWise, 3=BuildingWise, 4=FromToProperty.
    /// </summary>
    public PropertySearchCategory SearchCategory { get; set; }

    /// <summary>
    /// Required when SearchCategory is ZoneWise.
    /// </summary>
    public int? ZoneId { get; set; }

    /// <summary>
    /// Required when SearchCategory is WardWise, BuildingWise, or FromToProperty.
    /// </summary>
    public int? WardId { get; set; }

    /// <summary>
    /// Required when SearchCategory is BuildingWise.
    /// </summary>
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Optional refinement for BuildingWise.
    /// </summary>
    public string? PartitionNo { get; set; }

    /// <summary>
    /// Required when SearchCategory is FromToProperty. Format: "{PropertyNo}" or "{PropertyNo}-{PartitionNo}" (e.g. "1-A9").
    /// </summary>
    public string? PropertyFrom { get; set; }

    /// <summary>
    /// Optional upper bound for FromToProperty. When omitted, the range is open-ended from PropertyFrom onward.
    /// Format: "{PropertyNo}" or "{PropertyNo}-{PartitionNo}" (e.g. "1-S2").
    /// </summary>
    public string? PropertyTo { get; set; }
}
