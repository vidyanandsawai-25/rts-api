using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Core.Entities.Master
{
    public class EducationTaxMasterEntity : BaseEntity
    {
        public string? Type { get; set; }
        public int? Year { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal? Rate { get; set; }
        public string? OnRVOrALV { get; set; }
    }
}
