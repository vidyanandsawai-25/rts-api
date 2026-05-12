namespace NtisPlatform.Core.Models;

/// <summary>
/// Request DTO for apartment property tax details queries.
/// </summary>
public class PropertyApartmentTaxRequestDto
{
    public int? WardId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartType { get; set; }
    public string? Type { get; set; }
    public int? PropertyId { get; set; }
}
