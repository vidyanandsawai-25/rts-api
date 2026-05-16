using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class ScreenFormSectionMasterQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? ScreenId { get; set; }

    [Filterable]
    [Sortable]
    public int? ParentSectionId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SectionType { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SectionName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SectionCode { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}