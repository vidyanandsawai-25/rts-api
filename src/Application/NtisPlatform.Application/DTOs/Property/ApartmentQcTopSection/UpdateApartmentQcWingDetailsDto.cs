using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Payload for PATCH / PUT <c>api/ApartmentQC/wing-details/{wingDetailId}</c>.
/// Updates fields only in the <c>PTIS.WingDetailsMast</c> table for a specific wing record.
/// All fields are optional — only non-null values will be updated.
/// </summary>
public class UpdateApartmentQcWingDetailsDto
{
    /// <example>A</example>
    [StringLength(30, ErrorMessage = "WingName cannot exceed 30 characters.")]
    public string? WingName { get; set; }

    /// <example>Ashwin D</example>
    [StringLength(200, ErrorMessage = "SecretaryName cannot exceed 200 characters.")]
    public string? SecretaryName { get; set; }

    /// <example>Ashwin Deshmukh</example>
    [StringLength(200, ErrorMessage = "SecretaryNameEnglish cannot exceed 200 characters.")]
    public string? SecretaryNameEnglish { get; set; }

    /// <example>8625085936</example>
    [StringLength(13, ErrorMessage = "SecretaryMobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "SecretaryMobileNo contains invalid characters.")]
    public string? SecretaryMobileNo { get; set; }

    /// <example>secretary@example.com</example>
    [StringLength(100, ErrorMessage = "SecretaryEmailId cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid secretary email format.")]
    public string? SecretaryEmailId { get; set; }

    /// <example>Shubham K</example>
    [StringLength(200, ErrorMessage = "ManagerName cannot exceed 200 characters.")]
    public string? ManagerName { get; set; }

    /// <example>Shubham Kadam</example>
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
    /// Checks if at least one field has been provided in the payload.
    /// </summary>
    public bool HasAnyField() =>
        WingName != null ||
        SecretaryName != null ||
        SecretaryNameEnglish != null ||
        SecretaryMobileNo != null ||
        SecretaryEmailId != null ||
        ManagerName != null ||
        ManagerNameEnglish != null ||
        ManagerMobileNo != null ||
        ManagerEmailId != null;
}
