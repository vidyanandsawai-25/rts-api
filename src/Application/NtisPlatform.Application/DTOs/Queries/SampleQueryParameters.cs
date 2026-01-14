using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Queries;

public class SampleQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Name { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? Description { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.GreaterThanOrEqual)]
    public DateTime? CreatedAfter { get; set; }

    [Filterable(FilterOperator.LessThanOrEqual)]
    public DateTime? CreatedBefore { get; set; }
}
