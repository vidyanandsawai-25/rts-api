using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Core.Entities.Master
{

    /// <summary>
    /// Represents age factor CV master entity for managing age-based factor calculations
    /// </summary>
    [Table("AgeFactorCVMaster", Schema = "PTIS")]
    public class AgeFactorCVMasterEntity : BaseEntity
    {
       

        public int ConstructionTypeId { get; set; }

        public int AgeFrom { get; set; }

        public int AgeTo { get; set; }

        public decimal Factor { get; set; }

        public int YearRangeCVId { get; set; }

        public virtual ConstructionTypeEntity? ConstructionType { get; set; }

        public virtual AssessmentYearRangeCVEntity? YearRangeCV { get; set; }
    }

}
