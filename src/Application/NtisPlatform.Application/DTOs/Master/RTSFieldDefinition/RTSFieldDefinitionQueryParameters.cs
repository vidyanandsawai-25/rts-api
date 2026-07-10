using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
 public class RTSFieldDefinitionQueryParameters: BaseQueryParameters
 {
    [Filterable]
    public int? DepartmentId { get; set; }

    [Filterable]
    public int? ServiceId { get; set; }

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
}
