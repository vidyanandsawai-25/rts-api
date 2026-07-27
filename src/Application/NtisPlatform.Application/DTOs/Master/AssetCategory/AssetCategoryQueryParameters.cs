using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetCategoryQueryParameters : BaseQueryParameters
{
    [Filterable] 
    [Sortable]
    [Searchable]
    public string? CategoryName { get; set; }

    [Filterable] 
    [Sortable] 
    [Searchable]
    public string? CategoryCode { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

}
