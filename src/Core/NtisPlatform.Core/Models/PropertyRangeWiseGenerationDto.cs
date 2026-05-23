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
        public string? BuilderMobileNo { get; set; }
        public int? BuilderMobileRemarkId { get; set; }

        [StringLength(50, ErrorMessage = "CreateNewProperty_CSN_MaxLength")]
        public string? CSN { get; set; }

        [StringLength(200, ErrorMessage = "CreateNewProperty_SurveyRemark_MaxLength")]
        public string? SurveyRemark { get; set; }

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

        [Phone(ErrorMessage = "CreateNewProperty_AlternateMobileNo_Phone")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "CreateNewProperty_AlternateMobileNo_RegEx")]
        public string? AlternateMobileNo { get; set; }

        [Phone(ErrorMessage = "CreateNewProperty_OccupierMobileNo_Phone")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "CreateNewProperty_OccupierMobileNo_RegEx")]
        public string? OccupierMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_OccupierMobileNoRemarkId_RangeMax")]
        public int? OccupierMobileNoRemarkId { get; set; }

        [StringLength(50, ErrorMessage = "CreateNewProperty_PropertyNo_MaxLength")]
        public string? PropertyNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateNewProperty_PropertySeqNo_RangeMax")]
        public int? PropertySeqNo { get; set; }
        [StringLength(10, ErrorMessage = "CreateNewProperty_PartitionNo_MaxLength")]
        public string? PartitionNo { get; set; }
        public bool OpenPlot { get; set; }

        [StringLength(50, ErrorMessage = "CreateNewProperty_PlotNo_MaxLength")]
        public string? PlotNo { get; set; }

        [StringLength(5, ErrorMessage = "CreateNewProperty_Type_MaxLength")]
        public string? Type { get; set; }

        // -- Owner details ------------------------------------------------------

        [StringLength(20, ErrorMessage = "CreateNewProperty_OwnerTitle_MaxLength")]
        public string? OwnerTitle { get; set; }

        [StringLength(20, ErrorMessage = "CreateNewProperty_OwnerTitleEnglish_MaxLength")]
        public string? OwnerTitleEnglish { get; set; }

        [Required(ErrorMessage = "CreateNewProperty_OwnerName_Required")]
        [StringLength(1000, MinimumLength = 2, ErrorMessage = "CreateNewProperty_OwnerName_Length")]
        public string OwnerName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "CreateNewProperty_OwnerNameEnglish_MaxLength")]
        public string? OwnerNameEnglish { get; set; }

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "CreateNewProperty_MobileNo_RegEx")]
        public string? MobileNo { get; set; }

        [EmailAddress(ErrorMessage = "CreateNewProperty_EmailId_Email")]
        [StringLength(200, ErrorMessage = "CreateNewProperty_EmailId_MaxLength")]
        public string? EmailId { get; set; }

        // -- Occupier details ---------------------------------------------------

        [StringLength(20, ErrorMessage = "CreateNewProperty_OccupierTitle_MaxLength")]
        public string? OccupierTitle { get; set; }

        [StringLength(20, ErrorMessage = "CreateNewProperty_OccupierTitleEnglish_MaxLength")]
        public string? OccupierTitleEnglish { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_OccupierName_MaxLength")]
        public string? OccupierName { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_OccupierNameEnglish_MaxLength")]
        public string? OccupierNameEnglish { get; set; }

        // -- Flat / shop details ------------------------------------------------

        [StringLength(20, ErrorMessage = "CreateNewProperty_FlatOrShopNo_MaxLength")]
        public string? FlatOrShopNo { get; set; }

        [StringLength(20, ErrorMessage = "CreateNewProperty_FlatOrShopNoEnglish_MaxLength")]
        public string? FlatOrShopNoEnglish { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_FlatOrShopName_MaxLength")]
        public string? FlatOrShopName { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_FlatOrShopNameEnglish_MaxLength")]
        public string? FlatOrShopNameEnglish { get; set; }

        // -- Address details ----------------------------------------------------

        [StringLength(500, ErrorMessage = "CreateNewProperty_Address_MaxLength")]
        public string? Address { get; set; }

        [StringLength(500, ErrorMessage = "CreateNewProperty_AddressEnglish_MaxLength")]
        public string? AddressEnglish { get; set; }

        [StringLength(500, ErrorMessage = "CreateNewProperty_Location_MaxLength")]
        public string? Location { get; set; }

        [StringLength(200, ErrorMessage = "CreateNewProperty_LocationEnglish_MaxLength")]
        public string? LocationEnglish { get; set; }

        // -- Society details (Apartment category only) --------------------------

        [StringLength(1000, ErrorMessage = "CreateNewProperty_SocietyName_MaxLength")]
        public string? SocietyName { get; set; }

        [StringLength(500, ErrorMessage = "CreateNewProperty_SocietyNameEnglish_MaxLength")]
        public string? SocietyNameEnglish { get; set; }

        [StringLength(500, ErrorMessage = "CreateNewProperty_SocietyAddress_MaxLength")]
        public string? SocietyAddress { get; set; }

        [StringLength(500, ErrorMessage = "CreateNewProperty_SocietyAddressEnglish_MaxLength")]
        public string? SocietyAddressEnglish { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_SecretaryName_MaxLength")]
        public string? SecretaryName { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_SecretaryNameEnglish_MaxLength")]
        public string? SecretaryNameEnglish { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_ManagerName_MaxLength")]
        public string? ManagerName { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_ManagerNameEnglish_MaxLength")]
        public string? ManagerNameEnglish { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_LandOwnerName_MaxLength")]
        public string? LandOwnerName { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_LandOwnerNameEnglish_MaxLength")]
        public string? LandOwnerNameEnglish { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_BuilderName_MaxLength")]
        public string? BuilderName { get; set; }

        [StringLength(1000, ErrorMessage = "CreateNewProperty_BuilderNameEnglish_MaxLength")]
        public string? BuilderNameEnglish { get; set; }

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "CreateNewProperty_ManagerMobileNo_RegEx")]
        public string? ManagerMobileNo { get; set; }

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "CreateNewProperty_SecretaryMobileNo_RegEx")]
        public string? SecretaryMobileNo { get; set; }

        [EmailAddress(ErrorMessage = "CreateNewProperty_SocietyEmailId_Email")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_SocietyEmailId_MaxLength")]
        public string? SocietyEmailId { get; set; }

        [EmailAddress(ErrorMessage = "CreateNewProperty_SecretaryEmailId_Email")]
        [StringLength(1000, ErrorMessage = "CreateNewProperty_SecretaryEmailId_MaxLength")]
        public string? SecretaryEmailId { get; set; }

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
