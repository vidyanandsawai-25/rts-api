using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;

public class RetrospectiveRuleDateConditionQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? RuleId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? ComparatorCode { get; set; }
}
