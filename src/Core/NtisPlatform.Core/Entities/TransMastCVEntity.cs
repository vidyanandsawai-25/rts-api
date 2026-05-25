using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities
{
    public class TransMastCVEntity : BaseEntity, IHardDeletable
    {
        public int PropertyId { get; set; }

        public int FinanceYearId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CapitalValue { get; set; }

        public int TaxId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0m;

        public bool MarkedForDeletion { get; set; } = false;

        public DateTime? MarkedForDeletionDate { get; set; } = null;

        // Navigation properties
        [ForeignKey(nameof(PropertyId))]
        public virtual PropertyEntity? PropertyMast { get; set; }

        [ForeignKey(nameof(TaxId))]
        public virtual TaxMasterEntity? TaxMaster { get; set; }

        [ForeignKey(nameof(FinanceYearId))]
        public virtual YearMasterEntity? YearMaster { get; set; }
    }
}
