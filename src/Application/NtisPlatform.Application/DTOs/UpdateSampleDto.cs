namespace NtisPlatform.Application.DTOs;

/// <summary>
/// DTO for updating an existing Sample entity
/// </summary>
public class UpdateSampleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
