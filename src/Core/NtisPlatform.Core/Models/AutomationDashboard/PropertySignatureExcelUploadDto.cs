namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Result payload for PropertySignatureDetails Excel upload.
/// </summary>
public class PropertySignatureExcelUploadResultDto
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ApprovedCount { get; set; }
    public List<RejectedPropertyDto> RejectedProperties { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
