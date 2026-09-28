using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Query filter parameters for the appellate officer dashboard grid.
/// </summary>
public class RTSAppealQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? AppealStatus { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? AppealLevel { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? AppealTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ServiceId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DepartmentId { get; set; }
}
