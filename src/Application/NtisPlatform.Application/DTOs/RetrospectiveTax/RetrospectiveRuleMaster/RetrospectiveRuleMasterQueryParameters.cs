using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;

public class RetrospectiveRuleMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? RuleCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? RuleName { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? MatchType { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? RuleStatus { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? AuthorizationStatus { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsFallbackRule { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? PriorityNo { get; set; }
}
