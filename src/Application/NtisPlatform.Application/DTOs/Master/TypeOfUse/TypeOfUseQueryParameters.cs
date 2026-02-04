using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseQueryParameters : BaseQueryParameters
{

    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseID { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Type { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? GroupID { get; set; }
}

