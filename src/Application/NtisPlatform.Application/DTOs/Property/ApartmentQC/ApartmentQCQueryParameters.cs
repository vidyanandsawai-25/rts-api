using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Query parameters for the Apartment QC list endpoint.
/// Inherits standard pagination, search, sort, and filter-logic controls
/// from <see cref="BaseQueryParameters"/>.
/// </summary>
public sealed class ApartmentQCQueryParameters : BaseQueryParameters
{
    /// <summary>Filter by ward. Optional — omit to search across all wards.</summary>
    [Filterable]
    public int? WardId { get; set; }

    /// <summary>Partial-match filter on the property number.</summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PropertyNo { get; set; }

    /// <summary>Partial-match filter on the Partition number.</summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PartitionNo { get; set; }

    /// <summary>Exact filter on the partition type (e.g. "Shop", "Flat").</summary>
    [Filterable]
    [Sortable]
    public string? PartType { get; set; }

    /// <summary>Exact filter on type-of-use code (matched via PropertyDetails → TypeOfUse).</summary>
    [Filterable]
    [Sortable]
    public string? Type { get; set; }

    /// <summary>Filter by a specific PropertyMast Id (for single-property lookup).</summary>
    [Filterable]
    public int? PropertyId { get; set; }

    /// <summary>Partial-match filter on flat or shop number (PropertyMast.FlatOrShopNo).</summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? FlatOrShopNo { get; set; }

    /// <summary>Exact filter on apartment/property type (PropertyMast.Type).</summary>
    [Filterable]
    [Sortable]
    public string? ApartmentType { get; set; }

    /// <summary>Filter by PropertyType master Id (PropertyMast.PropertyTypeId).</summary>
    [Filterable]
    [Sortable]
    public int? PropertyType { get; set; }

    /// <summary>Partial-match filter on wing name (SocietyDetailsMast.WingName).</summary>
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Wing { get; set; }
    public ApartmentQCResultType? ResultType { get; set; }
}
