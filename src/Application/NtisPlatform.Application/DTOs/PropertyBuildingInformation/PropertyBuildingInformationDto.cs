namespace NtisPlatform.Application.DTOs.PropertyBuildingInformation;

public class PropertyBuildingInformationDto
{
    public int PropertyId { get; set; }
    public int? Id { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldWing { get; set; }
    public string? OldFlatOrShopNumber { get; set; }
    public int? OldPropertyTypeId { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldMobileNo { get; set; }
    public decimal? OldRV { get; set; }
    public decimal? OldTotalTax { get; set; }

    public string? BuilderName { get; set; }
    public string? BuilderNameEnglish { get; set; }
    public string? BuilderMobileNo { get; set; }
    public int? BuilderMobileNoRemarkId { get; set; }

    public decimal? AreaSqMtr { get; set; }
    public decimal? TotalAreaSqMtr { get; set; }

    public bool Identify { get; set; }
}