using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.RTSDepartmentMaster;

public class RTSDepartmentQueryParameters:BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? DepartmentName { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}