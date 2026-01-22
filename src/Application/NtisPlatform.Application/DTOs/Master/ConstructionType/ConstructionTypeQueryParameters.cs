using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class ConstructionTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? ConstructionId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? DescriptionEnglish { get; set; }
}
