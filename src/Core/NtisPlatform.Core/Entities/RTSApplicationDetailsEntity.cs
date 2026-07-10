using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class RTSApplicationDetailsEntity:BaseEntity, IHardDeletable
{
    public string ApplicationNo { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int ServiceId { get; set; }
    public string? SessionId { get; set; }
    public string ApplicationStatus { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
    public virtual List<RTSFieldValueEntity> FieldValueData { get; set; } = new List<RTSFieldValueEntity>();

}
