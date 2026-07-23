using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Query parameters for filtering and sorting ScreenMaster
/// </summary>
public class ScreenMasterQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? ScreenGroupId { get; set; }

    [Filterable]
    public int? DepartmentId { get; set; }

    [Filterable]
    public int? ModuleId { get; set; }

    [Filterable]
    [Sortable]
    public string? ScreenCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ScreenName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ScreenNameLocal { get; set; }

    [Filterable]
    public bool? IsMenu { get; set; }

    [Filterable]
    public bool? IsAuthenticationRequired { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public int? DisplayOrder { get; set; }
}
