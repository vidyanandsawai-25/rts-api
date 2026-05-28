using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.PropertyDetails;

public class FloorDetailsOldQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? OldFloorId { get; set; }

    [Filterable]
    [Sortable]
    public int? OldSubFloorId { get; set; }

    [Filterable]
    [Sortable]
    public int? OldConstructionTypeId { get; set; }

    [Filterable]
    [Sortable]
    public int? OldTypeOfUseId { get; set; }

    [Filterable]
    [Sortable]
    public int? OldSubTypeOfUseId { get; set; }

    [Filterable]
    public string? OldConstructionYear { get; set; }

    [Filterable]
    public string? OldAssessmentYear { get; set; }
}
