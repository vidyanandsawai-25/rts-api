using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class InventoryItemCategoryQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? AssetCategoryId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DisplayOrder { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TypeName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TypeCode { get; set; }
}


