using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.PropertyPhotoType;

public class PropertyPhotoTypeQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? PhotoTypeCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PhotoTypeName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DisplayOrder { get; set; }
}
