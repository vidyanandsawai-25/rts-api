namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Uploaded citizen document representation for appeal review.
/// </summary>
public class RTSAppealDocumentDto
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentPath { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
}
