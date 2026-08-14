namespace NtisPlatform.Core.Entities;

public class BulkUpdateActivityEntity
{
    public int Id { get; set; }
    public string? ActivityType { get; set; }
    public string? ActivityStatus { get; set; }
    public DateTime DateAndTime { get; set; }
    public int? Records { get; set; }
    public string? IPAddress { get; set; }
    public string? Remarks { get; set; }
    public string? UpdateName { get; set; }
    public string? DoneBy { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Duration { get; set; }
    public string? ActivityRemark { get; set; }
}
