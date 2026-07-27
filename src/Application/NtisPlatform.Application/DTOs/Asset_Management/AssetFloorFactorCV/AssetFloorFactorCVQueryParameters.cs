using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetFloorFactorCVQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? FloorId { get; set; }

    [Filterable]
    public int? YearRangeCVId { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable]
    public bool? MarkedForDeletion { get; set; }
}
