using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class WardQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? WardNo { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? ZoneNo { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? DescriptionEnglish { get; set; } = null!;

}

