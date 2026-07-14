using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RenterMast
{
    public class CreateRenterMastDto : CreateBaseDtos
    {
        [Range(1, int.MaxValue, ErrorMessage = "PropertyDetailsId is required.")]
        public int PropertyDetailsId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "RentMonthly_Range_Min_0")]
        public double? RentMonthly { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "FinalYearlyRent_Range_Min_0")]
        public double? FinalYearlyRent { get; set; }

        [StringLength(4, ErrorMessage = "FinancialYear_MaxLen_4")]
        public string? FinancialYear { get; set; }

        public DateTime? DurationFrom { get; set; }

        public DateTime? DurationTo { get; set; }

        [StringLength(20, ErrorMessage = "TaxLiability_MaxLen_20")]
        public string? TaxLiability { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "NonCalculateRentMonthly_Range_Min_0")]
        public double? NonCalculateRentMonthly { get; set; }

        [StringLength(500, ErrorMessage = "RenterNameEnglish_MaxLen_500")]
        public string? RenterNameEnglish { get; set; }

        [StringLength(500, ErrorMessage = "RenterName_MaxLen_500")]
        public string? RenterName { get; set; }

        public DateTime? AgreementDate { get; set; }

        public DateTime? AgreementFromDate { get; set; }

        public DateTime? AgreementToDate { get; set; }

        public int? DocumentBindingId { get; set; }
    }
}