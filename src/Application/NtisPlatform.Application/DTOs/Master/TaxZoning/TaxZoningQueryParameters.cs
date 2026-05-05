using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class TaxZoningQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public int? TaxZoneId { get; set; } 

    [Filterable]
    [Searchable]
    [Sortable]
    public int? WardId { get; set; } 

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? PropertyNo { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public bool? IsActive { get; set; } = true;

    public string? GroupBy { get; set; } // Values: null (default), "ward"
}
