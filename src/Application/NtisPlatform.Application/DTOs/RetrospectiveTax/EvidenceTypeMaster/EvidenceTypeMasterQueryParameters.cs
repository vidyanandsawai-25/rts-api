using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;

public class EvidenceTypeMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? EvidenceCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? EvidenceName { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsCertificate { get; set; }
}
