using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class RateQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? RateId { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public int? ConstructionTypeId { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public int? RateSectionId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Searchable]
    public int? TypeOfUseGroupId { get; set; }

    [Filterable]
    public int? Year { get; set; }

    [Filterable]
    public int? YearRangeRVId { get; set; }
}
