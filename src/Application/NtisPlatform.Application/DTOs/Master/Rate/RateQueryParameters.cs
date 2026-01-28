using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class RateQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? ID { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ConstructionID { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? RateSectionNo { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Searchable]
    public string? TypeOfUseGroupID { get; set; }

    [Filterable]
    public int? Year { get; set; }

    [Filterable]
    public int? MinYear { get; set; }

    [Filterable]
    public int? MaxYear { get; set; }
}
