using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class RateMasterForCVQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? SubZoneId { get; set; }

    [Filterable]
    [Sortable]
    public int? TypeOfUseGroupCVId { get; set; }

    [Filterable]
    [Sortable]
    public int? FloorGroupId { get; set; }

    [Filterable]
    [Sortable]
    public decimal? RateAmount { get; set; }

    [Filterable]
    [Sortable]
    public int? AssessmentYearRangeId { get; set; }
}