namespace NtisPlatform.Application.DTOs.PropertyMergeDetails;

public class PropertyMergeDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PropertyMergeDetailDto? Data { get; set; } = new();
}
public class PropertyMergeDetailDto
{
    public int Id { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public List<PropertyOldDetails> PropertyOldDetails { get; set; } = new();

}
public class PropertyOldDetails
{
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
