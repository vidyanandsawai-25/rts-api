using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class ActiveTaxesQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ActiveTaxesId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TaxName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TaxNameAlias { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DisplayOrder { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? TaxOnUnit { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }
}
