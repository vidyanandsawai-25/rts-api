namespace NtisPlatform.Application.DTOs;

public class ServiceDto
{
    public int Id { get; set; }
    public string Link { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtext { get; set; } = string.Empty;
    public List<ServiceStatDto> Stats { get; set; } = new();
}

public class ServiceStatDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
