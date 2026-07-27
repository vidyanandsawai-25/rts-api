using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Maps to [AMS].[AssessmentYearRangeMaster] — the assessment year range referenced by
/// the AMS CV factor masters (Floor/Age/Nature FactorCVMaster.YearRangeCVId).
/// </summary>
public class AssetAssessmentYearRangeMasterCVEntity : BaseEntity, IHardDeletable
{
    public int FromYear { get; set; }
    public int ToYear { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
