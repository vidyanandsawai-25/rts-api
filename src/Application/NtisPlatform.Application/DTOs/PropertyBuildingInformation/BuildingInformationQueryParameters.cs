using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.PropertyBuildingInformation;

public class BuildingInformationQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public string? OldWardNo { get; set; }

    [Filterable]
    [Sortable]
    public string? OldSocietyName { get; set; }

    [Filterable]
    [Sortable]
    public int? MapId { get; set; }
}