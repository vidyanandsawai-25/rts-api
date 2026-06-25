namespace NtisPlatform.Core.Entities;

public class PropertyWorkflowDetailsEntity : BaseEntity
{
    public int PropertyId { get; set; }
    public int WorkflowStageId { get; set; }
    public int? ModuleId { get; set; }
    public bool? CurrentStatus { get; set; }
}
