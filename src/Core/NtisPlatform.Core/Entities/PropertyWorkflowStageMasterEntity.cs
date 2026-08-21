using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

[Table("PropertyWorkflowStageMaster", Schema = "PTIS")]
public class PropertyWorkflowStageMasterEntity : BaseEntity
{
    public string StageName { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int? UserId { get; set; }

    public ICollection<PropertyWorkflowDetailsEntity> WorkflowDetails { get; set; } = new List<PropertyWorkflowDetailsEntity>();
 
}
