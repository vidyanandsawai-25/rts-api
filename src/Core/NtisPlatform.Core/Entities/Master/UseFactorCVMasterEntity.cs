
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master
{
    /// <summary>
    /// Represents use factor CV master entity for managing use factor calculations
    /// </summary>
    /// 

    [Table("UseFactorCVMaster", Schema = "PTIS")]
    public class UseFactorCVMasterEntity : BaseEntity
    {
       

        public int TypeOfUseId { get; set; }

        public int SubTypeOfUseId { get; set; }

        public decimal Factor { get; set; }

        public int YearRangeCVId { get; set; }

        public virtual TypeOfUseEntity? TypeOfUse { get; set; }

        public virtual SubTypeOfUseEntity? SubTypeOfUse { get; set; }

        public virtual AssessmentYearRangeCVEntity? YearRangeCV { get; set; }
    }

}
