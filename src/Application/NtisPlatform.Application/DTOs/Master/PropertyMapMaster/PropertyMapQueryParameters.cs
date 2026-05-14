using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

public class PropertyMapQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? Id { get; set; }

    [Filterable]
    [Sortable]
    public int? ModuleId { get; set; }

    [Filterable]
    [Sortable]
    public int? ParentPropertyMapId { get; set; }

    [Filterable]
    [Sortable]
    public int? VersionNo { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? MappingCategory { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ChangeReason { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Remark { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }
}