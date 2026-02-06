using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;
public class RateSectionQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? RateSectionNo { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Description { get; set; }
}

