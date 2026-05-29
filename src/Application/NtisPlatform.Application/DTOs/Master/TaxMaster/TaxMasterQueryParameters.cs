using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class TaxMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TaxCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TaxName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? TaxNameAlias { get; set; }

    [Filterable]
    [Sortable]
    public int? TaxCategoryId { get; set; }

    [Filterable]
    [Sortable]
    public int? DisplayOrder { get; set; }

    [Filterable]
    [Sortable]
    public bool? TaxOnUnit { get; set; }

    [Filterable]
    [Sortable]
    public bool? AssessmentStatus { get; set; }

    [Filterable]
    [Sortable]
    public bool? OldTaxStatus { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}
