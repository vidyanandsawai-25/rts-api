using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RTSUser;

/// <summary>
/// Lightweight user profile returned by the RTS user lookup endpoint.
/// Allocation collections are intentionally excluded.
/// </summary>
public sealed class RtsUserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? UserCode { get; set; }
    public string? Address { get; set; }
    public string? MobileNo { get; set; }
    public string? AlternateMobileNo { get; set; }
    public string? Email { get; set; }
    public string? Remark { get; set; }
    public int? EmployeeTypeID { get; set; }
}

/// <summary>
/// Filters for GET /api/RtsUsers. DepartmentId is an internal eligibility filter;
/// department allocations are never returned in the response.
/// </summary>
public sealed class RtsUserQueryParameters : BaseQueryParameters
{
    [Filterable(EntityProperty = "Id")]
    [Sortable(EntityProperty = "Id")]
    public int? UserId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    public string? UserName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    public string? FirstName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    public string? MiddleName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    public string? LastName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    public string? MobileNo { get; set; }

    public int? DepartmentId { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }
}
