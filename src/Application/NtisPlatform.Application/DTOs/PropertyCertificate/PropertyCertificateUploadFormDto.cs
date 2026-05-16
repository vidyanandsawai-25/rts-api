using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyCertificate;

/// <summary>
/// Form DTO for uploading PropertyCertificate with file
/// </summary>
public class PropertyCertificateUploadFormDto
{
    /// <summary>
    /// The certificate file to upload
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// The property ID this certificate belongs to
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId must be greater than 0")]
    public int PropertyId { get; set; }

    /// <summary>
    /// The certificate type ID
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CertificateTypeId must be greater than 0")]
    public int CertificateTypeId { get; set; }

    /// <summary>
    /// Optional certificate number
    /// </summary>
    [MaxLength(100)]
    public string? CertificateNo { get; set; }

    /// <summary>
    /// Optional issue date of the certificate
    /// </summary>
    public DateTime? IssueDate { get; set; }
}
