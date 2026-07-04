using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseCategoryQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseCategoryCode { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseCategoryName { get; set; }
}
