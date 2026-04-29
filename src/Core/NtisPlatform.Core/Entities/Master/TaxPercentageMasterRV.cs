using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master
{
    public class TaxPercentageMasterRV : BaseEntity
    {
        public int YearRangeRVId { get; set; }
        public int TypeOfUseId { get; set; }
    }
}
