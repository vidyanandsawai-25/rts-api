using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveCalculationEvidence;

public class RetrospectiveCalculationEvidenceQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? CalculationId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? EvidenceTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsAvailable { get; set; }
}
