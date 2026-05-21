using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class ConstructionTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? ConstructionCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public bool IsActive { get; set; }
    [Filterable(FilterOperator.Equals)]

    [Sortable]
    [Searchable]
    public int? SearchSequence { get; set; }
}
