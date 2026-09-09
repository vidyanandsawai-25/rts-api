namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for determining if a property can proceed to Draw Plan application directly
/// or needs to set a type first.
/// </summary>
public class PropertyDrawPlanStatusDto
{
    public int PropertyId { get; set; }
    public int? CategoryId { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? Type { get; set; }
}
