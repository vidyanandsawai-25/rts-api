using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace NtisPlatform.Core.Entities
{
    public class PropertyTaxCalculationRVResultsEntity:BaseEntity, IHardDeletable
    {
        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
        public virtual PropertyEntity? PropertyMast { get; set; }
        public int PropertyId { get; set; }
        public int PropertyDetailsId { get; set; }

        public double? MonthlyRate { get; set; }
        public double? YearlyRate { get; set; }
        public double? YearlyRent { get; set; }

        public decimal? Depreciation { get; set; }
        public double? AnnualRentalValue { get; set; }
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

        public double? TotalAreaSqMtr { get; set; }
        public double? RAreaSqMtr { get; set; }
        public double? CAreaSqlMtr { get; set; }

        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }
    }
}
