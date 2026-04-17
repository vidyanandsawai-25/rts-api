namespace NtisPlatform.Core.Entities.Master;

public class RuleEntity : BaseEntity
{
    public string RuleCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}

