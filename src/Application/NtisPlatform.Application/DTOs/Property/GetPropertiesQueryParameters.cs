namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Query parameters for GET /api/property/get-properties endpoint
/// </summary>
public class GetPropertiesQueryParameters
{
    public string WardNo { get; set; } = string.Empty;
    public string FromPropertyNo { get; set; } = string.Empty;
    public string ToPropertyNo { get; set; } = string.Empty;
    public string? PartitionNo { get; set; }
    public string Flag { get; set; } = string.Empty;
    public int UserId { get; set; }
    public bool Apartment { get; set; } = false;
}
