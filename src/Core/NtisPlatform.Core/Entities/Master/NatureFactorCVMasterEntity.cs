using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master
{
    /// <summary>
    /// Represents nature factor CV master entity for managing nature factor calculations
    /// </summary>
    [Table("NatureFactorCVMaster", Schema = "PTIS")]
    public class NatureFactorCVMasterEntity : BaseEntity
    {     
        public int ConstructionTypeId { get; set; }

        public decimal Factor { get; set; }

        public int YearRangeCVId { get; set; }

        public virtual ConstructionTypeEntity? ConstructionType { get; set; }

        public virtual AssessmentYearRangeCVEntity? YearRangeCV { get; set; }
    }
}
