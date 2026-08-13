namespace NtisPlatform.Core.Entities;

public class BulkUpdateHistoryEntity : BaseEntity
{
    public int ActivityId { get; set; }
    public int BulkUpdateMasterId { get; set; }
    public int PropertyId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UpdatedColumns { get; set; }
}
