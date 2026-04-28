using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents age factor CV master entity for managing age-based factor calculations
/// </summary>
public class AgeFactorCVMasterEntity : BaseEntity
{
    public int ConstructionTypeId { get; set; }
    public int AgeFrom { get; set; }
    public int AgeTo { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
}
