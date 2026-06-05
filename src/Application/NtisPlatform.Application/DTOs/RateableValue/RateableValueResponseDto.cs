using System;
using System.Collections.Generic;


namespace NtisPlatform.Application.DTOs.RateableValue
{
    public class RateableValueResponseDto
    {
        public int PropertyId { get; set; }
        public int FinanceYear { get; set; }
        public decimal TotalRateableValue { get; set; }
        public decimal TotalTax { get; set; }
        public PolicyTaxDto? Policy { get; set; }
        public List<RateableValueDetailDto> Details { get; set; } = new();
    }

    public class RateableValueDetailDto
    {
        public int PropertyDetailsId { get; set; }
        public bool Taxable { get; set; }
        public string Floor { get; set; } = string.Empty;
        public string SubFloor { get; set; } = string.Empty;
        public string ConstructionYear { get; set; } = string.Empty;
        public string AssessmentYear { get; set; } = string.Empty;
        public string ConstructionType { get; set; } = string.Empty;
        public string Use { get; set; } = string.Empty;
        public string SubTypeOfUse { get; set; } = string.Empty;
        public int NoOfRooms { get; set; }
        public double CarpetAreaSqFeet { get; set; }
        public double CarpetAreaSqMeter { get; set; }
        public double BuiltupAreaSqFeet { get; set; }
        public double BuiltupAreaSqMeter { get; set; }
        public string OccupancyNumber { get; set; } = string.Empty;
        public DateTime? OccupancyDate { get; set; }
        public string RenterName { get; set; } = string.Empty;
        public decimal RentMonthly { get; set; }
        public decimal RentYearly { get; set; }
        public decimal MonthlyRate { get; set; }
        public decimal YearlyRate { get; set; }
        public decimal YearlyRent { get; set; }
        public decimal Depreciation { get; set; }
        public decimal DepreciationPer { get; set; }
        public string AppliedOn { get; set; } = string.Empty;
        public decimal AnnualRentalValue { get; set; }
        public decimal Maintenance { get; set; }
        public decimal RateableValue { get; set; }
        public decimal TaxTotal { get; set; }
        public Dictionary<string, decimal> Taxes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class PolicyTaxDto
    {
        public string PolicyCode { get; set; } = string.Empty;
        public DateTime? PolicyDate { get; set; }
        public short? PolicyYear { get; set; }
        public decimal PolicyRVorCVvalue { get; set; }
        public decimal TaxTotal { get; set; }
        public Dictionary<string, decimal> Taxes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
