namespace NtisPlatform.Application.DTOs.RTSApplicationApproval;

public class RTSStageDocumentDto
{
    public int BindingId { get; set; }
    public int DocumentId { get; set; }
    public Guid DocumentGuid { get; set; }
    public string? DocumentTitle { get; set; }
    public string? FileName { get; set; }
    public string? OriginalFileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Remarks { get; set; }
    public int UploadedByUserId { get; set; }
    public string? UploadedByUserName { get; set; }
    public string? UploadedByFullName { get; set; }
    public DateTime? UploadedDate { get; set; }
}
