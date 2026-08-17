namespace NtisPlatform.Application.DTOs.PropertyMergeSingle;

public class PropertyMergeSingleDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<PropertyDetailsDto>? Data { get; set; } = null;
}
public class PropertyDetailsDto
{
    // Property UnMerge Details
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
