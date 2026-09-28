namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Internal data-transfer record passed from <c>ApartmentQcTopSectionRepository</c> to
/// <c>ApartmentQcTopSectionService</c>. Never exposed directly to API consumers.
/// </summary>
public sealed record PropertyTopSectionRawData
{
    public int Id { get; init; }
    public string Upic { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsLocked { get; init; }
    public int WardId { get; init; }
    public string? WardNo { get; init; }
    public string? PropertyNo { get; init; }
    public string? PartitionNo { get; init; }
    public string? OwnerName { get; init; }
    public string? PlotNo { get; init; }
    public string? Csn { get; init; }
    public string? Address { get; init; }
    public string? PinCode { get; init; }
    public string? MobileNo { get; init; }
    public string? AlternateMobileNo { get; init; }
    public string? EmailId { get; init; }
    public string? AadharCardNo { get; init; }
    public string? PropertyCategoryName { get; init; }
    public string? PropertyDescription { get; init; }
    public string? TaxZoneNo { get; init; }
    public string? TaxZoneRemark { get; init; }
    public string? DivisionName { get; init; }
    public string? MoujaName { get; init; }
    public string? SecretaryName { get; init; }
    public string? SecretaryNameEnglish { get; init; }
    public string? SecretaryMobileNo { get; init; }
    public string? SecretaryEmailId { get; init; }
    public string? ManagerName { get; init; }
    public string? ManagerNameEnglish { get; init; }
    public string? ManagerMobileNo { get; init; }
    public string? ManagerEmailId { get; init; }
    public string? SocietyName { get; init; }
    public string? SocietyNameEnglish { get; init; }
    public string? SocietyAddress { get; init; }
    public string? SocietyAddressEnglish { get; init; }
    public string? SocietyEmailId { get; init; }
    public string? LandOwnerName { get; init; }
    public string? LandOwnerNameEnglish { get; init; }
    public string? BuilderName { get; init; }
    public string? BuilderNameEnglish { get; init; }
    public string? BuilderMobileNo { get; init; }
    public string? OwnerNameEnglish { get; init; }
    public string? OccupierName { get; init; }
    public string? OccupierNameEnglish { get; init; }
    public string? OwnerCategory { get; init; }
    public Guid? SocietyBuildingPhotoGuid { get; init; }
}
