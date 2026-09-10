using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Payload for PATCH / PUT <c>api/ApartmentQcTopSection/{propertyId}</c>.
/// All fields are optional for partial updates (PATCH) — only non-null values will be updated in the database.
/// </summary>
public class UpdateApartmentQcTopSectionDto
{
    // ===== Owner & Occupier =====
    /// <example>राजेश शर्मा</example>
    [StringLength(1000, ErrorMessage = "OwnerName cannot exceed 1000 characters.")]
    public string? OwnerName { get; set; }

    /// <example>Rajesh Sharma</example>
    [StringLength(1000, ErrorMessage = "OwnerNameEnglish cannot exceed 1000 characters.")]
    public string? OwnerNameEnglish { get; set; }

    /// <example>सुनील वर्मा</example>
    [StringLength(1000, ErrorMessage = "OccupierName cannot exceed 1000 characters.")]
    public string? OccupierName { get; set; }

    /// <example>Sunil Verma</example>
    [StringLength(1000, ErrorMessage = "OccupierNameEnglish cannot exceed 1000 characters.")]
    public string? OccupierNameEnglish { get; set; }

    // ===== Contact Details =====
    /// <example>9876543210</example>
    [StringLength(13, ErrorMessage = "MobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "MobileNo contains invalid characters.")]
    public string? MobileNo { get; set; }

    /// <example>9123456780</example>
    [StringLength(13, ErrorMessage = "AlternateMobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AlternateMobileNo contains invalid characters.")]
    public string? AlternateMobileNo { get; set; }

    /// <example>user@example.com</example>
    [StringLength(100, ErrorMessage = "EmailId cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format.")]
    public string? EmailId { get; set; }

    // ===== Address & Identification =====
    /// <example>WING-B, FLAT NO-301, NAGNATH APARTMENT</example>
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string? Address { get; set; }

    /// <example>400612</example>
    [StringLength(10, ErrorMessage = "PinCode cannot exceed 10 characters.")]
    public string? PinCode { get; set; }

    /// <example>101</example>
    [StringLength(100, ErrorMessage = "PlotNo cannot exceed 100 characters.")]
    public string? PlotNo { get; set; }

    /// <example>45/2</example>
    [StringLength(100, ErrorMessage = "SurveyNo cannot exceed 100 characters.")]
    public string? SurveyNo { get; set; }

    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "MoujaId must be a positive integer.")]
    public int? MoujaId { get; set; }

    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "TaxZoneId must be a positive integer.")]
    public int? TaxZoneId { get; set; }

    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a positive integer.")]
    public int? CategoryId { get; set; }

    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "PropertyTypeId must be a positive integer.")]
    public int? PropertyTypeId { get; set; }

    // ===== Assessment / Identity =====
    /// <example>123456789012</example>
    [StringLength(12, ErrorMessage = "AadharCardNo cannot exceed 12 characters.")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "AadharCardNo must be a 12-digit numeric number.")]
    public string? AadharNo { get; set; }

    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "OwnerTypeId must be a positive integer.")]
    public int? OwnerTypeId { get; set; }

    // ===== Society Details =====
    /// <example>गोकुलधाम</example>
    [StringLength(500, ErrorMessage = "SocietyName cannot exceed 500 characters.")]
    public string? SocietyName { get; set; }

    /// <example>Gokuldham Co-Op Hsg</example>
    [StringLength(500, ErrorMessage = "SocietyNameEnglish cannot exceed 500 characters.")]
    public string? SocietyNameEnglish { get; set; }

    /// <example>Plot 12, Sector 4, Gokuldham</example>
    [StringLength(200, ErrorMessage = "SocietyAddress cannot exceed 200 characters.")]
    public string? SocietyAddress { get; set; }

    /// <example>Plot 12, Sector 4, Gokuldham</example>
    [StringLength(200, ErrorMessage = "SocietyAddressEnglish cannot exceed 200 characters.")]
    public string? SocietyAddressEnglish { get; set; }

    /// <example>society@example.com</example>
    [StringLength(100, ErrorMessage = "SocietyEmailId cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid society email format.")]
    public string? SocietyEmailId { get; set; }

    /// <example>रामराव पाटील</example>
    [StringLength(200, ErrorMessage = "LandOwnerName cannot exceed 200 characters.")]
    public string? LandOwnerName { get; set; }

    /// <example>Ramrao Patil</example>
    [StringLength(200, ErrorMessage = "LandOwnerNameEnglish cannot exceed 200 characters.")]
    public string? LandOwnerNameEnglish { get; set; }

    /// <example>के. बी. बिल्डर्स</example>
    [StringLength(200, ErrorMessage = "BuilderName cannot exceed 200 characters.")]
    public string? BuilderName { get; set; }

    /// <example>K. B. Builders</example>
    [StringLength(200, ErrorMessage = "BuilderNameEnglish cannot exceed 200 characters.")]
    public string? BuilderNameEnglish { get; set; }

    /// <example>9820011223</example>
    [StringLength(13, ErrorMessage = "BuilderMobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "BuilderMobileNo contains invalid characters.")]
    public string? BuilderMobileNo { get; set; }

    /// <example>A. Bhide</example>
    [StringLength(200, ErrorMessage = "SecretaryName cannot exceed 200 characters.")]
    public string? SecretaryName { get; set; }

    /// <example>A. Bhide</example>
    [StringLength(200, ErrorMessage = "SecretaryNameEnglish cannot exceed 200 characters.")]
    public string? SecretaryNameEnglish { get; set; }

    /// <example>9820011221</example>
    [StringLength(13, ErrorMessage = "SecretaryMobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "SecretaryMobileNo contains invalid characters.")]
    public string? SecretaryMobileNo { get; set; }

    /// <example>secretary@example.com</example>
    [StringLength(100, ErrorMessage = "SecretaryEmailId cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid secretary email format.")]
    public string? SecretaryEmailId { get; set; }

    /// <example>प्रवीण देशमुख</example>
    [StringLength(200, ErrorMessage = "ManagerName cannot exceed 200 characters.")]
    public string? ManagerName { get; set; }

    /// <example>Pravin Deshmukh</example>
    [StringLength(200, ErrorMessage = "ManagerNameEnglish cannot exceed 200 characters.")]
    public string? ManagerNameEnglish { get; set; }

    /// <example>9820011222</example>
    [StringLength(13, ErrorMessage = "ManagerMobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "ManagerMobileNo contains invalid characters.")]
    public string? ManagerMobileNo { get; set; }

    /// <example>manager@example.com</example>
    [StringLength(100, ErrorMessage = "ManagerEmailId cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid manager email format.")]
    public string? ManagerEmailId { get; set; }

    /// <summary>
    /// Optional list of specific WingDetailsMast.Id values (from frontend checkboxes) to replicate
    /// Secretary updates to. If empty or omitted, Secretary changes apply only to SocietyDetailsMast.
    /// </summary>
    public List<int>? SecretaryTargetWingDetailIds { get; set; }

    /// <summary>
    /// Optional list of specific WingDetailsMast.Id values (from frontend checkboxes) to replicate
    /// Manager updates to. If empty or omitted, Manager changes apply only to SocietyDetailsMast.
    /// </summary>
    public List<int>? ManagerTargetWingDetailIds { get; set; }


    /// <summary>
    /// Checks if at least one field has been provided in the payload.
    /// </summary>
    public bool HasAnyField() =>
        OwnerName != null ||
        OwnerNameEnglish != null ||
        OccupierName != null ||
        OccupierNameEnglish != null ||
        MobileNo != null ||
        AlternateMobileNo != null ||
        EmailId != null ||
        Address != null ||
        PinCode != null ||
        PlotNo != null ||
        SurveyNo != null ||
        MoujaId.HasValue ||
        TaxZoneId.HasValue ||
        CategoryId.HasValue ||
        PropertyTypeId.HasValue ||
        AadharNo != null ||
        OwnerTypeId.HasValue ||
        SocietyName != null ||
        SocietyNameEnglish != null ||
        SocietyAddress != null ||
        SocietyAddressEnglish != null ||
        SocietyEmailId != null ||
        LandOwnerName != null ||
        LandOwnerNameEnglish != null ||
        BuilderName != null ||
        BuilderNameEnglish != null ||
        BuilderMobileNo != null ||
        SecretaryName != null ||
        SecretaryNameEnglish != null ||
        SecretaryMobileNo != null ||
        SecretaryEmailId != null ||
        ManagerName != null ||
        ManagerNameEnglish != null ||
        ManagerMobileNo != null ||
        ManagerEmailId != null;
}


