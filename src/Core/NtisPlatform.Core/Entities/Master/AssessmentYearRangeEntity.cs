
namespace NtisPlatform.Core.Entities.Master;

public class AssessmentYearRangeEntity : BaseEntity
{
    public int FromYear { get; set; }
    public int ToYear { get; set; }
    public ICollection<RateEntity> Rates { get; set; } = new List<RateEntity>();
    public ICollection<TaxPercentageMasterRVEntity> TaxPercentageMasterRV { get; set; } = new List<TaxPercentageMasterRVEntity>();
    public ICollection<DepreciationMasterEntity> DepreciationMaster { get; set; } = new List<DepreciationMasterEntity>();
}

