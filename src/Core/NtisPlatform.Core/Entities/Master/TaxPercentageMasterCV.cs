using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master
{
    public class TaxPercentageMasterCV : BaseEntity
    {
        public int YearRangeCVId { get; set; }
        public int TypeOfUseId { get; set; }
    }
}
