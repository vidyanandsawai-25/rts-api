using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.DesignationMaster;

/// <summary>
/// Query parameters for filtering and sorting DesignationMaster
/// </summary>
public class DesignationMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by designation code
    /// </summary>
    [Filterable]
    [Sortable]
    public string? DesignationCode { get; set; }

    /// <summary>
    /// Search in designation name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? DesignationName { get; set; }

    /// <summary>
    /// Search in designation local name
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? DesignationLocal { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }
}
