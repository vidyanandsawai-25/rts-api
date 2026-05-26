namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for society amenity details associated with a property.
/// </summary>
public class SocietyAminityDetailsDto
{
    public int PropertyId { get; set; }
    public int SocietyDetailId { get; set; }
    public int wingId { get; set; }
    public string? WingNo { get; set; } = null;
    public string? WingName { get; set; } = null;
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public string PartitionNo { get; set; }=string.Empty;
    
}
