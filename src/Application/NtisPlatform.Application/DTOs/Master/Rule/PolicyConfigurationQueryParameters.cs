using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class PolicyConfigurationQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PolicyCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Category { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? DisplayName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? DataType { get; set; }
}
