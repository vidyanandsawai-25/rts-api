namespace NtisPlatform.Core.Models.AutomationDashboard;

public class TrackStageStatusDto
{
    public int WorkflowStageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int IsCompleted { get; set; }
}
