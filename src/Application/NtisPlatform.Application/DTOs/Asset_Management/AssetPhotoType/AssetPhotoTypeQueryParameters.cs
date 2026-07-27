using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetPhotoTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? PhotoTypeCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PhotoTypeName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable]
    public int? AssetCategoryId { get; set; }

    [Filterable]
    public int? AssetTypeId { get; set; }

    [Filterable]
    public bool? IsRequired { get; set; }

    [Filterable]
    public bool? IsSubUnit { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable]
    public bool? MarkedForDeletion { get; set; }

    [Filterable]
    [Sortable]
    public int? DisplayOrder { get; set; }
}
