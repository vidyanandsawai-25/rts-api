using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RenterMast
{
    public class RenterMastDto : BaseDtos
    {
        public int PropertyDetailsId { get; set; }
        public double? RentMonthly { get; set; }
        public double? FinalYearlyRent { get; set; }
        public string? FinancialYear { get; set; }
        public DateTime? DurationFrom { get; set; }
        public DateTime? DurationTo { get; set; }
        public string? TaxLiability { get; set; }
        public double? NonCalculateRentMonthly { get; set; }
        public string? RenterNameEnglish { get; set; }
        public string? RenterName { get; set; }
        public DateTime? AgreementDate { get; set; }
        public DateTime? AgreementFromDate { get; set; }
        public DateTime? AgreementToDate { get; set; }
        public int? DocumentBindingId { get; set; }
        public Guid? DocumentGuid { get; set; }
    }
}