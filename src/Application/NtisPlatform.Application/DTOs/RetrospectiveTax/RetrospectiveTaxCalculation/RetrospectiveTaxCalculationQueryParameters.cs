using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculation;

public class RetrospectiveTaxCalculationQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? PropertyId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? CalculationMode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? CalculationStatus { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? AuthorizationStatus { get; set; }
}
