using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;

public class RetrospectiveRuleEvidenceConditionQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? RuleId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? EvidenceTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? EvidenceState { get; set; }
}
