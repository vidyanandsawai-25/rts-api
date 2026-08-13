using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;

public class SubUnitsDetailsQueryParameters : BaseQueryParameters
{
    public int? AssetId { get; set; }
    public int? FloorId { get; set; }
    public int? SubFloorId { get; set; }
    public int? ConstructionTypeId { get; set; }
    public int? TypeOfUseId { get; set; }
    public int? SubTypeOfUseId { get; set; }
    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public bool? MarkedForDeletion { get; set; }
}
