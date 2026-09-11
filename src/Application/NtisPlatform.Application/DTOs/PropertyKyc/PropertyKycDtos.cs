using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.DTOs.PropertyKyc;

/// <summary>
/// Represents virtual property transfer history details.
/// </summary>
public class VirtualPropertyTransferHistoryDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int TransferredWardId { get; set; }
    public string? TransferredPropertyNo { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

/// <summary>
/// Represents common KYC details for a property in the exact requested sequence.
/// </summary>
public class PropertyKycDetailsCommonDto
{

    public int PropertyId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int? PropertyTypeId { get; set; }
    public int? CategoryId { get; set; }
    public string? PlotNo { get; set; }
    public string? CSN { get; set; }
    public int? OwnerTypeId { get; set; }
    public string? AdharCardNo { get; set; }
    public string? BlockNo { get; set; }
    public string? SurveyRemark { get; set; }
    public int? TypeOfUseId { get; set; }
    public string? OwnerType { get; set; }
    public string? OldCSN { get; set; }
    public string? OwnerTitle { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerTitleEnglish { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierTitle { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierTitleEnglish { get; set; }
    public string? OccupierNameEnglish { get; set; }
    public string? Address { get; set; }
    public string? Location { get; set; }
    public string? AddressEnglish { get; set; }
    public string? LocationEnglish { get; set; }
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNameEnglish { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? FlatOrShopNoEnglish { get; set; }
    public string? MobileNo { get; set; }
    public int? MobileNoRemarkId { get; set; }
    public string? AlternateMobileNo { get; set; }
    public string? OccupierMobileNo { get; set; }
    public int? OccupierMobileNoRemarkId { get; set; }
    public string? EmailId { get; set; }
    public string? PinCode { get; set; }
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
    public int? SocietyDetailId { get; set; }
    public double? PlotLength { get; set; }
    public double? PlotWidth { get; set; }
    public double? TotalArea { get; set; }
    public string? IssuedBy { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldSocietyName { get; set; }
    public List<VirtualPropertyTransferHistoryDto> VirtualPropertyTransferHistory { get; set; } = new();
    public int MapCount { get; set; }
    public int? PropertyIdOld { get; set; }
}
