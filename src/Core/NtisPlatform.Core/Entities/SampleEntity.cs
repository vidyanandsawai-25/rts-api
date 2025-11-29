namespace NtisPlatform.Core.Entities;

/// <summary>
/// Sample entity for demonstration purposes
/// </summary>
public class SampleEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
