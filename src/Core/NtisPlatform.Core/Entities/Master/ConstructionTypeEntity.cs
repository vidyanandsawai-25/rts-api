using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

/// <summary>
///  Represents a ConstructionType entity manage building construction type information.
/// </summary>
public class ConstructionTypeEntity :BaseEntity
{   
    public string? ConstructionCode { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public int? SearchSequence { get; set; } = 0;
    public ICollection<RateEntity> Rates { get; set; } = new List<RateEntity>();
    public ICollection<NatureFactorCVMasterEntity> NatureFactorCVMaster { get; set; } = new List<NatureFactorCVMasterEntity>();
    public ICollection<AgeFactorCVMasterEntity> AgeFactorCVMaster { get; set; } = new List<AgeFactorCVMasterEntity>();
    public ICollection<PropertyDetailsEntity> PropertyDetails { get; set; } = new List<PropertyDetailsEntity>();
    public ICollection<DepreciationMasterEntity> DepreciationMaster { get; set; } = new List<DepreciationMasterEntity>();
}
