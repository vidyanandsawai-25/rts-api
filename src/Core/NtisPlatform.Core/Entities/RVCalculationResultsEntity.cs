using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace NtisPlatform.Core.Entities
{
    public class RVCalculationResultsEntity : BaseEntity, IHardDeletable
    {
        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
        public virtual PropertyEntity? PropertyMast { get; set; }
        public int PropertyId { get; set; }
        public int PropertyDetailsId { get; set; }

        // These columns are SQL `float` in PTIS.RVCalculationResults (confirmed against the
        // live table DDL) — must stay CLR double, not decimal, or EF's decimal reader throws InvalidCastException.
        public double? MonthlyRate { get; set; }
        public double? YearlyRate { get; set; }
        public double? YearlyRent { get; set; }

        public decimal? Depreciation { get; set; }
        public decimal? DepreciationPer { get; set; }
        public string? AppliedOn { get; set; }
        public double? AnnualRentalValue { get; set; }
        public decimal? Maintenance { get; set; }
        public decimal? RateableValue { get; set; }

        public virtual ICollection<RVCalculationTaxDetailsEntity> TaxDetails { get; set; } = new List<RVCalculationTaxDetailsEntity>();

        public decimal? REducationTax { get; set; }
        public decimal? CEducationTax { get; set; }

        public decimal? CEmploymentTax { get; set; }

        public double? TotalAreaSqMtr { get; set; }
        public double? RAreaSqMtr { get; set; }
        public double? CAreaSqlMtr { get; set; }

        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }
    }
}
