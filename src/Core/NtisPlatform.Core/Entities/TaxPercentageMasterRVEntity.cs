using System;
using System.Collections.Generic;
using System.Text;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities
{
    public class TaxPercentageMasterRVEntity : BaseEntity
    {
        public int TaxId { get; set; }
        public int TypeOfUseId { get; set; }
        public int YearRangeRVId { get; set; }
        public decimal TaxPercentage { get; set; }

        // Navigation properties
        public virtual TypeOfUseEntity? TypeOfUse { get; set; }
        public virtual AssessmentYearRangeEntity? AssessmentYearRange { get; set; }
    }
}
