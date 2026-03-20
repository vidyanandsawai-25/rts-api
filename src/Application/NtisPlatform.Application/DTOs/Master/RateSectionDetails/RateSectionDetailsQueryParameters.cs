using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class RateSectionDetailsQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public int? RateSectionId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public int? WardId { get; set; }
}

