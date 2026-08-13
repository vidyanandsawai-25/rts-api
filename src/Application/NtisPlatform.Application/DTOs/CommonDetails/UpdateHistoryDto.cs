using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class UpdateHistoryQueryParameters : BaseQueryParameters
{
    public int? Id { get; set; }
    public int? ActivityId { get; set; }
    public string? UpdateName { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? Property { get; set; }
    public string? UpdatedColumns { get; set; }
    public bool? IsActive { get; set; }
    public string? DoneBy { get; set; }
    public string? ActivityType { get; set; }
    public string? ActivityStatus { get; set; }
}

public class UpdateHistoryDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string? UpdateName { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string Property { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UpdatedColumns { get; set; }
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }
    public string? IPAddress { get; set; }
    public string? DoneBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int ActivityId { get; set; }
    public string? ActivityType { get; set; }
    public string? ActivityStatus { get; set; }
    public string? ActivityDoneBy { get; set; }
    public int? Records { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Duration { get; set; }
    public string? ActivityRemark { get; set; }
}
