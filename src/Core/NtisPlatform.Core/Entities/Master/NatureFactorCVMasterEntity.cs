using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents nature factor CV master entity for managing nature factor calculations
/// </summary>
public class NatureFactorCVMasterEntity : BaseEntity
{  
    public int ConstructionTypeId { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
}