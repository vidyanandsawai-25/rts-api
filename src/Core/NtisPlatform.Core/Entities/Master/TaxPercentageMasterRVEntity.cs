using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master
{
    public class TaxPercentageMasterRVEntity : BaseEntity
    {
        public int YearRangeRVId { get; set; }
        public int TypeOfUseId { get; set; }
        public virtual TypeOfUseEntity? TypeOfUse { get; set; }
        public virtual AssessmentYearRangeEntity? AssessmentYearRange { get; set; }
    }
}
