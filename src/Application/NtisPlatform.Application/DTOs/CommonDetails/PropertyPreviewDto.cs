namespace NtisPlatform.Application.DTOs.CommonDetails;

public class PropertyPreviewDto
{
    public long Id { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public string PartitionNo { get; set; } = string.Empty;
    public Dictionary<string, object?> CurrentValues { get; set; } = [];
}
