using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

public class PropertyUnMergeResponseDto
{
    public int? PropertyId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? OwnerName { get; set; }
    public string? OccupierName { get; set; }
    public string? Address { get; set; }
    public string? MobileNo { get; set; }
    public string? Type { get; set; }
    public string? SocietyName { get; set; }
    public string? WingName { get; set; }
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? BHK { get; set; }
    public string? PropertyTypeDescription { get; set; }
}

public sealed class OldPropertyUnMergeResponseDto
{
    public int PropertyOldId { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldOccupierName { get; set; }
    public string? OldAddress { get; set; }
    public string? OldFlatOrShopNumber { get; set; }
    public string? OldWing { get; set; }
    public string? OldSocietyName { get; set; }
    public double? OldRV { get; set; }
    public double? OldGeneralTax { get; set; }
    public double? OldTotalTax { get; set; }
    public int? OldConstructionYear { get; set; }
    public double? OldConstructionArea { get; set; }
    public string? OldUseType { get; set; }
    public string? OldMobileNo { get; set; }
}

public class UnMergePropertydetailDto
{
    [Required(ErrorMessage = "UnMergeProperty_PropertyId_Required")]
    [Range(1,int.MaxValue,ErrorMessage = "UnMergeProperty_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "UnMergeProperty_PropertyType_Required")]
    [RegularExpression("^(Old|New)$", ErrorMessage = "UnMergePropertyd_PropertyType_Invalid")]
    public string PropertyType { get; set; } = string.Empty;
    public string WingName { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
