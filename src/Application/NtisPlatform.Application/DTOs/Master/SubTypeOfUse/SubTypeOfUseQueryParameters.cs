using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class SubTypeOfUseQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public int? TypeOfUseId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Description { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public int? TypeOfUseCategoryId { get; set; }

}

