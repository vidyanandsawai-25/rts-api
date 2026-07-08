namespace NtisPlatform.Core.Entities.Master;

public class ReportParameterDefinitionEntity : BaseEntity
{
    public int ReportDefinitionId { get; set; }
    public string ParameterKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ParameterType { get; set; } = string.Empty;
    public string? CascadeFromKey { get; set; }
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
}
