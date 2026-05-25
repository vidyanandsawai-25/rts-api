using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities
{
    public class TransMastRVEntity : BaseEntity, IHardDeletable
    {
        public int PropertyId { get; set; }
        public int FinanceYearId { get; set; }
        public decimal? RateableValue { get; set; }
        public int TaxId { get; set; }
        public decimal TaxAmount { get; set; } = 0m;
        public bool MarkedForDeletion { get; set; } = false;
        public DateTime? MarkedForDeletionDate { get; set; } = null;
        public virtual PropertyEntity? PropertyMast { get; set; }
        //public PropertyEntity PropertyMast { get; set; } = null!;
    }
}
