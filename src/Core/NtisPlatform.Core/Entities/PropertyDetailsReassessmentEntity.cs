using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents property details reassessment entity for reference validation.
/// </summary>
public class PropertyDetailsReassessmentEntity : BaseEntity
{
    public int FloorId { get; set; }
    public int SubFloorId { get; set; }
    public int ConstructionTypeId { get; set; }
    public int TypeOfUseId { get; set; }
    public int SubTypeOfUseId { get; set; }
}
