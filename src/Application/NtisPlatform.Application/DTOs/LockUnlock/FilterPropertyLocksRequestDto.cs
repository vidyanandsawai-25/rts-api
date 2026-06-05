using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.LockUnlock;

public class FilterPropertyLocksRequestDto : BaseQueryParameters
{
    public int WardId { get; set; }
    public string FromPropertyNo { get; set; } = string.Empty;
    public string ToPropertyNo { get; set; } = string.Empty;
    public string? Search { get; set; }
}
