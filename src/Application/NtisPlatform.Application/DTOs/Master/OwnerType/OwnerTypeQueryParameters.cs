using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class OwnerTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? OwnerTypeId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? OwnerType { get; set; }
}