using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RenterDetails
{
    public class UpdateRenterDetailsDto : UpdateBaseDtos
    {
        public int Id { get; set; }


        [StringLength(50, ErrorMessage = "AgreementId_MaxLen_50")]
        public string? AgreementId { get; set; }

        [StringLength(50, ErrorMessage = "IncrementFrequency_MaxLen_50")]
        public string? IncrementFrequency { get; set; }

        [StringLength(50, ErrorMessage = "IncrementType_MaxLen_50")]
        public string? IncrementType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "IncrementValue_Range_Min_0")]
        public double? IncrementValue { get; set; }

        [StringLength(50, ErrorMessage = "IncrementMethod_MaxLen_50")]
        public string? IncrementMethod { get; set; }

        public DateTime? DurationFrom { get; set; }
        public DateTime? DurationTo { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "RentAmount_Range_Min_0")]
        public double? RentAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "RentMonthly_Range_Min_0")]
        public double? RentMonthly { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Increment_Range_Min_0")]
        public double? Increment { get; set; }

        public bool? IncrementStatus { get; set; }

        // Custom increment fields
        public DateTime? CustomFromDate { get; set; }

        public DateTime? CustomToDate { get; set; }

        [StringLength(50, ErrorMessage = "CustomIncrementType_MaxLen_50")]
        public string? CustomIncrementType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "CustomIncrementValue_Range_Min_0")]
        public double? CustomIncrementValue { get; set; }

        [StringLength(50, ErrorMessage = "CustomMethod_MaxLen_50")]
        public string? CustomMethod { get; set; }
    }
}