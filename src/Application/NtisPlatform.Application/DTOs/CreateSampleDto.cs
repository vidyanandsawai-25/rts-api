namespace NtisPlatform.Application.DTOs;

/// <summary>
/// DTO for creating a new Sample entity
/// </summary>
public class CreateSampleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
