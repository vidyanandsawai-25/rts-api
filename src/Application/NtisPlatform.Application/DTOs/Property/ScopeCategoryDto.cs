using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// DTO representing a scope category and its configured input options.
/// </summary>
public class ScopeCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
}
