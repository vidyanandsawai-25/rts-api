using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>Query parameters for penalty rule master listing.</summary>
public class PenaltyRuleQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? PenaltyCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? PenaltyName { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? CalculationType { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public decimal? PenaltyValue { get; set; }

    [Filterable]
    [Sortable]
    public int? GracePeriodDays { get; set; }
}
