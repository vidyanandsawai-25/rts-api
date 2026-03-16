using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.ConfigCategoryMaster;

/// <summary>
/// Query parameters for filtering and sorting ConfigCategoryMaster
/// </summary>
public class ConfigCategoryMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by category code
    /// </summary>
    [Filterable]
    [Sortable]
     [Searchable]
    public string? CategoryCode { get; set; }

    /// <summary>
    /// Filter by category name
    /// </summary>
    [Filterable]
    [Sortable]
    [Searchable]
    public string? CategoryName { get; set; }

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
