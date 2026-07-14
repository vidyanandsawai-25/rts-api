using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;

public class TaxCalculationGuidelineQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? Id { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? GuidelineCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? GuidelineName { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? EnableCertificateBasedTax { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? EnableCurrentYearProration { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? TaxPersistenceMode { get; set; }
}
