using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.DepartmentMaster;

/// <summary>
/// Query parameters for filtering and sorting DepartmentMaster
/// </summary>
public class DepartmentMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by department code
    /// </summary>
    [Filterable]
    [Sortable]
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Search in department name (English)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Search in department name (Local)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? DepartmentNameLocal { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }
}
