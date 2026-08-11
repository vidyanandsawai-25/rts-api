namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class PropertyMergeDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<PropertyMergeDetailDto> Data { get; set; } = new();
    public List<NewPropertyDetailsDto> NewData { get; set; } = new();
    public List<OldPropertyDetailsDto> OldData { get; set; } = new();
}

public class NewPropertyDetailsDto
{
    // New Property UnMerge Details
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

public class OldPropertyDetailsDto
{
    // Old Property UnMerge Details
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

public class PropertyMergeDetailDto
{
    public int Id { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }

    // Old Property Details
    public int PropertyOldId { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldMobileNo { get; set; }
    public string? OldOccupierName { get; set; }
    public string? OldAddress { get; set; }
    public string? OldSocietyName { get; set; }
    public double? OldRV { get; set; }
    public double? OldTotalTax { get; set; }
    public double? OldPlotArea { get; set; }
    public double? OldGeneralTax { get; set; }
    public int? OldConstructionYear { get; set; }
    public double? OldConstructionArea { get; set; }
}

public sealed class PropertyMappingSelection
{
    public int Id { get; init; }

    public int PropertyMapId { get; init; }
    public int? PropertyIdOld { get; init; }
    public int? PropertyIdNew { get; init; }
    public string? PropertyNoOld { get; init; }
    public string? PropertyNoNew { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class PropertyDemergePair
{
    public int PropertyOldId { get; init; }
    public int PropertyId { get; init; }
}