using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master
{
    public class TaxPercentageMasterRV : BaseEntity
    {
        public int TaxId { get; set; }
        public int TaxPercentage { get; set; }
        public int YearRangeRVId { get; set; }
        public int TypeOfUseId { get; set; }
        public virtual TypeOfUseEntity? TypeOfUse { get; set; }
        public virtual AssessmentYearRangeEntity? AssessmentYearRange { get; set; }
    }
}
