namespace NtisPlatform.Application.DTOs.PropertyCertificate;

public class PropertyCertificateUploadResponseDto
{
    public int PropertyCertificateId { get; set; }
    public Guid DocumentGuid { get; set; }
    public int DocumentId { get; set; }
    public int DocumentBindingId { get; set; }
    public int PropertyId { get; set; }
    public int CertificateTypeId { get; set; }
    public string? CertificateNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

public class PropertyCertificateDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int CertificateTypeId { get; set; }
    public string? CertificateTypeName { get; set; }
    public string? CertificateNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public bool IsEnabled { get; set; }
}
