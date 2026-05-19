using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class MoujaQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? MoujaName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? MoujaNo { get; set; }
}

