using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Models
{
    public class CreateNewPropertyResponseDto
    {
        public int PropertyId { get; set; }
        public string? UPICID { get; set; } = null;
        public string? Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
        public int WardID { get; set; }
    }
    public class CreateNewPropertyDto
    {
        // -- Property basic details ---------------------------------------------

        [Required(ErrorMessage = "PropertyTypeId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "PropertyTypeId must be a valid positive number.")]
        public int PropertyTypeId { get; set; }

        [Required(ErrorMessage = "CategoryId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid positive number.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "TaxZoneId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "TaxZoneId must be a valid positive number.")]
        public int TaxZoneId { get; set; }

        [Required(ErrorMessage = "WardId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "WardId must be a valid positive number.")]
        public int WardId { get; set; }
        public string? BuilderMobileNo { get; set; }
        public int? BuilderMobileRemarkId { get; set; }

        [StringLength(50, ErrorMessage = "CSN cannot exceed 50 characters")]
        public string? CSN { get; set; }

        [StringLength(200, ErrorMessage = "SurveyRemark cannot exceed 200 characters")]
        public string? SurveyRemark { get; set; }

        [StringLength(50, ErrorMessage = "BlockNo cannot exceed 50 characters")]
        public string? BlockNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PropertyMastOldId must be greater than 0.")]
        public int? PropertyMastOldId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "SocietyDetailId must be greater than 0.")]
        public int? SocietyDetailId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PropertyAssessmentStatusId must be greater than 0.")]
        public int? PropertyAssessmentStatusId { get; set; }

        [StringLength(6, MinimumLength = 6, ErrorMessage = "PinCode must be exactly 6 characters.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "PinCode must be a valid 6-digit number.")]
        public string? PinCode { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "MobileNoRemarkId must be valid.")]
        public int? MobileNoRemarkId { get; set; }

        [Phone(ErrorMessage = "AlternateMobileNo is not a valid phone number.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "AlternateMobileNo must be a valid 10-digit Indian mobile number.")]
        public string? AlternateMobileNo { get; set; }

        [Phone(ErrorMessage = "OccupierMobileNo is not a valid phone number.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "OccupierMobileNo must be a valid 10-digit Indian mobile number.")]
        public string? OccupierMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "OccupierMobileNoRemarkId must be valid.")]
        public int? OccupierMobileNoRemarkId { get; set; }

        [StringLength(50, ErrorMessage = "PropertyNo cannot exceed 50 characters.")]
        public string? PropertyNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PropertySeqNo must be greater than 0.")]
        public int? PropertySeqNo { get; set; }
        [StringLength(50, ErrorMessage = "PartitionNo cannot exceed 50 characters.")]
        public string? PartitionNo { get; set; }
        public bool OpenPlot { get; set; }

        [StringLength(50, ErrorMessage = "PlotNo cannot exceed 50 characters.")]
        public string? PlotNo { get; set; }

        [StringLength(5, ErrorMessage = "Type cannot exceed 5 characters. Use short codes like 'COM'.")]
        public string? Type { get; set; }

        [StringLength(50, ErrorMessage = "PartType cannot exceed 50 characters.")]
        public string? PartType { get; set; }

        // -- Owner details ------------------------------------------------------

        [StringLength(20, ErrorMessage = "OwnerTitle cannot exceed 20 characters.")]
        public string? OwnerTitle { get; set; }

        [StringLength(20, ErrorMessage = "OwnerTitleEnglish cannot exceed 20 characters.")]
        public string? OwnerTitleEnglish { get; set; }

        [Required(ErrorMessage = "OwnerName is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "OwnerName must be between 2 and 100 characters.")]
        public string OwnerName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "OwnerNameEnglish cannot exceed 100 characters.")]
        public string? OwnerNameEnglish { get; set; }

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "MobileNo must be exactly 10 digits.")]
        public string? MobileNo { get; set; }

        [EmailAddress(ErrorMessage = "EmailId is not a valid email address.")]
        [StringLength(100, ErrorMessage = "EmailId cannot exceed 100 characters.")]
        public string? EmailId { get; set; }

        // -- Occupier details ---------------------------------------------------

        [StringLength(20, ErrorMessage = "OccupierTitle cannot exceed 20 characters.")]
        public string? OccupierTitle { get; set; }

        [StringLength(20, ErrorMessage = "OccupierTitleEnglish cannot exceed 20 characters.")]
        public string? OccupierTitleEnglish { get; set; }

        [StringLength(100, ErrorMessage = "OccupierName cannot exceed 100 characters.")]
        public string? OccupierName { get; set; }

        [StringLength(100, ErrorMessage = "OccupierNameEnglish cannot exceed 100 characters.")]
        public string? OccupierNameEnglish { get; set; }

        // -- Flat / shop details ------------------------------------------------

        [StringLength(20, ErrorMessage = "FlatOrShopNo cannot exceed 20 characters.")]
        public string? FlatOrShopNo { get; set; }

        [StringLength(20, ErrorMessage = "FlatOrShopNoEnglish cannot exceed 20 characters.")]
        public string? FlatOrShopNoEnglish { get; set; }

        [StringLength(100, ErrorMessage = "FlatOrShopName cannot exceed 100 characters.")]
        public string? FlatOrShopName { get; set; }

        [StringLength(100, ErrorMessage = "FlatOrShopNameEnglish cannot exceed 100 characters.")]
        public string? FlatOrShopNameEnglish { get; set; }

        // -- Address details ----------------------------------------------------

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        [StringLength(250, ErrorMessage = "AddressEnglish cannot exceed 250 characters.")]
        public string? AddressEnglish { get; set; }

        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string? Location { get; set; }

        [StringLength(100, ErrorMessage = "LocationEnglish cannot exceed 100 characters.")]
        public string? LocationEnglish { get; set; }

        // -- Society details (Apartment category only) --------------------------

        [StringLength(150, ErrorMessage = "SocietyName cannot exceed 150 characters.")]
        public string? SocietyName { get; set; }

        [StringLength(150, ErrorMessage = "SocietyNameEnglish cannot exceed 150 characters.")]
        public string? SocietyNameEnglish { get; set; }

        [StringLength(250, ErrorMessage = "SocietyAddress cannot exceed 250 characters.")]
        public string? SocietyAddress { get; set; }

        [StringLength(250, ErrorMessage = "SocietyAddressEnglish cannot exceed 250 characters.")]
        public string? SocietyAddressEnglish { get; set; }

        [StringLength(100, ErrorMessage = "SecretaryName cannot exceed 100 characters.")]
        public string? SecretaryName { get; set; }

        [StringLength(100, ErrorMessage = "SecretaryNameEnglish cannot exceed 100 characters.")]
        public string? SecretaryNameEnglish { get; set; }

        [StringLength(100, ErrorMessage = "ManagerName cannot exceed 100 characters.")]
        public string? ManagerName { get; set; }

        [StringLength(100, ErrorMessage = "ManagerNameEnglish cannot exceed 100 characters.")]
        public string? ManagerNameEnglish { get; set; }

        [StringLength(100, ErrorMessage = "LandOwnerName cannot exceed 100 characters.")]
        public string? LandOwnerName { get; set; }

        [StringLength(100, ErrorMessage = "LandOwnerNameEnglish cannot exceed 100 characters.")]
        public string? LandOwnerNameEnglish { get; set; }

        [StringLength(100, ErrorMessage = "BuilderName cannot exceed 100 characters.")]
        public string? BuilderName { get; set; }

        [StringLength(100, ErrorMessage = "BuilderNameEnglish cannot exceed 100 characters.")]
        public string? BuilderNameEnglish { get; set; }

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "ManagerMobileNo must be exactly 10 digits.")]
        public string? ManagerMobileNo { get; set; }

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "SecretaryMobileNo must be exactly 10 digits.")]
        public string? SecretaryMobileNo { get; set; }

        [EmailAddress(ErrorMessage = "SocietyEmailId is not a valid email address.")]
        [StringLength(100, ErrorMessage = "SocietyEmailId cannot exceed 100 characters.")]
        public string? SocietyEmailId { get; set; }

        [EmailAddress(ErrorMessage = "SecretaryEmailId is not a valid email address.")]
        [StringLength(100, ErrorMessage = "SecretaryEmailId cannot exceed 100 characters.")]
        public string? SecretaryEmailId { get; set; }

        [EmailAddress(ErrorMessage = "ManagerEmailId is not a valid email address.")]
        [StringLength(100, ErrorMessage = "ManagerEmailId cannot exceed 100 characters.")]
        public string? ManagerEmailId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Length must be between 0 and 100000.")]
        public decimal? LengthMtr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Width must be between 0 and 100000.")]
        public decimal? WidthMtr { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1000000, ErrorMessage = "Total Area must be between 0 and 1000000.")]
        public decimal? TotalAreaSqMtr { get; set; }

        // -- Common fields ------------------------------------------------------

        [Range(1, int.MaxValue, ErrorMessage = "CreatedBy must be a valid user Id.")]
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    }
}
