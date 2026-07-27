using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetFieldDefinitionQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? AssetCategoryId { get; set; }

    [Filterable]
    public int? AssetTypeId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? FieldCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? FieldName { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }
}
