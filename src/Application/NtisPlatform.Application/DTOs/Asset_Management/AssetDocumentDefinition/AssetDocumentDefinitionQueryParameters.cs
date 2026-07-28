using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetDocumentDefinitionQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? DocumentCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? DocumentName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DisplayOrder { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? AssetCategoryId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? AssetTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsRequired { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }
}
