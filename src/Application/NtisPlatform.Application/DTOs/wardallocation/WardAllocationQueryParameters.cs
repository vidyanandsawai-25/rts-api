using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.wardallocation;

/// <summary>
/// Query parameters for Ward Allocation filtering, sorting, and pagination.
/// </summary>
public class WardAllocationQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by user id.
    /// Example: ?UserId=1
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? UserId { get; set; }

    /// <summary>
    /// Search employee name.
    /// Example: ?EmployeeName=Alex
    /// </summary>
    [Filterable(FilterOperator.Contains, EntityProperty = "User.UserName")]
    [Sortable]
    [Searchable]
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Filter by module id.
    /// Example: ?ModuleId=2
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ModuleId { get; set; }

    /// <summary>
    /// Search module name.
    /// Example: ?ModuleName=Survey
    /// </summary>
    [Filterable(FilterOperator.Contains, EntityProperty = "Module.ModuleName")]
    [Sortable]
    [Searchable]
    public string? ModuleName { get; set; }

    /// <summary>
    /// Filter by zone id.
    /// Example: ?ZoneId=1
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ZoneId { get; set; }

    /// <summary>
    /// Search zone number.
    /// Example: ?ZoneNo=UT
    /// </summary>
    [Filterable(FilterOperator.Contains, EntityProperty = "Zone.ZoneNo")]
    [Sortable]
    [Searchable]
    public string? ZoneNo { get; set; }

    /// <summary>
    /// Filter by ward id.
    /// Example: ?WardId=10
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? WardId { get; set; }

    /// <summary>
    /// Search ward number.
    /// Example: ?WardNo=UT1
    /// </summary>
    [Filterable(FilterOperator.Contains, EntityProperty = "Ward.WardNo")]
    [Sortable]
    [Searchable]
    public string? WardNo { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DepartmentId { get; set; }

    [Filterable(FilterOperator.Contains, EntityProperty = "Department.DepartmentName")]
    [Sortable]
    [Searchable]
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Filter by active status.
    /// Example: ?IsActive=true
    /// </summary>
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }
}