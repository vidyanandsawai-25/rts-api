using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseQueryParameters : BaseQueryParameters
{

    [Filterable]
    [Sortable]
    [Searchable]
    public int? TypeOfUseId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseCode { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Type { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Description { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public int? TypeOfUseGroupId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public int? TypeOfUseCategoryId { get; set; }


    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseCategoryCode { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseCategoryName { get; set; }
}

