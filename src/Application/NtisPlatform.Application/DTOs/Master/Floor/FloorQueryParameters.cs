using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class FloorQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? FloorCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable]
    [Sortable]
    public int? SequenceNo { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}
