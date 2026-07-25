using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class OwnerTitleQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? OwnerTitleId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? OwnerTitle { get; set; }
}
