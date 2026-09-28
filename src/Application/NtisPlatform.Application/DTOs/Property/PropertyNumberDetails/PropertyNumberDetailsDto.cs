namespace NtisPlatform.Application.DTOs.PropertyNumberDetails;

public class PropertyNumberDetailsDto
{
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public string PartitionNo { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FlatOrShopNo { get; set; } = string.Empty;
    public int? WingDetailId { get; set; }
    public string PropertyType { get; set; } = string.Empty;
}
