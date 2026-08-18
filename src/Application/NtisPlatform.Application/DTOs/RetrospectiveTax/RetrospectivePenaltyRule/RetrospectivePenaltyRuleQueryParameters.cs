using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;

public class RetrospectivePenaltyRuleQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? RuleId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? PenaltyMode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsPenaltyApplicable { get; set; }
}
