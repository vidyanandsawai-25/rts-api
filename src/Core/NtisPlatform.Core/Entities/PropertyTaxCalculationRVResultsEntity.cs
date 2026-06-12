using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace NtisPlatform.Core.Entities
{
    public class PropertyTaxCalculationRVResultsEntity : BaseEntity, IHardDeletable
    {
        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
        public virtual PropertyEntity? PropertyMast { get; set; }
        public int PropertyId { get; set; }
        public int PropertyDetailsId { get; set; }

        // Financial rate and rent fields stored as decimal for precision consistency.
        // MIGRATION REQUIRED: update column types from float to decimal(18,4) for these columns.
        public decimal? MonthlyRate { get; set; }
        public decimal? YearlyRate { get; set; }
        public decimal? YearlyRent { get; set; }

        public decimal? Depreciation { get; set; }
        public decimal? DepreciationPer { get; set; }
        public string? AppliedOn { get; set; }
        public decimal? AnnualRentalValue { get; set; }
        public decimal? Maintenance { get; set; }
        public decimal? RateableValue { get; set; }

        public int TaxId { get; set; }
        [ForeignKey(nameof(TaxId))]
        public virtual TaxMasterEntity? TaxMaster { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }

        public decimal? REducationTax { get; set; }
        public decimal? CEducationTax { get; set; }
        public decimal? REducationTaxPercentage { get; set; }
        public decimal? CEducationTaxPercentage { get; set; }

        public decimal? REmploymentTax { get; set; }
        public decimal? CEmploymentTax { get; set; }
        public decimal? REmploymentTaxPercentage { get; set; }
        public decimal? CEmploymentTaxPercentage { get; set; }

        public decimal? TotalAreaSqMtr { get; set; }
        public decimal? RAreaSqMtr { get; set; }
        public decimal? CAreaSqlMtr { get; set; }

        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }
    }
}
