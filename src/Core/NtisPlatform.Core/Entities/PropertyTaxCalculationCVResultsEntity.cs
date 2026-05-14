using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace NtisPlatform.Core.Entities
{
    [Table("PropertyTaxCalculationCVResults", Schema = "PTIS")]
    public class PropertyTaxCalculationCVResultsEntity  
    {
        // note - Due To in Db There is the Id in BIGInt we cant inherit the baseEntity
        public long Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool MarkedForDeletion { get; set; } = false;
        public DateTime? MarkedForDeletionDate { get; set; }
        public int PropertyDetailsId { get; set; }
        public int PropertyId { get; set; }
        public int? FinanceYearId { get; set; }
        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }

        public int TaxId { get; set; }

        [ForeignKey(nameof(TaxId))]
        public virtual TaxMasterEntity? TaxMaster { get; set; }

        [ForeignKey(nameof(FinanceYearId))]
        public virtual YearMasterEntity? YearMaster { get; set; }
        
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CapitalValue { get; set; }


        public decimal? TaxPercentage { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TaxAmount { get; set; }

        public int? RateCVMasterId { get; set; }
        public double? BaseValue { get; set; }
        public double? FloorFactor { get; set; }
        public double? AgeFactor { get; set; }
        public double? NTBFactor { get; set; }
        public double? UseFactor { get; set; }
        public PropertyEntity PropertyMast { get; set; } = null!;
        public virtual RateMasterForCVEntity? RateCVMaster { get; set; }

    }


}
