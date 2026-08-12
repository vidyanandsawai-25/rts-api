using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class SubFloorQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? SubFloorCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable]
    [Sortable]
    public decimal? SubFloorPercentage { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
    [Filterable]
    [Sortable]
    public int? SequenceNo { get; set; }
}