using System.ComponentModel.DataAnnotations;

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

        [Required(ErrorMessage = "CreateNewProperty_PropertyTypeId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_PropertyTypeId_RangeMax")]
        public int PropertyTypeId { get; set; }

        [Required(ErrorMessage = "CreateNewProperty_CategoryId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_CategoryId_RangeMax")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "CreateNewProperty_TaxZoneId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_TaxZoneId_RangeMax")]
        public int TaxZoneId { get; set; }

        [Required(ErrorMessage = "CreateNewProperty_WardId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_WardId_RangeMax")]
        public int WardId { get; set; }
       
        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(50, ErrorMessage = "CreateNewProperty_CSN_MaxLength")]
        public string? CSN { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(200, ErrorMessage = "CreateNewProperty_SurveyRemark_MaxLength")]
        public string? SurveyRemark { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(50, ErrorMessage = "CreateNewProperty_BlockNo_MaxLength")]
        public string? BlockNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_PropertyMastOldId_RangeMax")]
        public int? PropertyMastOldId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_SocietyDetailId_RangeMax")]
        public int? SocietyDetailId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_PropertyAssessmentStatusId_RangeMax")]
        public int? PropertyAssessmentStatusId { get; set; }

        [StringLength(6, MinimumLength = 6, ErrorMessage = "CreateNewProperty_PinCode_Length")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "CreateNewProperty_PinCode_RegEx")]
        public string? PinCode { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_MobileNoRemarkId_RangeMax")]
        public int? MobileNoRemarkId { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [Phone(ErrorMessage = "CreateNewProperty_AlternateMobileNo_Phone")]
        [StringLength(13, ErrorMessage = "CreateNewProperty_AlternateMobileNo_MaxLength")]
        public string? AlternateMobileNo { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [Phone(ErrorMessage = "CreateNewProperty_OccupierMobileNo_Phone")]
        [StringLength(13, ErrorMessage = "CreateNewProperty_OccupierMobileNo_MaxLength")]
        public string? OccupierMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_OccupierMobileNoRemarkId_RangeMax")]
        public int? OccupierMobileNoRemarkId { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(50, ErrorMessage = "CreateNewProperty_PropertyNo_MaxLength")]
        public string? PropertyNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_PropertySeqNo_RangeMax")]
        public int? PropertySeqNo { get; set; }

        public bool OpenPlot { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(50, ErrorMessage = "CreateNewProperty_PlotNo_MaxLength")]
        public string? PlotNo { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(5, ErrorMessage = "CreateNewProperty_Type_MaxLength")]
        public string? Type { get; set; }

        // -- Owner details ------------------------------------------------------

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(20, ErrorMessage = "CreateNewProperty_OwnerTitle_MaxLength")]
        public string? OwnerTitle { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(20, ErrorMessage = "CreateNewProperty_OwnerTitleEnglish_MaxLength")]
        public string? OwnerTitleEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [Required(ErrorMessage = "CreateNewProperty_OwnerName_Required")]
        [StringLength(1000, MinimumLength = 2, ErrorMessage = "CreateNewProperty_OwnerName_Length")]
        public string OwnerName { get; set; } = string.Empty;

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_OwnerNameEnglish_MaxLength")]
        public string? OwnerNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(13, ErrorMessage = "CreateNewProperty_MobileNo_MaxLength")]
        public string? MobileNo { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [EmailAddress(ErrorMessage = "CreateNewProperty_EmailId_Email")]
        [StringLength(200, ErrorMessage = "CreateNewProperty_EmailId_MaxLength")]
        public string? EmailId { get; set; }

        // -- Occupier details ---------------------------------------------------

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(20, ErrorMessage = "CreateNewProperty_OccupierTitle_MaxLength")]
        public string? OccupierTitle { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(20, ErrorMessage = "CreateNewProperty_OccupierTitleEnglish_MaxLength")]
        public string? OccupierTitleEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_OccupierName_MaxLength")]
        public string? OccupierName { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_OccupierNameEnglish_MaxLength")]
        public string? OccupierNameEnglish { get; set; }

        // -- Flat / shop details ------------------------------------------------

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(20, ErrorMessage = "CreateNewProperty_FlatOrShopNo_MaxLength")]
        public string? FlatOrShopNo { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(20, ErrorMessage = "CreateNewProperty_FlatOrShopNoEnglish_MaxLength")]
        public string? FlatOrShopNoEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_FlatOrShopName_MaxLength")]
        public string? FlatOrShopName { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_FlatOrShopNameEnglish_MaxLength")]
        public string? FlatOrShopNameEnglish { get; set; }

        // -- Address details ----------------------------------------------------

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(500, ErrorMessage = "CreateNewProperty_Address_MaxLength")]
        public string? Address { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(500, ErrorMessage = "CreateNewProperty_AddressEnglish_MaxLength")]
        public string? AddressEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(500, ErrorMessage = "CreateNewProperty_Location_MaxLength")]
        public string? Location { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(200, ErrorMessage = "CreateNewProperty_LocationEnglish_MaxLength")]
        public string? LocationEnglish { get; set; }

        // -- Society details (Apartment category only) --------------------------

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_SocietyName_MaxLength")]
        public string? SocietyName { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(500, ErrorMessage = "CreateNewProperty_SocietyNameEnglish_MaxLength")]
        public string? SocietyNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(500, ErrorMessage = "CreateNewProperty_SocietyAddress_MaxLength")]
        public string? SocietyAddress { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(500, ErrorMessage = "CreateNewProperty_SocietyAddressEnglish_MaxLength")]
        public string? SocietyAddressEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_SecretaryName_MaxLength")]
        public string? SecretaryName { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_SecretaryNameEnglish_MaxLength")]
        public string? SecretaryNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_ManagerName_MaxLength")]
        public string? ManagerName { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_ManagerNameEnglish_MaxLength")]
        public string? ManagerNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_LandOwnerName_MaxLength")]
        public string? LandOwnerName { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_LandOwnerNameEnglish_MaxLength")]
        public string? LandOwnerNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_BuilderName_MaxLength")]
        public string? BuilderName { get; set; }
        public string? BuilderMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_BuilderMobileNoRemarkId_RangeMax")]
        public int? BuilderMobileNoRemarkId { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_BuilderNameEnglish_MaxLength")]
        public string? BuilderNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(13, ErrorMessage = "CreateNewProperty_ManagerMobileNo_MaxLength")]
        public string? ManagerMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_ManagerMobileNoRemarkId_RangeMax")]
        public int? ManagerMobileNoRemarkId { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [StringLength(13, ErrorMessage = "CreateNewProperty_SecretaryMobileNo_MaxLength")]
        public string? SecretaryMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_SecretaryMobileNoRemarkId_RangeMax")]
        public int? SecretaryMobileNoRemarkId { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [EmailAddress(ErrorMessage = "CreateNewProperty_SocietyEmailId_Email")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_SocietyEmailId_MaxLength")]
        public string? SocietyEmailId { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [EmailAddress(ErrorMessage = "CreateNewProperty_SecretaryEmailId_Email")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_SecretaryEmailId_MaxLength")]
        public string? SecretaryEmailId { get; set; }

        [RegularExpression(@"^[^<>;|~]*$", ErrorMessage = "CreateNewProperty_InvalidCharacters")]
        [EmailAddress(ErrorMessage = "CreateNewProperty_ManagerEmailId_Email")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_ManagerEmailId_MaxLength")]
        public string? ManagerEmailId { get; set; }
        public double? LengthMtr { get; set; }

 
        public double? WidthMtr { get; set; }

   
        public double? TotalAreaSqMtr { get; set; }


        // -- Common fields ------------------------------------------------------

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_CreatedBy_RangeMax")]
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

    }
}
