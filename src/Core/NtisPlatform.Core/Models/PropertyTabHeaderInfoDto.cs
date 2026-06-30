namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Tab Header Info - includes StatusName and Old property identifier numbers
/// </summary>
public class PropertyTabHeaderInfoDto
{
    public int PropertyId { get; set; }
    public string? StatusName { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? UPICId { get; set; }
    public string? OwnerName { get; set; }
    public string? Address { get; set; }
    public string? TypeOfUse { get; set; }
}
