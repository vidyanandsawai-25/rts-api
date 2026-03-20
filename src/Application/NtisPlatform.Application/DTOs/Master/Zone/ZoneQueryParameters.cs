using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class ZoneQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public int? ZoneId { get; set; } = null!;
    [Filterable]
    [Sortable]
    [Searchable]
    public string? ZoneNo { get; set; } = null!;

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Description { get; set; } = null!;

    [Filterable]
    [Sortable]
    [Searchable]
    public string? DescriptionEnglish { get; set; } = null!;
}

