using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;

/// <summary>
/// Query parameters for filtering and sorting PropertyWorkflowStageMaster
/// </summary>
public class PropertyWorkflowStageMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by stage name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? StageName { get; set; }

    /// <summary>
    /// Filter by display order
    /// </summary>
    [Filterable]
    [Sortable]
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }
}
