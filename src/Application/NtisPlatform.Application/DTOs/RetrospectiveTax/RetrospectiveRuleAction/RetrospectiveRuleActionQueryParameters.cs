using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;

public class RetrospectiveRuleActionQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? RuleId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? TaxStartMode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? TaxCalculationMode { get; set; }
}
