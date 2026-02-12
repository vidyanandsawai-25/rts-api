using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.ScreenGroupMaster;

/// <summary>
/// Query parameters for filtering and sorting ScreenGroupMaster
/// </summary>
public class ScreenGroupMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by screen group code
    /// </summary>
    [Filterable]
    [Sortable]
    public string? ScreenGroupCode { get; set; }

    /// <summary>
    /// Search in screen group name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ScreenGroupName { get; set; }

    /// <summary>
    /// Search in screen group local name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ScreenGroupNameLocal { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by display order
    /// </summary>
    [Filterable]
    [Sortable]
    public int? DisplayOrder { get; set; }
}
