using System;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities
{
    public class RVCalculationTaxDetailsEntity : BaseEntity, IHardDeletable
    {
        public int RVCalculationResultsId { get; set; }
        public virtual RVCalculationResultsEntity? RVCalculationResults { get; set; }

        public int TaxId { get; set; }
        [ForeignKey(nameof(TaxId))]
        public virtual TaxMasterEntity? TaxMaster { get; set; }

        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }

        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }
    }
}
