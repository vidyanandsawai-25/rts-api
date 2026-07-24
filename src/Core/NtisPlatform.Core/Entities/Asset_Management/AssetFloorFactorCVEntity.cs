using System;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Floor factor calculation multiplier master (AMS.FloorFactorCVMaster).
/// </summary>
public class AssetFloorFactorCVEntity : BaseEntity, IHardDeletable
{
    public int FloorId { get; set; }
    public decimal FactorWithLift { get; set; }
    public decimal FactorWithoutLift { get; set; }
    public int YearRangeCVId { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public FloorEntity? Floor { get; set; }
    public AssetAssessmentYearRangeMasterCVEntity? YearRangeCV { get; set; }
}
