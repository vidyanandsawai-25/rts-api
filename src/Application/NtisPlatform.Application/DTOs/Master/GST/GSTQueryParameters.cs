using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System;

namespace NtisPlatform.Application.DTOs;

/// <summary>Query parameters for GST master listing.</summary>
public class GSTQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? TaxCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? TaxName { get; set; }

    [Filterable]
    [Sortable]
    public decimal? TaxPercentage { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public DateTime? EffectiveFromDate { get; set; }

    [Filterable]
    [Sortable]
    public DateTime? EffectiveToDate { get; set; }
}
