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
        public int PropertyDetailsId { get; set; }
        public int PropertyId { get; set; }
        public int? RateCVMasterId { get; set; }
        public double? BaseValue { get; set; }
        // Factor IDs - store references to master factor tables instead of values
        public int? FloorFactorCVId { get; set; }
        public int? AgeFactorCVId { get; set; }
        public int? NatureFactorCVId { get; set; }  // NTB Factor
        public int? UseFactorCVId { get; set; }
        public decimal? CapitalValue { get; set; }
        public int TaxId { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }

        /// <summary>
        /// SHA256 hash of all input fields used in CV calculation.
        /// Used to detect changes in property details that require recalculation.
        /// Format: FloorId|SubFloorId|ConstructionYear|AssessmentYear|ConstructionTypeId|TypeOfUseId|SubTypeOfUseId|CarpetAreaSqMeter|BuiltupAreaSqMeter|HasLift|PropertyLevelData
        /// </summary>
        public string? CVInputHash { get; set; }

        public bool IsActive { get; set; } = true;
        public bool? MarkedForDeletion { get; set; } = false;
        public DateTime? MarkedForDeletionDate { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }  
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public PropertyEntity PropertyMast { get; set; } = null!;

        [ForeignKey(nameof(TaxId))]
        public virtual TaxMasterEntity? TaxMaster { get; set; }

        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }

        public virtual RateMasterForCVEntity? RateCVMaster { get; set; }

        // Navigation properties for factors
        [ForeignKey(nameof(FloorFactorCVId))]
        public virtual FloorFactorCVMasterEntity? FloorFactorCVMaster { get; set; }

        [ForeignKey(nameof(AgeFactorCVId))]
        public virtual AgeFactorCVMasterEntity? AgeFactorCVMaster { get; set; }

        [ForeignKey(nameof(NatureFactorCVId))]
        public virtual NatureFactorCVMasterEntity? NatureFactorCVMaster { get; set; }

        [ForeignKey(nameof(UseFactorCVId))]
        public virtual UseFactorCVMasterEntity? UseFactorCVMaster { get; set; }

    }


}
