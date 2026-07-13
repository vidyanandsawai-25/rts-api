using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RTSCitizenSession;

public class RTSCitizenSessionQueryParameters:BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? Id { get; set; }
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? MobileNo { get; set; }
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? Upic { get; set; }
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public string? PropertyNo { get; set; }
    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}
