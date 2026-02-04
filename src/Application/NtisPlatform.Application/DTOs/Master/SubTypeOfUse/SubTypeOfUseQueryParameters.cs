using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class SubTypeOfUseQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseID { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Description { get; set; }

}

