namespace NtisPlatform.Application.DTOs.DataEntrySameAs;

/// <summary>
/// A candidate destination property returned by the sibling-property lookup.
/// </summary>
public class DataEntrySameAsPropertyDto
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? WingName { get; set; }
    public string? FlatOrShopNo { get; set; }
    public double CarpetAreaSqMeter { get; set; }
    public double CarpetAreaSqFeet { get; set; }
}
