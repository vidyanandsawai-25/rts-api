namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// AMS capital-value rate master. Maps to [AMS].[CVRateMaster].
/// Column-compatible with the legacy PTIS.RateCVMaster (RateMasterForCVEntity) rate
/// lookup, but scoped to Asset Management CV calculation only — property-tax CV still
/// uses RateMasterForCVEntity. Rate is keyed by SubZone + TypeOfUseGroupCV + FloorGroup
/// (when floor-wise) + AssessmentYearRange.
/// </summary>
public class CVRateMasterEntity : BaseEntity
{
    public int? SubZoneId { get; set; }
    public int? TypeOfUseGroupCVId { get; set; }
    public int? FloorGroupId { get; set; }
    public int? AssessmentYearRangeId { get; set; }
    public decimal? RateAmount { get; set; }
}
