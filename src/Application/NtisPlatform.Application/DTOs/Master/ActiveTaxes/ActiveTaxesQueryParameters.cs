using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class ActiveTaxesQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? TaxName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TaxNameAlias { get; set; }

    [Filterable]
    [Sortable]
    public int? TaxNameOrder { get; set; }

    [Filterable]
    public int? DisplayOrder { get; set; }
}
