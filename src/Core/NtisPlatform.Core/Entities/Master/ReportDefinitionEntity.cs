namespace NtisPlatform.Core.Entities.Master;

public class ReportDefinitionEntity : BaseEntity
{
    public string ReportCode { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public int? ModuleId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TemplateFile { get; set; } = string.Empty;
    public string DataProviderCode { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
