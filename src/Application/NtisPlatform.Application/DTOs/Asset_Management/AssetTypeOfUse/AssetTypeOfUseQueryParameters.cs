using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetTypeOfUseQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? TypeOfUseCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable]
    public string? Type { get; set; }

    [Filterable]
    public int? AssetCategoryId { get; set; }

    [Filterable]
    public int? AssetTypeId { get; set; }

    [Filterable]
    public int? TypeOfUseGroupId { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable]
    public bool? MarkedForDeletion { get; set; }
}
