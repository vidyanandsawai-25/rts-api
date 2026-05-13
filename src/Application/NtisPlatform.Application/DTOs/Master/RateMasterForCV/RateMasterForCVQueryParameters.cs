using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Query parameters for filtering and sorting RateMasterForCV entities
/// </summary>
public class RateMasterForCVQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? SubZoneId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? TypeOfUseGroupId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? FloorGroupId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? AssessmentYearRangeId { get; set; }

    [Filterable(FilterOperator.GreaterThanOrEqual)]
    public decimal? MinRateAmount { get; set; }

    [Filterable(FilterOperator.LessThanOrEqual)]
 
    public decimal? MaxRateAmount { get; set; }

    [Filterable(FilterOperator.Equals)]
 
    public decimal? RateAmount { get; set; }
}