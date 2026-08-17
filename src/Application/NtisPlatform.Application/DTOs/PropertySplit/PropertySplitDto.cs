namespace NtisPlatform.Application.DTOs.PropertySplit;

public class PropertySplitDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<PropertyDetailsOldDto>? Data { get; set; } = null;
}
public class PropertyDetailsOldDto
{
    public int? PropertyOldId { get; set; }
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

