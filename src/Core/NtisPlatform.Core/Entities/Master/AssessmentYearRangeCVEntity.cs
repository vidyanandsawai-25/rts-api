namespace NtisPlatform.Core.Entities.Master;

public class AssessmentYearRangeCVEntity : BaseEntity
{   public int FromYear { get; set; }
    public int ToYear { get; set; }
    public ICollection<FloorFactorCVMasterEntity> FloorFactorCVMaster { get; set; } = new List<FloorFactorCVMasterEntity>();
    public ICollection<UseFactorCVMasterEntity> UseFactorCVMaster { get; set; } = new List<UseFactorCVMasterEntity>();
    public ICollection<NatureFactorCVMasterEntity> NatureFactorCVMaster { get; set; } = new List<NatureFactorCVMasterEntity>();
    public ICollection<AgeFactorCVMasterEntity> AgeFactorCVMaster { get; set; } = new List<AgeFactorCVMasterEntity>();
    public ICollection<RateMasterForCVEntity> RateMasterForCV { get; set; } = new List<RateMasterForCVEntity>();
    public ICollection<TaxPercentageMasterCVEntity> TaxPercentageMasterCV { get; set; } = new List<TaxPercentageMasterCVEntity>();

}