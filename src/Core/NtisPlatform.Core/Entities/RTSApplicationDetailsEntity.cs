using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class RTSApplicationDetailsEntity:BaseEntity, IHardDeletable
{
    public string ApplicationNo { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int ServiceId { get; set; }
    public string? SessionId { get; set; }
    public string? ApplicantName { get; set; }
    public string? ApplicantMobileNo { get; set; }
    public int? OwnerId { get; set; }
    public int ApprovalFlowId { get; set; }
    public int CurrentApprovalFlowStageId { get; set; }
    public int? UserId { get; set; }
    public int? CurrentStageOrder { get; set; }
    public bool IsReverted { get; set; }
    public string ApplicationStatus { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
    public virtual RTSDepartmentEntity Department { get; set; } = null!;
    public virtual RTSServiceEntity Service { get; set; } = null!;
    public virtual UserEntity User { get; set; } = null!;
    public virtual List<RTSFieldValueEntity> FieldValueData { get; set; } = new List<RTSFieldValueEntity>();
    public virtual List<TrackApplicationHistoryEntity> TrackApplicationHistory { get; set; } = new List<TrackApplicationHistoryEntity>();

}
