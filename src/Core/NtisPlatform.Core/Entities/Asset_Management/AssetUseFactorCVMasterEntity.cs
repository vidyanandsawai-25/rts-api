using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Maps to [AMS].[UseFactorCVMaster] — CV use factors scoped to a
/// [AMS].[TypeOfUseMaster] / [AMS].[SubTypeOfUseMaster] combination for a given assessment year range.
/// </summary>
public class AssetUseFactorCVMasterEntity : BaseEntity, IHardDeletable
{
    public int TypeOfUseId { get; set; }
    public int SubTypeOfUseId { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
