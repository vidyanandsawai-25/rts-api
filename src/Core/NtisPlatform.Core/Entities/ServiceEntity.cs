namespace NtisPlatform.Core.Entities;

public class ServiceEntity : BaseEntity
{
    public string Link { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtext { get; set; } = string.Empty;
    public List<ServiceStat> Stats { get; set; } = new();
}

public class ServiceStat
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
