using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models
{
    public class UpdateAllPropertyDetailsResponseDto
    {
        public int PropertyId { get; set; }
        public string? UPICID { get; set; }
        public string? Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
    }

    public class UpdateAllPropertyDetailsDto
    {
        // -- Property basic details ---------------------------------------------

        [Required(ErrorMessage = "UpdateAllPropertyDetails_PropertyTypeId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_PropertyTypeId_RangeMax")]
        public int PropertyTypeId { get; set; }

        [Required(ErrorMessage = "UpdateAllPropertyDetails_CategoryId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_CategoryId_RangeMax")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "UpdateAllPropertyDetails_TaxZoneId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_TaxZoneId_RangeMax")]
        public int TaxZoneId { get; set; } = 1;

        [Required(ErrorMessage = "UpdateAllPropertyDetails_WardId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_WardId_RangeMax")]
        public int WardId { get; set; }

        [RegularExpression(@"^\+\d{1,3}[6-9]\d{9}$", ErrorMessage = "UpdateAllPropertyDetails_BuilderMobileNo_RegEx")]
        public string? BuilderMobileNo { get; set; }

        public int? BuilderMobileNoRemarkId { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? CSN { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? SurveyRemark { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? BlockNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_PropertyAssessmentStatusId_RangeMax")]
        public int? PropertyAssessmentStatusId { get; set; }

        [RegularExpression(@"^\d{6}$", ErrorMessage = "UpdateAllPropertyDetails_PinCode_RegEx")]
        public string? PinCode { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_MobileNoRemarkId_RangeMax")]
        public int? MobileNoRemarkId { get; set; }

        [RegularExpression(@"^\+\d{1,3}[6-9]\d{9}$", ErrorMessage = "UpdateAllPropertyDetails_AlternateMobileNo_RegEx")]
        public string? AlternateMobileNo { get; set; }

        [RegularExpression(@"^\+\d{1,3}[6-9]\d{9}$", ErrorMessage = "UpdateAllPropertyDetails_OccupierMobileNo_RegEx")]
        public string? OccupierMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_OccupierMobileNoRemarkId_RangeMax")]
        public int? OccupierMobileNoRemarkId { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? PropertyNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_PropertySeqNo_RangeMax")]
        public int? PropertySeqNo { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? PartitionNo { get; set; }

        public bool OpenPlot { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? PlotNo { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? Type { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? PartType { get; set; }

        // -- Owner details ------------------------------------------------------

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OwnerTitle { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OwnerTitleEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OwnerName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OwnerNameEnglish { get; set; }

        [RegularExpression(@"^\+\d{1,3}[6-9]\d{9}$", ErrorMessage = "UpdateAllPropertyDetails_MobileNo_RegEx")]
        public string? MobileNo { get; set; }

        [EmailAddress(ErrorMessage = "UpdateAllPropertyDetails_EmailId_Email")]
        public string? EmailId { get; set; }

        // -- Occupier details ---------------------------------------------------

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OccupierTitle { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OccupierTitleEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OccupierName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? OccupierNameEnglish { get; set; }

        // -- Flat / shop details ------------------------------------------------

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? FlatOrShopNo { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? FlatOrShopNoEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? FlatOrShopName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? FlatOrShopNameEnglish { get; set; }

        // -- Address details ----------------------------------------------------

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? Address { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? AddressEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? Location { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? LocationEnglish { get; set; }

        // -- Society details ----------------------------------------------------

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? SocietyName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? SocietyNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? SocietyAddress { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? SocietyAddressEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? SecretaryName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? SecretaryNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? ManagerName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? ManagerNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? LandOwnerName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? LandOwnerNameEnglish { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? BuilderName { get; set; }

        [RegularExpression(@"^[^<>;|~=]*$", ErrorMessage = "UpdateAllPropertyDetails_InvalidCharacters")]
        public string? BuilderNameEnglish { get; set; }

        [RegularExpression(@"^\+\d{1,3}[6-9]\d{9}$", ErrorMessage = "UpdateAllPropertyDetails_ManagerMobileNo_RegEx")]
        public string? ManagerMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_ManagerMobileNoRemarkId_RangeMax")]
        public int? ManagerMobileNoRemarkId { get; set; }

        [RegularExpression(@"^\+\d{1,3}[6-9]\d{9}$", ErrorMessage = "UpdateAllPropertyDetails_SecretaryMobileNo_RegEx")]
        public string? SecretaryMobileNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_SecretaryMobileNoRemarkId_RangeMax")]
        public int? SecretaryMobileNoRemarkId { get; set; }

        [EmailAddress(ErrorMessage = "UpdateAllPropertyDetails_SocietyEmailId_Email")]
        public string? SocietyEmailId { get; set; }

        [EmailAddress(ErrorMessage = "UpdateAllPropertyDetails_SecretaryEmailId_Email")]
        public string? SecretaryEmailId { get; set; }

        [EmailAddress(ErrorMessage = "UpdateAllPropertyDetails_ManagerEmailId_Email")]
        public string? ManagerEmailId { get; set; }

        // -- Room wise Sub ------------------------------------------------------

        public double? LengthMtr { get; set; }
        public double? WidthMtr { get; set; }
        public double? TotalAreaSqMtr { get; set; }
        public int FloorId { get; set; }
        public int ConstructionTypeId { get; set; }
        public int TypeOfUseId { get; set; }
        public bool IsActive { get; set; }

        // -- Common fields ------------------------------------------------------

        [Range(1, int.MaxValue, ErrorMessage = "UpdateAllPropertyDetails_UpdatedBy_RangeMax")]
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
