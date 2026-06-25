using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

[Table("PropertyWorkflowDetails", Schema = "PTIS")]
public class PropertyWorkflowDetailsEntity : BaseEntity
{
    public int PropertyId { get; set; }
    public int WorkflowStageId { get; set; }
    public int? ModuleId { get; set; }
    public bool? CurrentStatus { get; set; }

    [ForeignKey(nameof(PropertyId))]
    public virtual PropertyEntity Property { get; set; } = null!;

    [ForeignKey(nameof(WorkflowStageId))]
    public virtual PropertyWorkflowStageMasterEntity WorkflowStage { get; set; } = null!;
}
