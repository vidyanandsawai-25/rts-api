namespace NtisPlatform.Core.Entities;

public class BulkUpdateHistoryEntity : BaseEntity
{
    public int BulkUpdateMasterId { get; set; }
    public long PropertyId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UpdatedColumns { get; set; }
    public string? IpAddress { get; set; }
    public string? Remarks { get; set; }
}
