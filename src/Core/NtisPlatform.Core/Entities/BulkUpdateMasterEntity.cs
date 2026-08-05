namespace NtisPlatform.Core.Entities;

public class BulkUpdateMasterEntity : BaseEntity
{
    public string UpdateCode { get; set; } = string.Empty;
    public string UpdateName { get; set; } = string.Empty;
    public string? ReferenceTableName { get; set; }
    public bool? IsApprovalRequired { get; set; }
    public ICollection<BulkUpdateFieldConfigEntity> FieldConfigs { get; set; } = [];
}
