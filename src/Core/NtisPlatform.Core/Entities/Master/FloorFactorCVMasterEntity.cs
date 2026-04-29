using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents floor factor CV master entity for managing floor factor calculations
/// </summary>
public class FloorFactorCVMasterEntity : BaseEntity
{
    public int FloorId { get; set; }
    public decimal FactorWithLift { get; set; }
    public decimal FactorWithoutLift { get; set; }
    public int YearRangeCVId { get; set; }   
}
