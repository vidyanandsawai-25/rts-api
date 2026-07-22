namespace NtisPlatform.Application.DTOs.DataEntrySameAs;

/// <summary>
/// A single assessable property "unit" under a building (Ward + PropertyNo), with its ward/zone/type/
/// category master data and carpet areas summed per property. Amenity part-types and building/wing-level
/// rows are excluded by the query, so <see cref="IsWing"/> is always false here (kept for SQL fidelity).
/// </summary>
public class DataEntrySameAsUnitDto
{
    public int PropertyId { get; set; }
    public int TaxZoneId { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string PartitionNo { get; set; } = string.Empty;
    public int? PropertyTypeId { get; set; }
    public string? PartType { get; set; }
    public int? CategoryId { get; set; }
    public string? PropertyCategoryName { get; set; }
    public bool IsWing { get; set; }
    public string Type { get; set; } = "0";
    public string FlatOrShopNo { get; set; } = "0";
    public double CarpetAreaSqMeter { get; set; }
    public double CarpetAreaSqFeet { get; set; }
}
