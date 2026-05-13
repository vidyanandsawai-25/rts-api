using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.DTOs.CapitalValue
{

    public class CapitalValueDto
    {
        public int Id { get; set; }
        public int? PropertyDetailsId { get; set; }
        public int? PropertyId { get; set; }
        public string? FloorDescription { get; set; }
        public string? SubFloorDescription { get; set; }
        public string? ConstructionTypeDescription { get; set; }
        public string? TypeOfUseDescription { get; set; }
        public string? SubTypeOfUseDescription { get; set; }
        public string? ConstructionYear { get; set; }
        public string? AssessmentYear { get; set; }
        public int? NoOfRooms { get; set; }
        public double? CarpetAreaSqFeet { get; set; }
        public double? CarpetAreaSqMeter { get; set; }
        public double? BuiltupAreaSqMeter { get; set; }
        public double? BuiltupAreaSqFeet { get; set; }
        public bool? RenterYesNo { get; set; }
        public string? RenterName { get; set; }
        public double? RentMonthly { get; set; }
        public double? SDRR { get; set; }
        public double? BaseValue { get; set; }
        public double? FloorFactor { get; set; }
        public double? AgeFactor { get; set; }
        public double? NTBFactor { get; set; }
        public double? UseFactor { get; set; }
        public decimal? CapitalValue { get; set; }
        public int? YearRangeCVId { get; set; }
        public List<TaxHeadDto> Taxes { get; set; } = new();

    }
    public class CreateCapitalValueDto
    {
        /// <summary>
        /// The ID of the property for which to calculate capital value.
        /// </summary>
        public int PropertyId { get; set; }

        /// <summary>
        /// Optional: Specific PropertyDetails ID to calculate. If 0 or not provided, all property details will be calculated.
        /// </summary>
        public int PropertyDetailsId { get; set; }

        /// <summary>
        /// Policy code to be used in PolicyTaxDetailsCV records. Defaults to "NETTAX".
        /// </summary>
        public string? PolicyCode { get; set; } = "NETTAX";

        /// <summary>
        /// Policy date for PolicyTaxDetailsCV records. If not provided, current date will be used.
        /// </summary>
        public DateTime? PolicyDate { get; set; }

        /// <summary>
        /// Policy year for PolicyTaxDetailsCV records. If not provided, current year will be used.
        /// </summary>
        public int? PolicyYear { get; set; }

        /// <summary>
        /// Optional policy reason/description for PolicyTaxDetailsCV records.
        /// </summary>
        public string? PolicyReason { get; set; }

        /// <summary>
        /// Finance year for TransMastCV records. If not provided, active finance year will be used.
        /// </summary>
        public int? FinanceYear { get; set; }

        /// <summary>
        /// User ID creating the capital value calculation.
        /// </summary>
        public int? CreatedBy { get; set; }

        // NOTE: This operation will automatically create/update:
        // - PropertyTaxCalculationCVResults (tax calculation details per property detail)
        // - PolicyTaxDetailsCV (aggregated policy tax details)
        // - TransMastCV (transaction master records for the finance year)
    }
    public class TaxHeadDto
    {
        public int? TaxId { get; set; }
        public string? TaxName { get; set; }
        public decimal? Percentage { get; set; }
        public decimal? Amount { get; set; }
 
    }

}
