using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Maps to [AMS].[AgeFactorCVMaster] — CV age factors scoped to an
/// [AMS].[ConstructionTypeMaster] row for a given age band and assessment year range.
/// </summary>
public class AssetAgeFactorCVMasterEntity : BaseEntity, IHardDeletable
{
    public int ConstructionTypeId { get; set; }
    public int AgeFrom { get; set; }
    public int AgeTo { get; set; }
    public decimal Factor { get; set; }
    public int YearRangeCVId { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
