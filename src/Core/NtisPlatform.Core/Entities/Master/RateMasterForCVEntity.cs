using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RateMasterForCVEntity : BaseEntity
{
    public int? SubZoneId { get; set; }

    public int? TypeOfUseGroupCVId { get; set; }

    public int? FloorGroupId { get; set; }

    public int? AssessmentYearRangeId { get; set; }

    public decimal? RateAmount { get; set; }

    public virtual ICollection<CSNDetailsEntity>? CSNDetails { get; set; } = new List<CSNDetailsEntity>();


    public virtual AssessmentYearRangeCVEntity? AssessmentYearRange { get; set; }

    public virtual FloorGroupMasterEntity? FloorGroup { get; set; }

    public virtual TypeOfUseGroupCVEntity? TypeOfUseGroupCV { get; set; }

}