using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class UpdateActivityQueryParameters : BaseQueryParameters
{
    public int? Id { get; set; }
    public string? ActivityType { get; set; }
    public string? ActivityStatus { get; set; }
    public DateTime? CreatedDateFrom { get; set; }
    public DateTime? CreatedDateTo { get; set; }
    public string? DoneBy { get; set; }
    public string? Remarks { get; set; }
    public string? ActivityRemark { get; set; }
}

public class UpdateActivityDto
{
    public int Id { get; set; }
    public string? ActivityType { get; set; }
    public string? ActivityStatus { get; set; }
    public DateTime CreatedDate { get; set; }
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
