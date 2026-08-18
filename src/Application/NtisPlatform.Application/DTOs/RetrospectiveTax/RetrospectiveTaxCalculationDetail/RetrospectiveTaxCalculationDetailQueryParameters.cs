using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculationDetail;

public class RetrospectiveTaxCalculationDetailQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public long? CalculationId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? PropertyId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? FloorId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? FinancialYear { get; set; }
}
