using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for updating Property Society Details Tab
/// Used for the PUT /{propertyId}/society-details API endpoint
/// </summary>
public class UpdatePropertySocietyDetailsDto
{
    [Range(1, int.MaxValue, ErrorMessage = "WingId must be greater than 0.")]
    public int? WingId { get; set; }

    [StringLength(30, ErrorMessage = "WingName cannot exceed 30 characters.")]
    public string? WingName { get; set; }

    [StringLength(500, ErrorMessage = "SocietyName cannot exceed 500 characters.")]
    public string? SocietyName { get; set; }

    [StringLength(200, ErrorMessage = "SocietyAddress cannot exceed 200 characters.")]
    public string? SocietyAddress { get; set; }

    [StringLength(200, ErrorMessage = "SecretaryName cannot exceed 200 characters.")]
    public string? SecretaryName { get; set; }

    [StringLength(200, ErrorMessage = "ManagerName cannot exceed 200 characters.")]
    public string? ManagerName { get; set; }

    [StringLength(200, ErrorMessage = "LandOwnerName cannot exceed 200 characters.")]
    public string? LandOwnerName { get; set; }

    [StringLength(200, ErrorMessage = "BuilderName cannot exceed 200 characters.")]
    public string? BuilderName { get; set; }

    [StringLength(500, ErrorMessage = "SocietyNameEnglish cannot exceed 500 characters.")]
    public string? SocietyNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SocietyAddressEnglish cannot exceed 200 characters.")]
    public string? SocietyAddressEnglish { get; set; }

    [StringLength(200, ErrorMessage = "SecretaryNameEnglish cannot exceed 200 characters.")]
    public string? SecretaryNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "ManagerNameEnglish cannot exceed 200 characters.")]
    public string? ManagerNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "LandOwnerNameEnglish cannot exceed 200 characters.")]
    public string? LandOwnerNameEnglish { get; set; }

    [StringLength(200, ErrorMessage = "BuilderNameEnglish cannot exceed 200 characters.")]
    public string? BuilderNameEnglish { get; set; }

    [StringLength(13, ErrorMessage = "ManagerMobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "ManagerMobileNo contains invalid characters.")]
    public string? ManagerMobileNo { get; set; }

    [StringLength(13, ErrorMessage = "SecretaryMobileNo cannot exceed 13 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "SecretaryMobileNo contains invalid characters.")]
    public string? SecretaryMobileNo { get; set; }

    [StringLength(100, ErrorMessage = "SocietyEmailId cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "SocietyEmailId is not a valid email address.")]
    public string? SocietyEmailId { get; set; }

    [StringLength(100, ErrorMessage = "SecretaryEmailId cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "SecretaryEmailId is not a valid email address.")]
    public string? SecretaryEmailId { get; set; }

    [StringLength(100, ErrorMessage = "ManagerEmailId cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "ManagerEmailId is not a valid email address.")]
    public string? ManagerEmailId { get; set; }
}

