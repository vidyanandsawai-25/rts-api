using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents use factor CV master entity for managing use factor calculations
/// </summary>
public class UseFactorCVMasterEntity : BaseEntity
{
    public int TypeOfUseId { get; set; }
    public int SubTypeOfUseId { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }
}
