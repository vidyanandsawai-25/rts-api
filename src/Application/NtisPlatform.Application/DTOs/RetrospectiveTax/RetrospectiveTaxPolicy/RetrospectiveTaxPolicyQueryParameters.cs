using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;

public class RetrospectiveTaxPolicyQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? TaxPolicyCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? TaxPolicyName { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? RateMode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? PercentageMode { get; set; }
}
