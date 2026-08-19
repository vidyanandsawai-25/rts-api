using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.VillageMaster;

public class VillageQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? Id { get; set; }

    [Filterable]
    [Sortable]
    public int? ZoneId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? VillageName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? VillageNameEnglish { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? Pincode { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }
}
