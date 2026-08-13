using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

public class AssetMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DepartmentId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? AssetNo { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? AssetName { get; set; }

    [Filterable(FilterOperator.In, EntityProperty = "AssetCategoryId")]
    [Sortable]
    public List<int>? AssetCategoryId { get; set; }

    [Filterable(FilterOperator.In, EntityProperty = "AssetTypeId")]
    [Sortable]
    public List<int>? AssetTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ParentAssetId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    public string? OccupancyStatus { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    public string? OwnershipType { get; set; }

    /// <summary>Zone filter — applied via join on AssetDetails in the service.</summary>
    public int? ZoneId { get; set; }

    /// <summary>Ward filter — applied via join on AssetDetails in the service.</summary>
    public int? WardId { get; set; }

    /// <summary>Address filter — applied via join on AssetDetails in the service.</summary>
    public string? Address { get; set; }

    [Sortable]
    public int? AssetLife { get; set; }

    [Sortable]
    public decimal? CapitalValue { get; set; }
}

