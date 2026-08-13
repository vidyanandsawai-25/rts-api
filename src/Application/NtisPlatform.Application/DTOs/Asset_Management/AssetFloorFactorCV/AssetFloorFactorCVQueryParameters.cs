using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetFloorFactorCVQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? FloorId { get; set; }

    [Filterable]
    [Sortable]
    public int? YearRangeCVId { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }
}
