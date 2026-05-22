namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
