using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;


namespace NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;

public class PropertyAssessmentStatusQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? Id { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? StatusName { get; set; }
}
