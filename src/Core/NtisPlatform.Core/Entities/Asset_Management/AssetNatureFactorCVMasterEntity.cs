using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Maps to [AMS].[NatureFactorCVMaster] — CV nature factors scoped to an
/// [AMS].[ConstructionTypeMaster] row for a given assessment year range.
/// </summary>
public class AssetNatureFactorCVMasterEntity : BaseEntity, IHardDeletable
{
    public int ConstructionTypeId { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
