using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.ULBMaster;

/// <summary>
/// ULB Master DTO for read operations
/// </summary>
public class ULBMasterDto : BaseDtos
{
    public int UlbId { get; set; }
    public string UlbCode { get; set; } = string.Empty;
    public string UlbName { get; set; } = string.Empty;
    public string? UlbNameLocal { get; set; }
    public byte UlbTypeId { get; set; }
    public string? UlbLogo { get; set; }
    public string? EmailId { get; set; }
    public string? MobileNo { get; set; }
    public string? AlternateMobileNo { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPersonDesignation { get; set; }
    public string? UlbAddress { get; set; }
    public string? State { get; set; }
    public string? District { get; set; }
    public string? PinCode { get; set; }
    public DateTime? ProjectStartDate { get; set; }
    public DateTime? FinancialYearStartDate { get; set; }
    public DateTime? ExpectedGoLiveDate { get; set; }
    public string? PartnerName { get; set; }
    public string? PMName { get; set; }
    public string? PMEmailId { get; set; }
    public string? PMMobileNo { get; set; }
    public string? LicenceType { get; set; }
    public DateTime? LicenceStartDate { get; set; }
    public DateTime? LicenceEndDate { get; set; }
    public string? LicenceDuration { get; set; }
    public string? SupportType { get; set; }
    public string? LicenceKey { get; set; }
}

/// <summary>
/// DTO for creating new ULB Master
/// </summary>
public class CreateULBMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "UlbCode_Required")]
    [StringLength(50, ErrorMessage = "UlbCode_MaxLen_50")]
    public string UlbCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "UlbName_Required")]
    [StringLength(200, ErrorMessage = "UlbName_MaxLen_200")]
    public string UlbName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "UlbNameLocal_MaxLen_200")]
    public string? UlbNameLocal { get; set; }

    [Required(ErrorMessage = "UlbTypeId_Required")]
    public byte UlbTypeId { get; set; }

    [StringLength(500, ErrorMessage = "UlbLogo_MaxLen_500")]
    public string? UlbLogo { get; set; }

    [EmailAddress(ErrorMessage = "Email_Invalid_Format")]
    [StringLength(200, ErrorMessage = "Email_MaxLen_200")]
    public string? EmailId { get; set; }
    

    [StringLength(20, ErrorMessage = "PhoneNo_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "PhoneNo_Invalid_Format")]
    public string? MobileNo { get; set; }
    

    [StringLength(20, ErrorMessage = "AlternatePhoneNo_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AlternatePhoneNo_Invalid_Format")]
    public string? AlternateMobileNo { get; set; }

    [StringLength(200, ErrorMessage = "WebsiteUrl_MaxLen_200")]
    [Url(ErrorMessage = "WebsiteUrl_Invalid_Format")]
    public string? WebsiteUrl { get; set; }

    [StringLength(200, ErrorMessage = "ContactPersonName_MaxLen_200")]
    public string? ContactPersonName { get; set; }

    [StringLength(200, ErrorMessage = "ContactPersonDesignation_MaxLen_200")]
    public string? ContactPersonDesignation { get; set; }

    [StringLength(500, ErrorMessage = "UlbAddress_MaxLen_500")]
    public string? UlbAddress { get; set; }

    [StringLength(100, ErrorMessage = "State_MaxLen_100")]
    public string? State { get; set; }

    [StringLength(100, ErrorMessage = "District_MaxLen_100")]
    public string? District { get; set; }

    [StringLength(6, MinimumLength = 6, ErrorMessage = "PinCode_Length_6")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "PinCode_Invalid_Format")]
    public string? PinCode { get; set; }

    public DateTime? ProjectStartDate { get; set; }
    public DateTime? FinancialYearStartDate { get; set; }
    public DateTime? ExpectedGoLiveDate { get; set; }

    [StringLength(200, ErrorMessage = "PartnerName_MaxLen_200")]
    public string? PartnerName { get; set; }

    [StringLength(200, ErrorMessage = "PMName_MaxLen_200")]
    public string? PMName { get; set; }

    [EmailAddress(ErrorMessage = "PMEmail_Invalid_Format")]
    [StringLength(200, ErrorMessage = "PMEmail_MaxLen_200")]
    public string? PMEmailId { get; set; }

    [StringLength(20, ErrorMessage = "PMContactNo_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "PMContactNo_Invalid_Format")]
    public string? PMMobileNo { get; set; }

    [StringLength(50, ErrorMessage = "LicenceType_MaxLen_50")]
    public string? LicenceType { get; set; }

    public DateTime? LicenceStartDate { get; set; }
    public DateTime? LicenceEndDate { get; set; }

    [StringLength(50, ErrorMessage = "LicenceDuration_MaxLen_50")]
    public string? LicenceDuration { get; set; }

    [StringLength(100, ErrorMessage = "SupportType_MaxLen_100")]
    public string? SupportType { get; set; }

    [StringLength(500, ErrorMessage = "LicenceKey_MaxLen_500")]
    public string? LicenceKey { get; set; } 
}

/// <summary>
/// DTO for updating existing ULB Master
/// </summary>
public class UpdateULBMasterDto: UpdateBaseDtos
{
    [Required(ErrorMessage = "UlbCode_Required")]
    [StringLength(50, ErrorMessage = "UlbCode_MaxLen_50")]
    public string UlbCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "UlbName_Required")]
    [StringLength(200, ErrorMessage = "UlbName_MaxLen_200")]
    public string UlbName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "UlbNameLocal_MaxLen_200")]
    public string? UlbNameLocal { get; set; }

    [Required(ErrorMessage = "UlbTypeId_Required")]
    public byte UlbTypeId { get; set; }

    [StringLength(500, ErrorMessage = "UlbLogo_MaxLen_500")]
    public string? UlbLogo { get; set; }

    [EmailAddress(ErrorMessage = "Email_Invalid_Format")]
    [StringLength(200, ErrorMessage = "Email_MaxLen_200")]
    public string? EmailId { get; set; }

    [StringLength(20, ErrorMessage = "PhoneNo_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "PhoneNo_Invalid_Format")]
    public string? MobileNo { get; set; }

    [StringLength(20, ErrorMessage = "AlternatePhoneNo_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AlternatePhoneNo_Invalid_Format")]
    public string? AlternateMobileNo { get; set; }

    [StringLength(200, ErrorMessage = "WebsiteUrl_MaxLen_200")]
    [Url(ErrorMessage = "WebsiteUrl_Invalid_Format")]
    public string? WebsiteUrl { get; set; }

    [StringLength(200, ErrorMessage = "ContactPersonName_MaxLen_200")]
    public string? ContactPersonName { get; set; }

    [StringLength(200, ErrorMessage = "ContactPersonDesignation_MaxLen_200")]
    public string? ContactPersonDesignation { get; set; }

    [StringLength(500, ErrorMessage = "UlbAddress_MaxLen_500")]
    public string? UlbAddress { get; set; }

    [StringLength(100, ErrorMessage = "State_MaxLen_100")]
    public string? State { get; set; }

    [StringLength(100, ErrorMessage = "District_MaxLen_100")]
    public string? District { get; set; }

    [StringLength(6, MinimumLength = 6, ErrorMessage = "PinCode_Length_6")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "PinCode_Invalid_Format")]
    public string? PinCode { get; set; }

    public DateTime? ProjectStartDate { get; set; }
    public DateTime? FinancialYearStartDate { get; set; }
    public DateTime? ExpectedGoLiveDate { get; set; }

    [StringLength(200, ErrorMessage = "PartnerName_MaxLen_200")]
    public string? PartnerName { get; set; }

    [StringLength(200, ErrorMessage = "PMName_MaxLen_200")]
    public string? PMName { get; set; }

    [EmailAddress(ErrorMessage = "PMEmail_Invalid_Format")]
    [StringLength(200, ErrorMessage = "PMEmail_MaxLen_200")]
    public string? PMEmailId { get; set; }

    [StringLength(20, ErrorMessage = "PMContactNo_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "PMContactNo_Invalid_Format")]
    public string? PMMobileNo { get; set; }

    [StringLength(50, ErrorMessage = "LicenceType_MaxLen_50")]
    public string? LicenceType { get; set; }

    public DateTime? LicenceStartDate { get; set; }
    public DateTime? LicenceEndDate { get; set; }

    [StringLength(50, ErrorMessage = "LicenceDuration_MaxLen_50")]
    public string? LicenceDuration { get; set; }

    [StringLength(100, ErrorMessage = "SupportType_MaxLen_100")]
    public string? SupportType { get; set; }

    [StringLength(500, ErrorMessage = "LicenceKey_MaxLen_500")]
    public string? LicenceKey { get; set; } 
}
