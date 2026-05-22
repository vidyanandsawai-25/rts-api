using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? CategoryId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeCode { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeName { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeNameLocal { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? Icon { get; set; }


}
