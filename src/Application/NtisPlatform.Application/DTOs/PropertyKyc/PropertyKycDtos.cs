using NtisPlatform.Core.Models;
namespace NtisPlatform.Application.DTOs.PropertyKyc;


/// <summary>
/// Represents common KYC details for a property.
/// </summary>
public class PropertyKycDetailsCommonDto : PropertyKycDetailsDto
{
    /// <summary>
    /// Property identifier.
    /// </summary>
    public int? PropertyTypeId { get; set; }
    public int? CategoryId { get; set; }
    public string? PlotNo { get; set; }
    public string? CSN { get; set; }
    public string? BlockNo { get; set; }
    public string? SurveyRemark { get; set; }
    public string? OldCSN { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldSocietyName { get; set; }
    public int? MobileNoRemarkId { get; set; }
    public string? OccupierMobileNo { get; set; }
    public int? OccupierMobileNoRemarkId { get; set; }
    public int? SocietyDetailId { get; set; }
    public string? SocietyName { get; set; }
    public string? SocietyAddress { get; set; }
    public string? SocietyNameEnglish { get; set; }
    public string? SocietyAddressEnglish { get; set; }
    public string? SocietyEmailId { get; set; }
    public int? WingId { get; set; }
    public string? WingNo { get; set; }
    public string? WingName { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerNameEnglish { get; set; }
    public string? ManagerMobileNo { get; set; }
    public int? ManagerMobileNoId { get; set; }
    public string? ManagerEmailId { get; set; }
    public string? SecretaryName { get; set; }
    public string? SecretaryNameEnglish { get; set; }
    public string? SecretaryMobileNo { get; set; }
    public int? SecretaryMobileNoId { get; set; }
    public string? SecretaryEmailId { get; set; }
    public string? LandOwnerName { get; set; }
    public string? LandOwnerNameEnglish { get; set; }
    public string? BuilderName { get; set; }
    public string? BuilderNameEnglish { get; set; }
    public string? BuilderMobileNo { get; set; }
    public int? BuilderMobileNoId { get; set; }
    public double? PlotLength { get; set; }
    public double? PlotWidth { get; set; }
    public double? TotalArea { get; set; }
    public string? IssuedBy { get; set; }
}