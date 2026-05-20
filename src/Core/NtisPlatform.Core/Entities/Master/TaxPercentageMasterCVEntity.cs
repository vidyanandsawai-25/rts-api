using System;
using System.Collections.Generic;
using System.Text;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Core.Entities.Master
{
    public class TaxPercentageMasterCVEntity : BaseEntity
    {
     
        public int TaxId { get; set; } = 0;
        public int TypeOfUseId { get; set; } = 0;
        public int YearRangeCVId { get; set; } = 0;
        public decimal TaxPercentage { get; set; } = 0;
        public virtual TypeOfUseEntity? TypeOfUse { get; set; }
        public virtual TaxMasterEntity? TaxMaster { get; set; }
        public virtual AssessmentYearRangeCVEntity? AssessmentYearRangeCV { get; set; }


    }
}
