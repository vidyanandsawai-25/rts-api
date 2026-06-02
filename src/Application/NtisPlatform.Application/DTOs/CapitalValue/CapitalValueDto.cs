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
         public int PropertyId { get; set; }
         public int PropertyDetailsId { get; set; }
         public string? PolicyCode { get; set; } = "NETTAX";
         public DateTime? PolicyDate { get; set; }
         public int? PolicyYear { get; set; }
         public string? PolicyReason { get; set; }
         public int? FinanceYear { get; set; }
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
