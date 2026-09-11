namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Response item DTO for GET /api/property/get-properties endpoint
/// </summary>
public class GetPropertiesItemDto : BaseDtos
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? UPICId { get; set; }
    public string? CSN { get; set; }
    public string? SubZoneNo { get; set; }
    public string? PlotNo { get; set; }
    public int? CategoryId { get; set; }
    public string? Type { get; set; }
    public string? PartType { get; set; }
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
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsMerged { get; set; }
    public int MapCount { get; set; }
    public string? CreatedBy { get; set; }
}
