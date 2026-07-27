using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("ApprovalFlowMaster", Schema = "RTS")]
public class ApprovalFlowMasterEntity : BaseEntity
{
    public int ServiceId { get; set; }
    public string ApprovalFlowName { get; set; } = string.Empty;
}
