namespace NtisPlatform.Core.Entities;

public class BulkUpdateMasterEntity : BaseEntity
{
    public string UpdateCode { get; set; } = string.Empty;
    public string UpdateName { get; set; } = string.Empty;
    public string UpdateNameMarathi { get; set; } = string.Empty;
    public string ReferenceTableName { get; set; } = string.Empty;
    public int DisplaySequence { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsApprovalRequired { get; set; }
    public ICollection<BulkUpdateFieldConfigEntity> FieldConfigs { get; set; } = [];
}
