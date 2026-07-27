using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("ApprovalFlowStageMaster", Schema = "RTS")]
public class ApprovalFlowStageMasterEntity
{
    public int Id { get; set; }
    public int ApprovalFlowId { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int EmployeeTypeId { get; set; }
    public int SLADays { get; set; }
    public bool CanVerifyDocument { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanReturn { get; set; }
    public bool CanPay { get; set; }
    public bool IsFinalStage { get; set; }
}
