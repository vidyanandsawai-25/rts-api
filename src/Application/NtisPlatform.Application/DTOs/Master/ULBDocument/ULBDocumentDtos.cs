using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Read model for the current (latest) upload of a given ULB document type, with file metadata
/// joined in from <c>IDocumentApplicationService.GetDocumentByBindingAsync</c>.
/// </summary>
public class ULBDocumentDto
{
    public int Id { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public int? DocumentBindingId { get; set; }

    // Joined-in file metadata (null when no document has been uploaded yet)
    public string? OriginalFileName { get; set; }
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public Guid? DocumentGuid { get; set; }
}

/// <summary>
/// Creates the ULB document metadata row BEFORE any file upload — the frontend then calls the
/// existing generic <c>POST /api/documents/upload</c> with the returned Id as
/// <c>ReferenceTableId</c> (ReferenceTableName = "ULBDocument"), per the mandatory
/// Document/DocumentBinding pattern.
/// </summary>
public class CreateULBDocumentDto
{
    [Required(ErrorMessage = "ULBDocument_DocumentTypeCode_Required")]
    [StringLength(100, ErrorMessage = "ULBDocument_DocumentTypeCode_MaxLen_100")]
    public string DocumentTypeCode { get; set; } = string.Empty;
}
