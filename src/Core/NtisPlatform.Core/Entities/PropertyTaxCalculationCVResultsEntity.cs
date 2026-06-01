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
 
        public int PropertyDetailsId { get; set; }
        public int PropertyId { get; set; }
      
        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }

        public int TaxId { get; set; }

        [ForeignKey(nameof(TaxId))]
        public virtual TaxMasterEntity? TaxMaster { get; set; }

 
        
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CapitalValue { get; set; }


        public decimal? TaxPercentage { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TaxAmount { get; set; }

        public int? RateCVMasterId { get; set; }
        public double? BaseValue { get; set; }
        public int? FloorFactorId { get; set; }
        public int? AgeFactorId { get; set; }
        public int? NatureFactorId { get; set; }
        public int? UseFactorId { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? SDRR { get; set; }
        public PropertyEntity PropertyMast { get; set; } = null!;
        public virtual RateMasterForCVEntity? RateCVMaster { get; set; }

    }


}
