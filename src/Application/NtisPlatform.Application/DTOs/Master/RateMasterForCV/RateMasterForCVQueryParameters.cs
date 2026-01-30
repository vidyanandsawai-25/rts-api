using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;


public class RateMasterForCVQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? ID { get; set; }
    [Filterable]
    [Sortable]
    public int? MoujaId { get; set; }
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SubZoneNo { get; set; }
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SubZoneName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? CSN { get; set; }
}