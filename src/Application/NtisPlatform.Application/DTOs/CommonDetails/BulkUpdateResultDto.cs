namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateResultDto
{
    public string UpdateCode { get; set; } = string.Empty;
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
