using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.ModuleMaster;

/// <summary>
/// Query parameters for filtering and sorting ModuleMaster
/// </summary>
public class ModuleMasterQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by department
    /// </summary>
    [Filterable]
    [Sortable]
    public int? DepartmentMasterId { get; set; }

    /// <summary>
    /// Filter by module code
    /// </summary>
    [Filterable]
    [Sortable]
    public string? ModuleCode { get; set; }

    /// <summary>
    /// Search in module name (English)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ModuleName { get; set; }

    /// <summary>
    /// Search in module name (Local)
    /// </summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? ModuleNameLocal { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }
}
