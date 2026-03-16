using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.ConfigKeyMaster;

/// <summary>
/// Query parameters for filtering and sorting ConfigKeyMaster
/// </summary>
public class ConfigKeyMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by category ID
    /// </summary>
    [Filterable]
    [Sortable]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Filter by config code
    /// </summary>
    [Filterable]
    [Sortable]
    [Searchable]
    public string? ConfigCode { get; set; }

    /// <summary>
    /// Filter by config name
    /// </summary>
    [Filterable]
    [Sortable]
    [Searchable]
    public string? ConfigName { get; set; }

    /// <summary>
    /// Filter by data type
    /// </summary>
    [Filterable]
    public string? DataType { get; set; }

    /// <summary>
    /// Filter by control type
    /// </summary>
    [Filterable]
    public string? ControlType { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }
}
