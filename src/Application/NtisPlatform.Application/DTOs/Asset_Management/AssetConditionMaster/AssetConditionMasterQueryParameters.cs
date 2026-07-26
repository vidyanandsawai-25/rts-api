using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetConditionMasterQueryParameters : BaseQueryParameters
{
    // 'Asset' | 'Inventory' — the Add-Asset wizard filters on 'Asset'.
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? ConditionCategory { get; set; }

    // Polymorphic: AssetCategoryMaster.Id when ConditionCategory='Asset', InventoryItemCategoryMaster.Id
    // when 'Inventory'. The Add-Asset wizard passes the selected Asset Category's id here so only the
    // conditions seeded for that category (e.g. Buildings vs Land & Plots) are returned.
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? CategoryId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ConditionName { get; set; }

    [Sortable]
    public int? DisplayOrder { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }
}
